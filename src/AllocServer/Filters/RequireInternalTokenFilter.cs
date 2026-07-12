using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.Extensions.Configuration;

namespace AllocServer.Filters
{
    public class RequireInternalTokenFilter : IAsyncActionFilter
    {
        private readonly IConfiguration _configuration;

        public RequireInternalTokenFilter(IConfiguration configuration)
        {
            _configuration = configuration;
        }

        public async Task OnActionExecutionAsync(ActionExecutingContext context, ActionExecutionDelegate next)
        {
            var settings = _configuration.GetSection("PythonServiceSettings");
            var internalSection = settings.GetSection("Internal");
            
            var headerName = internalSection["HeaderName"] ?? settings["HeaderName"] ?? "X-Internal-Token";
            var expectedSecret = internalSection["Secret"] ?? settings["Secret"];

            if (string.IsNullOrEmpty(expectedSecret))
            {
                context.Result = new ObjectResult(new { message = "Cấu hình bảo mật hệ thống bị thiếu." })
                {
                    StatusCode = StatusCodes.Status500InternalServerError
                };
                return;
            }

            if (!context.HttpContext.Request.Headers.TryGetValue(headerName, out var actualSecretValues) 
                || string.IsNullOrEmpty(actualSecretValues.ToString())
                || !string.Equals(actualSecretValues.ToString(), expectedSecret, StringComparison.Ordinal))
            {
                context.Result = new UnauthorizedObjectResult(new { message = "Yêu cầu không được xác thực nội bộ hợp lệ." });
                return;
            }

            await next();
        }
    }
}
