using DMNSN.Helpers.Minifier;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using System.Diagnostics;

namespace DMNSN.AspNetCore.Middlewares.RqRsLogging
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
            var perfTime = new Stopwatch();
            perfTime.Start();
            context.Request.EnableBuffering();

            var contentType = context.Request.ContentType ?? "";
            var requestBody = await ReadRequestBody(context.Request);
            var queryString = SerializeDictionary(context.Request.Query);
            var headers = SerializeDictionary(context.Request.Headers);
            var files = new List<RqRsFileLogModel>();
            if (context.Request.HasJsonContentType())
            {
                requestBody = MinifyHelper.JsonMinify(requestBody);
                if (options.MaxFieldLength > 0)
                { requestBody = TruncateLargeFields(requestBody, options.MaxFieldLength); }
            }
            else if (contentType == "application/xml")
            { requestBody = MinifyHelper.XmlMinify(requestBody); }
            else if (context.Request.HasFormContentType)
            {
                if (context.Request.Form.Files.Count > 0)
                {
                    foreach (var file in context.Request.Form.Files)
                    { files.Add(new RqRsFileLogModel { Name = file.FileName, Length = file.Length }); }
                }
                if (context.Request.Form.Count > 0)
                {
                    requestBody = SerializeDictionary(context.Request.Form);
                    if (options.MaxFieldLength > 0)
                    { requestBody = TruncateLargeFields(requestBody, options.MaxFieldLength); }
                }
            }
            else
            { requestBody = MinifyHelper.TextMinify(requestBody); }

            if (requestBody.Length > options.MaxRequestSizeToLog)
            { requestBody = $"Request body too large to log.[over {options.MaxRequestSizeToLog}]"; }

            logger.LogInformation("Request {Method} {Path} {ContentType} {Length}, QueryString: {QueryString}",
                context.Request.Method,
                context.Request.Path,
                context.Request.ContentType,
                context.Request.ContentLength,
                MinifyHelper.JsonMinify(queryString));
            logger.LogInformation("Headers: {Headers}", MinifyHelper.JsonMinify(headers));
            logger.LogInformation("Body: {RequestBody}", requestBody);

            if (files.Count > 0)
            {
                logger.LogInformation("Files: {Files}",
                    MinifyHelper.JsonMinify(JsonConvert.SerializeObject(files)));
            }

            var originalResponseBodyStream = context.Response.Body;
            using var responseBodyStream = new MemoryStream();
            context.Response.Body = responseBodyStream;

            await next(context);

            context.Response.Body.Seek(0, SeekOrigin.Begin);
            var responseBody = await new StreamReader(context.Response.Body).ReadToEndAsync();
            context.Response.Body.Seek(0, SeekOrigin.Begin);
            string rsBody;
            if (context.Response.ContentType == "application/json")
            { rsBody = MinifyHelper.JsonMinify(responseBody); }
            else if (context.Response.ContentType == "application/xml")
            { rsBody = MinifyHelper.XmlMinify(responseBody); }
            else { rsBody = "[Not Logging]"; }
            perfTime.Stop();

            logger.LogInformation("Response: {time} {ResponseBody}", perfTime.ElapsedMilliseconds, MinifyHelper.JsonMinify(responseBody));
            await responseBodyStream.CopyToAsync(originalResponseBodyStream);
        }
    }
}
