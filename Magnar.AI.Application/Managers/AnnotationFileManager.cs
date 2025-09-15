using Magnar.AI.Application.Dto.Schema;
using Magnar.AI.Application.Interfaces.Managers;
using System.Text;
using System.Text.RegularExpressions;

namespace Magnar.AI.Application.Managers
{
    public class AnnotationFileManager : IAnnotationFileManager
    {
        #region Members
        private readonly string baseFolder;
        private static readonly Regex BlockHeader = new(@"^Table: \[(?<schema>[^\]]+)\]\.\[(?<table>[^\]]+)\]", RegexOptions.Multiline | RegexOptions.Compiled);
        #endregion

        #region Constructor

        public AnnotationFileManager()
        {
            baseFolder = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Assets/Annotations");

            // Ensure the folder exists
            if (!Directory.Exists(baseFolder))
            {
                Directory.CreateDirectory(baseFolder);
            }
        }
        #endregion

        public async Task<IEnumerable<SelectedTableBlock>> ReadAllBlocksAsync(int connectionId)
        {
            var blocks = new List<SelectedTableBlock>();

            // Resolve the file path for the current connection
            var path = GetFilePath(connectionId);

            // If the file does not exist, return empty
            if (!File.Exists(path))
            {
                return blocks;
            }

            // Read file content
            var text = await File.ReadAllTextAsync(path);

            // Return empty if file is blank
            if (string.IsNullOrWhiteSpace(text))
            {
                return blocks;
            }

            // Normalize spacing: collapse 3+ blank lines into exactly 2
            text = Regex.Replace(text.Trim(), @"(\r?\n){3,}", Environment.NewLine + Environment.NewLine);

            // Split into blocks using double newline as separator
            var chunks = text.Split(
                new[] { Environment.NewLine + Environment.NewLine, "\n\n" },
                StringSplitOptions.RemoveEmptyEntries);

            foreach (var chunk in chunks)
            {
                // Extract full table name from "Table: [Schema].[Table]" header
                var match = BlockHeader.Match(chunk);
                var fullName = match.Success
                    ? $"[{match.Groups["schema"].Value}].[{match.Groups["table"].Value}]"
                    : string.Empty;

                // Parse description line
                var descMatch = Regex.Match(chunk, @"^Description:\s*(.*)$", RegexOptions.Multiline);
                var desc = descMatch.Success ? descMatch.Groups[1].Value.Trim() : string.Empty;

                // Treat "Columns" / "Columns:" as no description
                if (Regex.IsMatch(desc, @"^columns:?$", RegexOptions.IgnoreCase))
                {
                    desc = string.Empty;
                }

                // Detect if there are column comments
                var hasColComments = Regex.IsMatch(chunk, @"- \[[^\]]+\]\s*:\s*\S");

                // Determine whether this block has any comments at all
                bool hasComments = !string.IsNullOrWhiteSpace(desc) || hasColComments;

                // Build block
                blocks.Add(new SelectedTableBlock
                {
                    RawBlockText = chunk.Trim() + Environment.NewLine,
                    FullTableName = fullName,
                    HasComments = hasComments
                });
            }

            // Return normalized list of blocks
            return blocks;
        }
      
        public async Task AppendOrReplaceBlocksAsync(IEnumerable<TableAnnotationRequest> requests, int connectionId)
        {
            // Resolve the file path
            var path = GetFilePath(connectionId);

            // Ensure the file exists
            if (!File.Exists(path))
            {
                using (File.Create(path)) { }
            }

            // Read existing file content
            var all = await File.ReadAllTextAsync(path);

            // Normalize spacing: collapse 3+ newlines into exactly 2
            all = Regex.Replace(all.Trim(), @"(\r?\n){3,}", Environment.NewLine + Environment.NewLine);

            foreach (var req in requests)
            {
                // Build new block for this table
                var sb = new StringBuilder();
                sb.AppendLine($"Table: {req.FullTableName}");
                sb.AppendLine($"Description: {req.TableDescription?.Trim() ?? string.Empty}");
                sb.AppendLine("Columns:");

                foreach (var (col, comment) in req.ColumnComments.OrderBy(kv => kv.Key))
                {
                    var suffix = string.IsNullOrWhiteSpace(comment) ? string.Empty : $" : {comment}";
                    sb.AppendLine($"- [{col}]{suffix}");
                }

                var newBlock = sb.ToString().TrimEnd();

                // Try to replace existing block
                var updated = Regex.Replace(
                    all,
                    Regex.Escape($"Table: {req.FullTableName}") + @"[\s\S]*?(?=(?:\r?\n){2}|\z)",
                    newBlock,
                    RegexOptions.Multiline);

                if (updated != all)
                {
                    // Replacement happened
                    all = updated;
                }
                else
                {
                    // Append to the end
                    if (!string.IsNullOrWhiteSpace(all))
                    {
                        all = all.TrimEnd() + Environment.NewLine + Environment.NewLine + newBlock;
                    }
                    else
                    {
                        all = newBlock;
                    }
                }

                // Re-normalize after each addition to keep spacing consistent
                all = Regex.Replace(all.Trim(), @"(\r?\n){3,}", Environment.NewLine + Environment.NewLine);
            }

            // Save back once at the end
            await File.WriteAllTextAsync(path, all + Environment.NewLine);
        }

        public async Task CleanupOrphanedBlocksAsync(int connectionId, IEnumerable<string> existingDbTables)
        {
            // Resolve the file path for the given connection
            var path = GetFilePath(connectionId);

            // Ensure the file exists
            if (!File.Exists(path))
            {
                return; // nothing to clean
            }

            var text = await File.ReadAllTextAsync(path);
            if (string.IsNullOrWhiteSpace(text))
            {
                return; // empty file
            }

            var chunks = text.Split(["\r\n\r\n", "\n\n"], StringSplitOptions.RemoveEmptyEntries);

            // Keep only blocks where the table still exists in DB
            var validChunks = new List<string>();
            foreach (var chunk in chunks)
            {
                var match = BlockHeader.Match(chunk);
                if (!match.Success) continue;

                var fullName = $"[{match.Groups["schema"].Value}].[{match.Groups["table"].Value}]";
                if (existingDbTables.Contains(fullName, StringComparer.OrdinalIgnoreCase))
                {
                    validChunks.Add(chunk.Trim());
                }
            }

            // Rebuild file content
            var cleaned = string.Join(Environment.NewLine + Environment.NewLine, validChunks);
            await File.WriteAllTextAsync(path, cleaned);
        }

        #region Private Methods
        private string GetFilePath(int connectionId) => Path.Combine(baseFolder, $"annotations_{connectionId}.txt");
        #endregion
    }
}
