namespace SITA.Core.Repositories;

internal static class SqlHelpers
{
    /// <summary>
    /// Builds a "contains" pattern for LIKE ... ESCAPE '\'. Returns null for empty input so the
    /// query can use "@Keyword IS NULL OR ..." to skip the filter.
    /// </summary>
    public static string? ContainsPattern(string? keyword)
    {
        if (string.IsNullOrWhiteSpace(keyword)) return null;
        var escaped = keyword.Trim()
            .Replace("\\", "\\\\")
            .Replace("%", "\\%")
            .Replace("_", "\\_");
        return $"%{escaped}%";
    }
}
