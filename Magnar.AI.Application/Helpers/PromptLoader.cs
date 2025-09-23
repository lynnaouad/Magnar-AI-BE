namespace Magnar.AI.Application.Helpers;

public static class PromptLoader
{
    private static readonly Dictionary<string, string> _cache = [];

    /// <summary>
    /// Load prompt from text file.
    /// </summary>
    /// <param name="promptName">Name of the prompt.</param>
    /// <param name="promptsFolder">Folder containing prompts.</param>
    /// <returns>Prompt content as string.</returns>
    public static async Task<string> LoadPromptAsync(string promptName, string? promptsFolder = null)
    {
        var cacheKey = $"{promptsFolder}/{promptName}";
        if (_cache.TryGetValue(cacheKey, out string value))
        {
            return value;
        }

        var path = !string.IsNullOrEmpty(promptsFolder) 
            ? Path.Combine(AppDomain.CurrentDomain.BaseDirectory, Constants.Folders.Assets, Constants.Folders.Prompts, promptsFolder, $"{promptName}")
            : Path.Combine(AppDomain.CurrentDomain.BaseDirectory, Constants.Folders.Assets, Constants.Folders.Prompts, $"{promptName}");

        if (!File.Exists(path))
        {
            throw new FileNotFoundException($"Prompt '{promptName}' not found in '{promptsFolder}'");
        }

        var content = await File.ReadAllTextAsync(path);
        if (string.IsNullOrEmpty(content))
        {
            throw new FileNotFoundException($"Prompt '{promptName}' not found in '{promptsFolder}'");
        }

        _cache[cacheKey] = content;
        return content;
    }
}