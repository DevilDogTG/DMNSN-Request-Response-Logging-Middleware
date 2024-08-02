using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.Configuration;

namespace DMNSN.AspNetCore.Middleware.RqRsLogging
{
    public static class RqRsLoggingMiddlewareExtensions
    {
        private const string DefaultSection = "DMNSN:Middlewares:RqRsLogging";
        public static IApplicationBuilder UseRqRsLoggingMiddleware(this IApplicationBuilder builder)
        {
            return builder.UseMiddleware<RqRsLoggingMiddleware>(new RqRsLoggingMiddlewareOptions());
        }
        public static IApplicationBuilder UseRqRsLoggingMiddleware(this IApplicationBuilder builder, IConfiguration configuration)
        {
            // Attempt to read the configuration section
            var options = configuration.GetSection(DefaultSection).Get<RqRsLoggingMiddlewareOptions>() ?? new RqRsLoggingMiddlewareOptions();
            return builder.UseMiddleware<RqRsLoggingMiddleware>(options);
        }
        public static IApplicationBuilder UseRqRsLoggingMiddleware(this IApplicationBuilder builder, RqRsLoggingMiddlewareOptions options)
        {
            return builder.UseMiddleware<RqRsLoggingMiddleware>(options);
        }

        public static IApplicationBuilder UseRqRsSingleLoggingMiddleware(this IApplicationBuilder builder)
        {
            return builder.UseMiddleware<RqRsSingleLoggingMiddleware>(new RqRsLoggingMiddlewareOptions());
        }
        public static IApplicationBuilder UseRqRsSingleLoggingMiddleware(this IApplicationBuilder builder, IConfiguration configuration)
        {
            // Attempt to read the configuration section
            var options = configuration.GetSection(DefaultSection).Get<RqRsLoggingMiddlewareOptions>() ?? new RqRsLoggingMiddlewareOptions();
            return builder.UseMiddleware<RqRsSingleLoggingMiddleware>(options);
        }
        public static IApplicationBuilder UseRqRsSingleLoggingMiddleware(this IApplicationBuilder builder, RqRsLoggingMiddlewareOptions options)
        {
            return builder.UseMiddleware<RqRsSingleLoggingMiddleware>(options);
        }
    }
}
