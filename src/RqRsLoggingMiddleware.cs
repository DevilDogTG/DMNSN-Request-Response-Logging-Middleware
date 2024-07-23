using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Primitives;
using System.Text;
using System.Text.Json;

namespace DMNSN.AspNetCore.Middleware.RqRsLogging
{
    public class RqRsLoggingMiddleware
    {
        private readonly RequestDelegate next;
        private readonly ILogger<RqRsLoggingMiddleware> logger;
        private readonly RqRsLoggingMiddlewareOptions options;

        public RqRsLoggingMiddleware(RequestDelegate _next, ILogger<RqRsLoggingMiddleware> _logger, RqRsLoggingMiddlewareOptions _options)
        {
            next = _next;
            logger = _logger;
            options = _options;
        }

        public async Task InvokeAsync(HttpContext context)
        {
            context.Request.EnableBuffering();

            if (!IsFileUpload(context.Request))
            {
                var requestBody = await ReadRequestBody(context.Request);
                var queryString = SerializeDictionary(context.Request.Query);
                var headers = SerializeDictionary(context.Request.Headers);
                if (!string.IsNullOrEmpty(requestBody) || !string.IsNullOrEmpty(queryString))
                {
                    var minifiedBody = IsJson(context.Request) ? MinifyJson(requestBody) : MinifyText(requestBody);
                    logger.LogInformation("Request {Method} {Path} {ContentType} {Length}, QueryString: {QueryString}",
                        context.Request.Method,
                        context.Request.Path,
                        context.Request.ContentType,
                        context.Request.ContentLength,
                        MinifyJson(queryString));
                    logger.LogInformation("Headers: {Headers}", MinifyJson(headers));
                    logger.LogInformation("Body: {RequestBody}",
                        (minifiedBody.Length <= options.MaxRequestSizeToLog) ? minifiedBody : $"Request body too large to log.[over {options.MaxRequestSizeToLog}]");
                }
            }

            var originalResponseBodyStream = context.Response.Body;
            using var responseBodyStream = new MemoryStream();
            context.Response.Body = responseBodyStream;

            await next(context);

            context.Response.Body.Seek(0, SeekOrigin.Begin);
            var responseBody = await new StreamReader(context.Response.Body).ReadToEndAsync();
            context.Response.Body.Seek(0, SeekOrigin.Begin);

            logger.LogInformation("Response: {ResponseBody}", MinifyJson(responseBody));
            await responseBodyStream.CopyToAsync(originalResponseBodyStream);
        }

        private async Task<string> ReadRequestBody(HttpRequest request)
        {
            request.Body.Position = 0;
            var reader = new StreamReader(request.Body, Encoding.UTF8, leaveOpen: true);
            var body = await reader.ReadToEndAsync();
            request.Body.Position = 0;
            return body;
        }

        private string MinifyJson(string jsonString)
        {
            try
            {
                using var jsonDocument = JsonDocument.Parse(jsonString);
                var rootElement = jsonDocument.RootElement;
                using var memoryStream = new MemoryStream();
                using var writer = new Utf8JsonWriter(memoryStream, new JsonWriterOptions { Indented = false });

                rootElement.WriteTo(writer);

                writer.Flush();
                var json = Encoding.UTF8.GetString(memoryStream.ToArray());
                return TruncateLargeFields(json);
            }
            catch
            { return jsonString; }
        }

        private string TruncateLargeFields(string jsonString)
        {
            try
            {
                var jsonDocument = JsonDocument.Parse(jsonString);
                var rootElement = jsonDocument.RootElement;
                using var memoryStream = new MemoryStream();
                using var writer = new Utf8JsonWriter(memoryStream, new JsonWriterOptions { Indented = true });

                TruncateLargeFieldsRecursive(rootElement, writer);

                writer.Flush();
                return Encoding.UTF8.GetString(memoryStream.ToArray());
            }
            catch
            { return jsonString; }
        }

        private void TruncateLargeFieldsRecursive(JsonElement element, Utf8JsonWriter writer)
        {
            switch (element.ValueKind)
            {
                case JsonValueKind.Object:
                    writer.WriteStartObject();
                    foreach (var property in element.EnumerateObject())
                    {
                        writer.WritePropertyName(property.Name);
                        TruncateLargeFieldsRecursive(property.Value, writer);
                    }
                    writer.WriteEndObject();
                    break;
                case JsonValueKind.Array:
                    writer.WriteStartArray();
                    foreach (var item in element.EnumerateArray())
                    { TruncateLargeFieldsRecursive(item, writer); }
                    writer.WriteEndArray();
                    break;
                case JsonValueKind.String:
                    var stringValue = element.GetString() ?? string.Empty;
                    writer.WriteStringValue(stringValue.Length > options.MaxFieldLength ? $"{TruncateText(stringValue, options.MaxFieldLength)}..." : stringValue);
                    break;
                default:
                    element.WriteTo(writer);
                    break;
            }
        }

        private bool IsFileUpload(HttpRequest request)
        { return request.HasFormContentType && request.Form.Files.Any(); }

        private bool IsJson(HttpRequest request)
        { return request.ContentType != null && request.ContentType.Contains("application/json", StringComparison.OrdinalIgnoreCase); }

        private string MinifyText(string text)
        { return text.Replace("\r", "").Replace("\n", "").Replace("\t", "").Trim(); }

        private string TruncateText(string text, int maxLength)
        { return text.Length > maxLength ? text.Substring(0, maxLength) + "..." : text; }

        private string SerializeDictionary<T>(T dictionary) where T : IEnumerable<KeyValuePair<string, StringValues>>
        {
            var dict = dictionary.ToDictionary(kv => kv.Key, kv => string.Join(", ", kv.Value));
            return JsonSerializer.Serialize(dict);
        }
    }
}
