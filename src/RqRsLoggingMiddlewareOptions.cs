namespace DMNSN.AspNetCore.Middleware.RqRsLogging
{
    public class RqRsLoggingMiddlewareOptions
    {
        public string CorrelationKey { get; set; } = "X-Correlation-ID";
        public bool LogRequest { get; set; } = true;
        public bool LogResponse { get; set; } = true;
        public int MaxFieldLength { get; set; } = 100;
        public int MaxRequestSizeToLog { get; set; } = 1024;
        public int MaxQueryString { get; set; } = 100;
    }
}
