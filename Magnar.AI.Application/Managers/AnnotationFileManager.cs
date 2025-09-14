using Magnar.AI.Application.Dto.Schema;
using Magnar.AI.Application.Interfaces.Managers;
using System.Text;
using System.Text.RegularExpressions;

namespace Magnar.AI.Application.Managers
{
    public class AnnotationFileManager : IAnnotationFileManager
    {
        private readonly string baseFolder;
        private static readonly Regex BlockHeader = new(@"^Table: \[(?<schema>[^\]]+)\]\.\[(?<table>[^\]]+)\]", RegexOptions.Multiline | RegexOptions.Compiled);

        public AnnotationFileManager()
        {
            baseFolder = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Assets/Annotations");

            // Ensure the folder exists
            if (!Directory.Exists(baseFolder))
            {
                Directory.CreateDirectory(baseFolder);
            }
        }

        public async Task<IEnumerable<SelectedTableBlock>> ReadAllBlocksAsync(int connectionId)
        {
            var path = GetFilePath(connectionId);
            if (!File.Exists(path))
            {
                return [];
            }

            var text = await File.ReadAllTextAsync(path);
            var blocks = new List<SelectedTableBlock>();
            if (string.IsNullOrWhiteSpace(text)) return blocks;

            var chunks = text.Split(["\r\n\r\n", "\n\n"], StringSplitOptions.RemoveEmptyEntries);
            foreach (var chunk in chunks)
            {
                var match = BlockHeader.Match(chunk);
                var fullName = match.Success ? $"[{match.Groups["schema"].Value}].[{match.Groups["table"].Value}]" : string.Empty;

                // Parse description
                var descMatch = Regex.Match(chunk, @"^Description:\s*(.*)$", RegexOptions.Multiline);
                var desc = descMatch.Success ? descMatch.Groups[1].Value.Trim() : string.Empty;

                if (Regex.IsMatch(desc, @"^columns:?$", RegexOptions.IgnoreCase))
                {
                    desc = string.Empty;
                }
                   
                // Parse column comments
                var hasColComments = Regex.IsMatch(chunk, @"- \[[^\]]+\]\s*:\s*\S");

                // Determine HasComments
                bool hasComments = !string.IsNullOrWhiteSpace(desc) || hasColComments;

                blocks.Add(new SelectedTableBlock
                {
                    RawBlockText = chunk.Trim() + Environment.NewLine,
                    FullTableName = fullName,
                    HasComments = hasComments
                });
            }

            return blocks;
        }

        public async Task AppendOrReplaceBlockAsync(TableAnnotationRequest req, int connectionId)
        {
            var path = GetFilePath(connectionId);
            if (!File.Exists(path))
            {
                using (File.Create(path)) { }
            }

            var all = await File.ReadAllTextAsync(path);

            // Build block text in the exact format
            var sb = new StringBuilder();
            sb.AppendLine($"Table: {req.FullTableName}");
            sb.AppendLine($"Description: {req.TableDescription?.Trim() ?? string.Empty}");
            sb.AppendLine("Columns:");

            // Column lines: maintain order by column name asc (or the UI can preserve order)
            foreach (var (col, comment) in req.ColumnComments.OrderBy(kv => kv.Key))
            {
                var suffix = string.IsNullOrWhiteSpace(comment) ? string.Empty : $" : {comment}";
                sb.AppendLine($"- [{col}]{suffix}");
            }

            var newBlock = sb.ToString().TrimEnd() + Environment.NewLine; // ensure newline

            var m = BlockHeader.Matches(all);
            bool replaced = false;
            if (m.Count > 0)
            {
                all = Regex.Replace(all, Regex.Escape($"Table: {req.FullTableName}") + @"[\s\S]*?(?=(?:\r?\n){2}|\z)",
                                     newBlock, RegexOptions.Multiline);
                replaced = all.Contains(newBlock);
            }

            if (!replaced)
            {
                if (!string.IsNullOrWhiteSpace(all)) all += Environment.NewLine + Environment.NewLine;
                all += newBlock;
            }

            await File.WriteAllTextAsync(path, all);
        }

        private string GetFilePath(int connectionId) => Path.Combine(baseFolder, $"annotations_{connectionId}.json");
    }
}
