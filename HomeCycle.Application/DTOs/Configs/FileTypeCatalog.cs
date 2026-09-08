using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HomeCycle.Application.DTOs.Configs
{
    public static class FileTypeCatalog
    {
        private static readonly IReadOnlyDictionary<string, string> MimeTypesByExtension =
            new ReadOnlyDictionary<string, string>(
                new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
                {
                    [".jpg"] = "image/jpeg",
                    [".jpeg"] = "image/jpeg",
                    [".png"] = "image/png",
                    [".webp"] = "image/webp",
                    [".gif"] = "image/gif",
                    [".pdf"] = "application/pdf"
                });

        public static IReadOnlyCollection<string> SupportedExtensions { get; } = MimeTypesByExtension.Keys.ToArray();

        public static string NormalizeExtension(string? extension)
        {
            if (string.IsNullOrWhiteSpace(extension))
                return string.Empty;

            var normalized = extension.Trim().ToLowerInvariant();

            return normalized.StartsWith('.') ? normalized : $".{normalized}";
        }

        public static bool IsSupportedExtension(string? extension)
        {
            var normalized = NormalizeExtension(extension);
            return MimeTypesByExtension.ContainsKey(normalized);
        }

        public static bool TryGetMimeType(string? extension, out string mimeType)
        {
            var normalized = NormalizeExtension(extension);
            return MimeTypesByExtension.TryGetValue(normalized, out mimeType!);
        }

        public static List<string> NormalizeExtensions(IEnumerable<string> extensions)
        {
            return extensions
                .Select(NormalizeExtension)
                .Where(x => !string.IsNullOrWhiteSpace(x))
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .OrderBy(x => x, StringComparer.OrdinalIgnoreCase)
                .ToList();
        }
    }

}
