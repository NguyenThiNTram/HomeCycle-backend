using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json.Nodes;
using System.Text.Json;
using System.Threading.Tasks;

namespace HomeCycle.Infrastructure.Auditing
{
    public sealed class AuditPayloadSanitizer
    {
        private static readonly string[] SensitiveTerms =
        [
            "password",
            "passwordhash",
            "token",
            "otp",
            "authorization",
            "cookie",
            "secret",
            "secretkey",
            "apikey",
            "checksumkey",
            "privatekey",
            "credential"
        ];

        private static readonly JsonSerializerOptions JsonOptions =
            new(JsonSerializerDefaults.Web);

        public JsonNode? Sanitize(
            IReadOnlyDictionary<string, object?>? values)
        {
            if (values is null || values.Count == 0)
                return null;

            var node = JsonSerializer.SerializeToNode(
                values,
                JsonOptions);

            Redact(node);

            return node;
        }

        private static void Redact(JsonNode? node)
        {
            if (node is JsonObject jsonObject)
            {
                var keys = jsonObject
                    .Select(x => x.Key)
                    .ToArray();

                foreach (var key in keys)
                {
                    if (IsSensitive(key))
                    {
                        jsonObject.Remove(key);
                        continue;
                    }

                    Redact(jsonObject[key]);
                }

                return;
            }

            if (node is JsonArray jsonArray)
            {
                foreach (var item in jsonArray)
                    Redact(item);
            }
        }

        private static bool IsSensitive(string key)
        {
            var normalized = new string(
                key.Where(char.IsLetterOrDigit)
                    .Select(char.ToLowerInvariant)
                    .ToArray());

            return SensitiveTerms.Any(
                term => normalized.Contains(
                    term,
                    StringComparison.Ordinal));
        }
    }
}
