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
                var tableDescMatch = Regex.Match(chunk, @"^Description:\s*(.*)$", RegexOptions.Multiline);
                var tableDesc = tableDescMatch.Success ? tableDescMatch.Groups[1].Value.Trim() : string.Empty;

                // Treat "Columns" / "Columns:" as no description
                if (Regex.IsMatch(tableDesc, @"^columns:?$", RegexOptions.IgnoreCase))
                {
                    tableDesc = string.Empty;
                }

                var colMatches = Regex.Matches(chunk, @"- \[(?<col>[^\]]+)\]\r?\nDescription\s*:?(?<desc>(?:(?!^- \[).*\r?\n?)*)", RegexOptions.Multiline);
                var columnDescriptions = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
                foreach (Match m in colMatches)
                {
                    var col = m.Groups["col"].Value;
                    var desc = m.Groups["desc"].Value.Trim();
                    columnDescriptions[col] = desc;
                }

                bool hasComments = !string.IsNullOrWhiteSpace(tableDesc) ||
                                          columnDescriptions.Values.Any(v => !string.IsNullOrWhiteSpace(v));

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

            foreach (var req in requests)
            {
                all = await AppendOrReplaceBlockAsync(all, req);
            }

            await File.WriteAllTextAsync(path, all);
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

        private async Task<string> AppendOrReplaceBlockAsync(string all, TableAnnotationRequest req)
        {
            var sb = new StringBuilder();

            sb.AppendLine($"Table: {req.FullTableName}");

            // --- Table description (multi-line, preserve blank lines) ---
            var tableDesc = (req.TableDescription ?? string.Empty).Replace("\r\n", "\n");
            var tdLines = tableDesc.Split('\n');
            if (tdLines.Length > 0 && !(tdLines.Length == 1 && string.IsNullOrWhiteSpace(tdLines[0])))
            {
                sb.AppendLine($"Description: {tdLines[0]}");
                for (int i = 1; i < tdLines.Length; i++)
                {
                    sb.AppendLine(tdLines[i]); // preserve blank lines
                }                
            }
            else
            {
                sb.AppendLine("Description:");
            }

            sb.AppendLine("Columns:");

            // --- Column descriptions (multi-line, preserve blank lines) ---
            foreach (var kv in req.ColumnComments.OrderBy(k => k.Key, StringComparer.OrdinalIgnoreCase))
            {
                var col = kv.Key;
                var comment = (kv.Value ?? string.Empty).Replace("\r\n", "\n");
                var cLines = comment.Split('\n');

                sb.AppendLine($"- [{col}]");

                if (cLines.Length > 0 && !(cLines.Length == 1 && string.IsNullOrWhiteSpace(cLines[0])))
                {
                    sb.AppendLine($"Description: {cLines[0]}");
                    for (int i = 1; i < cLines.Length; i++)
                    {
                        sb.AppendLine(cLines[i]); // preserve blank lines
                    }           
                }
                else
                {
                    sb.AppendLine("Description:");
                }
            }

            // Ensure block ends with one newline
            var newBlock = sb.ToString().TrimEnd('\r', '\n') + Environment.NewLine;

            // --- Replace from this header until the next "Table:" header (or EOF) ---
            var pattern = $@"^Table:\s*{Regex.Escape(req.FullTableName)}[\s\S]*?(?=^\s*Table:\s*\[|\z)";
            var re = new Regex(pattern, RegexOptions.Multiline);

            if (re.IsMatch(all))
            {
                all = re.Replace(all, newBlock, 1);
            }
            else
            {
                if (!string.IsNullOrWhiteSpace(all) && !all.EndsWith(Environment.NewLine + Environment.NewLine))
                {
                    all = all.TrimEnd('\r', '\n') + Environment.NewLine + Environment.NewLine;
                }
                all += newBlock;
            }

            // Ensure between-table spacing = exactly 2 newlines
            all = Regex.Replace(all, @"(\r?\n){2,}(?=Table:)", Environment.NewLine + Environment.NewLine);

            // Collapse 2+ newlines elsewhere into 1 (but not before a Table:)
            all = Regex.Replace(all, @"(\r?\n){2,}(?!Table:)", Environment.NewLine);

            // Remove trailing newlines at EOF (optional)
            all = Regex.Replace(all, @"(\r?\n)+\z", Environment.NewLine);

            return all;
        }
        #endregion
    }
}
