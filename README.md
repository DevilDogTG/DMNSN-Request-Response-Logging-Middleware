# Request and Response Logging Middleware

This repository contains middleware for logging HTTP requests and responses in an ASP.NET Core application. The middleware provides detailed logging of request and response data, including headers, body, and performance metrics.

## Installation

To install the middleware, add the following NuGet package to your project:

```bash
dotnet add package DMNSN.AspNetCore.Middleware.RqRsLogging
```

## Usage

To use the middleware, add it to the `IApplicationBuilder` in the `Startup.cs` file:

```csharp
public void Configure(IApplicationBuilder app, IWebHostEnvironment env)
{
    app.UseRqRsLoggingMiddleware();
    // or
    app.UseRqRsSingleLoggingMiddleware();
    // Other middlewares
}
```

You can also configure the middleware using an `IConfiguration` instance:

```csharp
public void Configure(IApplicationBuilder app, IWebHostEnvironment env, IConfiguration configuration)
{
    app.UseRqRsLoggingMiddleware(configuration);
    // or
    app.UseRqRsSingleLoggingMiddleware(configuration);
    // Other middlewares
}
```

Alternatively, you can pass custom options:

```csharp
public void Configure(IApplicationBuilder app, IWebHostEnvironment env)
{
    var options = new RqRsLoggingMiddlewareOptions {
        CorrelationKey = "X-Custom-Correlation-ID",
        LogRequest = true,
        LogResponse = true,
        MaxFieldLength = 200,
        MaxRequestSizeToLog = 2048,
        MaxQueryString = 200
    };
    app.UseRqRsLoggingMiddleware(options);
    // or
    app.UseRqRsSingleLoggingMiddleware(options);

    // Other middlewares
}
```

## Configuration

The middleware can be configured using the `RqRsLoggingMiddlewareOptions` class. The following options are available:

- `CorrelationKey`: The key used to retrieve the correlation ID from the request headers.
- `LogRequest`: Whether to log the request data.
- `LogResponse`: Whether to log the response data.
- `MaxFieldLength`: The maximum length of a field before it is truncated.
- `MaxRequestSizeToLog`: The maximum size of the request body to log.
- `MaxQueryString`: The maximum length of the query string to log.

## License

This project is licensed under the MIT License. See the [LICENSE](LICENSE) file for more information.