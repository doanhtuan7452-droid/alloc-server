using AllocServer.DTOs.Common;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using System.Threading.Tasks;

namespace AllocServer.Middleware
{
    /// <summary>
    /// Middleware validating that requests to the Rest API contain a matching test token header,
    /// used to restrict unauthorized machine access in staging/development environments.
    /// </summary>
    public class TestTokenValidationMiddleware
    {
        private readonly RequestDelegate _next;
        private readonly ILogger<TestTokenValidationMiddleware> _logger;
        private readonly string _headerName;
        private readonly string _secretToken;

        public TestTokenValidationMiddleware(RequestDelegate next, IConfiguration configuration, ILogger<TestTokenValidationMiddleware> logger)
        {
            _next = next;
            _logger = logger;

            var section = configuration.GetSection("TestTokenSettings");
            _headerName = section.GetValue<string>("HeaderName") ?? "X-Alloc-Test-Token";
            _secretToken = section.GetValue<string>("SecretToken") ?? string.Empty;
        }

        public async Task InvokeAsync(HttpContext context)
        {
            // Bypass OPTIONS preflight requests to prevent blocking browser CORS checks
            if (HttpMethods.IsOptions(context.Request.Method))
            {
                await _next(context);
                return;
            }

            // Only enforce validation on API endpoints (starting with /api/)
            // This prevents blocking Swagger UI, SignalR hubs, root check-alive probes, and health checks.
            var path = context.Request.Path.Value ?? string.Empty;
            if (!path.StartsWith("/api/", System.StringComparison.OrdinalIgnoreCase))
            {
                await _next(context);
                return;
            }

            if (string.IsNullOrEmpty(_secretToken))
            {
                _logger.LogWarning("TestTokenSettings:SecretToken is empty or not configured. Test token validation fails open-close (access is forbidden).");
                
                context.Response.StatusCode = StatusCodes.Status403Forbidden;
                context.Response.ContentType = "application/json";
                
                var errorResponse = new ApiResponse
                {
                    Message = "Forbidden: Test token configuration is invalid.",
                    ErrorCode = "TEST_TOKEN_CONFIG_ERROR"
                };
                
                await context.Response.WriteAsJsonAsync(errorResponse);
                return;
            }

            if (!context.Request.Headers.TryGetValue(_headerName, out var extractedToken) || extractedToken != _secretToken)
            {
                _logger.LogWarning("Unauthorized test API access attempt. IP: {IP}, Method: {Method}, Path: {Path}, Missing or invalid header: {HeaderName}",
                    context.Connection.RemoteIpAddress,
                    context.Request.Method,
                    context.Request.Path,
                    _headerName);

                context.Response.StatusCode = StatusCodes.Status403Forbidden;
                context.Response.ContentType = "application/json";

                var errorResponse = new ApiResponse
                {
                    Message = "Forbidden: Invalid or missing test token header.",
                    ErrorCode = "INVALID_TEST_TOKEN"
                };

                await context.Response.WriteAsJsonAsync(errorResponse);
                return;
            }

            await _next(context);
        }
    }
}
