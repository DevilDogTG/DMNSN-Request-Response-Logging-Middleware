using DMNSN.Helpers.Minifier;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using System.Diagnostics;

namespace DMNSN.AspNetCore.Middleware.RqRsLogging
{
    public class RqRsSingleLoggingMiddleware : RqRsLoggingBase
    {
        private readonly RequestDelegate next;
        private readonly ILogger<RqRsSingleLoggingMiddleware> logger;
        private readonly RqRsLoggingMiddlewareOptions options;

        public RqRsSingleLoggingMiddleware(RequestDelegate _next, ILogger<RqRsSingleLoggingMiddleware> _logger, RqRsLoggingMiddlewareOptions _options)
        {
            next = _next;
            logger = _logger;
            options = _options;
        }

        public async Task InvokeAsync(HttpContext context)
        {
            var perfTime = new Stopwatch();
            perfTime.Start();
            var log = new RqRsLogModel()
            {
                RqDateTime = DateTime.Now,
                Method = context.Request.Method,
                Scheme = context.Request.Scheme,
                Protocol = context.Request.Protocol,
                Path = context.Request.Path,
                ContentType = context.Request.ContentType ?? "",
                ContentLength = context.Request.ContentLength ?? 0
            };
            context.Request.EnableBuffering();

            var correlationId = context.Request.Headers[options.CorrelationKey];
            if (string.IsNullOrEmpty(correlationId)) { correlationId = ""; }

            var contentType = log.ContentType.ToLower();
            log.QueryString = SerializeDictionary(context.Request.Query);
            log.RqHeaders = SerializeDictionary(context.Request.Headers);
            var rqBody = await ReadRequestBody(context.Request);
            if (context.Request.HasJsonContentType())
            {
                rqBody = MinifyHelper.JsonMinify(rqBody);
                if (options.MaxFieldLength > 0)
                { rqBody = TruncateLargeFields(rqBody, options.MaxFieldLength); }
            }
            else if (contentType == "application/xml")
            { rqBody = MinifyHelper.XmlMinify(rqBody); }
            else if (context.Request.HasFormContentType)
            {
                if (context.Request.Form.Files.Count > 0)
                {
                    log.RqFiles = new List<RqRsFileLogModel>();
                    foreach (var file in context.Request.Form.Files)
                    { log.RqFiles.Add(new RqRsFileLogModel { Name = file.FileName, Length = file.Length }); }
                }
                if (context.Request.Form.Count > 0)
                {
                    rqBody = SerializeDictionary(context.Request.Form);
                    if (options.MaxFieldLength > 0)
                    { rqBody = TruncateLargeFields(rqBody, options.MaxFieldLength); }
                }
            }
            else
            { rqBody = MinifyHelper.TextMinify(rqBody); }

            if (rqBody.Length <= options.MaxRequestSizeToLog) { log.RqBody = rqBody; }
            else { log.RqBody = $"Request body too large to log.[over {options.MaxRequestSizeToLog}]"; }

            var originalResponseBodyStream = context.Response.Body;
            using var responseBodyStream = new MemoryStream();
            context.Response.Body = responseBodyStream;

            await next(context);

            context.Response.Body.Seek(0, SeekOrigin.Begin);
            var responseBody = await new StreamReader(context.Response.Body).ReadToEndAsync();
            context.Response.Body.Seek(0, SeekOrigin.Begin);
            log.StatusCode = context.Response.StatusCode;
            log.RsHeaders = SerializeDictionary(context.Response.Headers);

            if (options.MaxFieldLength > 0)
            {
                log.QueryString = TruncateLargeFields(log.QueryString, options.MaxFieldLength);
                log.RqHeaders = TruncateLargeFields(log.RqHeaders, options.MaxFieldLength);
                log.RsHeaders = TruncateLargeFields(log.RsHeaders, options.MaxFieldLength);
            }
            if (context.Response.ContentType == "application/json")
            { log.RsBody = MinifyHelper.JsonMinify(responseBody); }
            else if (context.Response.ContentType == "application/xml")
            { log.RsBody = MinifyHelper.XmlMinify(responseBody); }
            perfTime.Stop();
            log.ResponseTime = perfTime.ElapsedMilliseconds;
            log.RsDateTime = DateTime.Now;

            await responseBodyStream.CopyToAsync(originalResponseBodyStream);
        }
    }
}
