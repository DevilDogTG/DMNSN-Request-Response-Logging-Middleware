namespace DMNSN.AspNetCore.Middleware.RqRsLogging
{
    public class RqRsLogModel
    {
        public string CorrelationID { get; set; } = string.Empty;
        public long ResponseTime { get; set; }
        public int ResponseCode { get; set; }
        public DateTime RqDateTime { get; set; }
        public DateTime RsDateTime { get; set; }
        public string Method { get; set; } = string.Empty;
        public string Scheme { get; set; } = string.Empty;
        public string Protocol { get; set; } = string.Empty;
        public string Path { get; set; } = string.Empty;
        public string ContentType { get; set; } = string.Empty;
        public long ContentLength { get; set; }
        public string QueryString { get; set; } = string.Empty;
        public string RqHeaders { get; set; } = string.Empty;
        public string RqBody { get; set; } = string.Empty;
        public List<RqRsFileLogModel> RqFiles { get; set; } = [];
        public string RsHeaders { get; set; } = string.Empty;
        public string RsBody { get; set; } = string.Empty;
        public int StatusCode { get; set; }
    }

    public class RqRsFileLogModel
    {
        public string Name { get; set; } = string.Empty;
        public long Length { get; set; }
    }
}
