using DMNSN.Helpers.Minifier;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Primitives;
using System.Text;
using System.Text.Json;

namespace DMNSN.AspNetCore.Middleware.RqRsLogging
{
    public class RqRsLoggingMiddleware : RqRsLoggingBase
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
                    var minifiedBody = IsJson(context.Request) ? MinifyHelper.JsonMinify(requestBody) : MinifyHelper.TextMinify(requestBody);
                    minifiedBody = TruncateLargeFields(minifiedBody, options.MaxFieldLength);
                    logger.LogInformation("Request {Method} {Path} {ContentType} {Length}, QueryString: {QueryString}",
                        context.Request.Method,
                        context.Request.Path,
                        context.Request.ContentType,
                        context.Request.ContentLength,
                        MinifyHelper.JsonMinify(queryString));
                    logger.LogInformation("Headers: {Headers}", MinifyHelper.JsonMinify(headers));
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

            logger.LogInformation("Response: {ResponseBody}", MinifyHelper.JsonMinify(responseBody));
            await responseBodyStream.CopyToAsync(originalResponseBodyStream);
        }
    }
}
