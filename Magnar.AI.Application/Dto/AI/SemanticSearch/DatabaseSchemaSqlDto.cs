namespace Magnar.AI.Application.Dto.AI.SemanticSearch;

public class DatabaseSchemaSqlDto
{
    public bool Success { get; set; }

    public List<string> RelevantTables { get; set; } = [];

    public List<string> MissingTables { get; set; } = [];

    public string Sql { get; set; } = string.Empty;

    public List<string> Columns { get; set; } = [];
}
