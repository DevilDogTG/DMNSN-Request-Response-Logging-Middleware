using DMNSN.Helpers.Minifier;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Primitives;
using System.Text;
using System.Text.Json;

namespace DMNSN.AspNetCore.Middleware.RqRsLogging
{
    public class RqRsLoggingBase
    {
        internal async Task<string> ReadRequestBody(HttpRequest request)
        {
            request.Body.Position = 0;
            var reader = new StreamReader(request.Body, Encoding.UTF8, leaveOpen: true);
            var body = await reader.ReadToEndAsync();
            request.Body.Position = 0;
            return body;
        }

        internal string TruncateLargeFields(string jsonString, int maxFieldLength)
        {
            try
            {
                var jsonDocument = JsonDocument.Parse(jsonString);
                var rootElement = jsonDocument.RootElement;
                using var memoryStream = new MemoryStream();
                using var writer = new Utf8JsonWriter(memoryStream, new JsonWriterOptions { Indented = true });

                TruncateLargeFieldsRecursive(rootElement, writer, maxFieldLength);

                writer.Flush();
                return Encoding.UTF8.GetString(memoryStream.ToArray());
            }
            catch
            { return jsonString; }
        }

        internal void TruncateLargeFieldsRecursive(JsonElement element, Utf8JsonWriter writer, int maxFieldLength)
        {
            switch (element.ValueKind)
            {
                case JsonValueKind.Object:
                    writer.WriteStartObject();
                    foreach (var property in element.EnumerateObject())
                    {
                        writer.WritePropertyName(property.Name);
                        TruncateLargeFieldsRecursive(property.Value, writer, maxFieldLength);
                    }
                    writer.WriteEndObject();
                    break;
                case JsonValueKind.Array:
                    writer.WriteStartArray();
                    foreach (var item in element.EnumerateArray())
                    { TruncateLargeFieldsRecursive(item, writer, maxFieldLength); }
                    writer.WriteEndArray();
                    break;
                case JsonValueKind.String:
                    var stringValue = element.GetString() ?? string.Empty;
                    writer.WriteStringValue(stringValue.Length > maxFieldLength ? $"{TruncateText(stringValue, maxFieldLength)}..." : stringValue);
                    break;
                default:
                    element.WriteTo(writer);
                    break;
            }
        }

        internal bool IsFileUpload(HttpRequest request)
        { return request.HasFormContentType && request.Form.Files.Any(); }

        internal bool IsJson(HttpRequest request)
        { return request.ContentType != null && request.ContentType.Contains("application/json", StringComparison.OrdinalIgnoreCase); }

        internal string TruncateText(string text, int maxLength)
        { return text.Length > maxLength ? text.Substring(0, maxLength) + "..." : text; }

        internal string SerializeDictionary<T>(T dictionary) where T : IEnumerable<KeyValuePair<string, StringValues>>
        {
            var dict = dictionary.ToDictionary(kv => kv.Key, kv => string.Join(", ", kv.Value));
            return MinifyHelper.JsonMinify(JsonSerializer.Serialize(dict));
        }
    }
}
