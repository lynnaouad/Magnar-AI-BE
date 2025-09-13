using FuzzySharp;
using FuzzySharp.Extractor;
using Magnar.AI.Application.Extensions;
using Microsoft.AspNetCore.StaticFiles;

namespace Magnar.AI.Application.Helpers;

public static class Utilities
{
    public static IEnumerable<EnumDto> GetEnumValues(Type type)
    {
        return Enum.GetValues(type)
                 .Cast<Enum>()
                 .Select(e => new EnumDto
                 {
                     Id = Convert.ToInt32(e),
                     Name = e.ToString(),
                     Description = e.GetDescription(),
                 });
    }

    public static IDictionary<string, string> GetDefaultMimeTypes()
    {
        FileExtensionContentTypeProvider provider = new();
        return provider.Mappings;
    }

    public static string GetExtension(string path)
    {
        if (string.IsNullOrWhiteSpace(path))
        {
            return string.Empty;
        }

        int index = path.LastIndexOf('.');
        if (index < 0)
        {
            return string.Empty;
        }

        return path[index..];
    }

    public static IDictionary<string, string> FilterMimeTypes(IEnumerable<string> extensions)
    {
        IDictionary<string, string> mimeTypes = GetDefaultMimeTypes();
        return mimeTypes
            .Where(kvp => extensions.Contains(kvp.Key, StringComparer.OrdinalIgnoreCase))
            .ToDictionary(kvp => kvp.Key, kvp => kvp.Value);
    }

    public static IDictionary<string, string> GetImageMimeTypes()
    {
        List<string> desiredExtensions =
        [
            ".webp", ".svg", ".svgz", ".pnz", ".png", ".jpg", ".jpeg", ".jpe", ".jfif", ".ico", ".gif",
        ];

        return FilterMimeTypes(desiredExtensions);
    }

    public static IDictionary<string, string> GetDocumentMimeTypes()
    {
        List<string> desiredExtensions =
        [
             ".pdf", ".doc", ".docx", ".xls", ".xlsx", ".ppt", ".pptx",
        ];

        return FilterMimeTypes(desiredExtensions);
    }

    public static bool CheckCompany(string companyIds, int companyId)
    {
        if (string.IsNullOrEmpty(companyIds))
        {
            return false;
        }

        IEnumerable<string> ids = companyIds.Split(',').Select(x => x.Trim());
        if (ids is null || !ids.Any())
        {
            return false;
        }

        return ids.Contains(companyId.ToString());
    }

    /// <summary>
    /// A helper method that safely converts a string to an enum value.
    /// </summary>
    public static TEnum? GetEnum<TEnum>(string key)
        where TEnum : struct, Enum
    {
        return Enum.GetValues<TEnum>().FirstOrDefault(x => x.ToString() == key);
    }

    /// <summary>
    /// A helper method that safely converts aan id to an enum value.
    /// </summary>
    public static TEnum? GetEnum<TEnum>(int? id)
        where TEnum : struct, Enum
    {
        return Enum.GetValues<TEnum>().FirstOrDefault(x => Convert.ToInt32(x) == (id ?? 0));
    }

    /// <summary>
    /// Validates whether a string represents a defined value of a specified enum type.
    /// </summary>
    public static bool ValidateEnumValue<TEnum>(string value, bool ignoreCase = true)
        where TEnum : struct, Enum
    {
        if (string.IsNullOrEmpty(value))
        {
            return false;
        }

        return Enum.TryParse<TEnum>(value, ignoreCase, out _) && Enum.IsDefined(typeof(TEnum), value);
    }

    /// <summary>
    /// Retrieves a code from a collection of objects by matching an input string against a property of those objects, using exact or fuzzy matching.
    /// </summary>
    /// <param name="input">The string to search for (e.g., a name or key).</param>
    /// <param name="list">A collection of objects (IEnumerable.<T>) to search through.</param>
    /// <param name="searchSelector">A function that selects the property of T to compare against input (e.g., x => x.Name).</param>
    /// <param name="returnSelector">A function that selects the code to return when a match is found (e.g., x => x.Code).</param>
    /// <param name="fuzzyThreshold">(default: 80): The minimum similarity score (0-100) required for a fuzzy match to be considered valid.</param>
    /// <returns>The code (string?) associated with the matched object, or null if no match is found.</returns>
    public static string GetCodeFromLookup<T>(string input, IEnumerable<T> list, Func<T, string> searchSelector, Func<T, string> returnSelector, int fuzzyThreshold = 80)
    {
        if (string.IsNullOrWhiteSpace(input) || list is null || !list.Any())
        {
            return null;
        }

        // Try exact match (case-insensitive)
        T exactMatch = list.FirstOrDefault(x => string.Equals(searchSelector(x), input, StringComparison.OrdinalIgnoreCase));
        if (exactMatch is not null)
        {
            return returnSelector(exactMatch);
        }

        // Prepare map for fast lookup
        var nameMap = list.Select(x => new { Item = x, Name = searchSelector(x) }).Where(x => !string.IsNullOrWhiteSpace(x.Name));

        ExtractedResult<string> bestMatch = Process.ExtractOne(input, nameMap.Select(x => x.Name));

        if (bestMatch is not null && bestMatch.Score >= fuzzyThreshold)
        {
            var matched = nameMap.FirstOrDefault(x => x.Name == bestMatch.Value);
            return matched is not null ? returnSelector(matched.Item) : null;
        }

        return null;
    }
}
