using DMNSN.Helpers.Minifier;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Primitives;
using System.Text;
using System.Text.Json;

namespace DMNSN.AspNetCore.Middleware.RqRsLogging
{
    /// <summary>
    /// Base class for request and response logging.
    /// </summary>
    public class RqRsLoggingBase
    {
        /// <summary>
        /// Reads the request body as a string.
        /// </summary>
        /// <param name="request">The HTTP request.</param>
        /// <returns>A task that represents the asynchronous read operation. The task result contains the request body as a string.</returns>
        internal async Task<string> ReadRequestBody(HttpRequest request)
        {
            request.Body.Position = 0;
            var reader = new StreamReader(request.Body, Encoding.UTF8, leaveOpen: true);
            var body = await reader.ReadToEndAsync();
            request.Body.Position = 0;
            return body;
        }

        /// <summary>
        /// Truncates large fields in a JSON string.
        /// </summary>
        /// <param name="jsonString">The JSON string.</param>
        /// <param name="maxFieldLength">The maximum length of a field.</param>
        /// <returns>The JSON string with large fields truncated.</returns>
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

        /// <summary>
        /// Recursively truncates large fields in a JSON element.
        /// </summary>
        /// <param name="element">The JSON element.</param>
        /// <param name="writer">The JSON writer.</param>
        /// <param name="maxFieldLength">The maximum length of a field.</param>
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

        /// <summary>
        /// Truncates the text to the specified maximum length.
        /// </summary>
        /// <param name="text">The text to truncate.</param>
        /// <param name="maxLength">The maximum length of the text.</param>
        /// <returns>The truncated text.</returns>
        internal string TruncateText(string text, int maxLength)
        { return text.Length > maxLength ? text.Substring(0, maxLength) + "..." : text; }

        /// <summary>
        /// Serializes a dictionary to a JSON string.
        /// </summary>
        /// <typeparam name="T">The type of the dictionary.</typeparam>
        /// <param name="dictionary">The dictionary to serialize.</param>
        /// <returns>The JSON string representation of the dictionary.</returns>
        internal string SerializeDictionary<T>(T dictionary) where T : IEnumerable<KeyValuePair<string, StringValues>>
        {
            var dict = dictionary.ToDictionary(kv => kv.Key, kv => string.Join(", ", kv.Value));
            return MinifyHelper.JsonMinify(JsonSerializer.Serialize(dict));
        }
    }
}
