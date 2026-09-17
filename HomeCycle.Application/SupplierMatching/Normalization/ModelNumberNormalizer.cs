namespace HomeCycle.Application.SupplierMatching.Normalization;

public static class ModelNumberNormalizer
{
    public static string? Normalize(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
            return null;

        var normalized = new string(value
            .Trim()
            .ToLowerInvariant()
            .Where(char.IsAsciiLetterOrDigit)
            .ToArray());
        return normalized.Length == 0 ? null : normalized;
    }
}
