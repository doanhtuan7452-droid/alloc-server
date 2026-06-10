using AllocServer.DTOs.Common;
using AllocServer.Resources;
using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Localization;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace AllocServer.Exceptions
{
    public class GlobalExceptionHandler : IExceptionHandler
    {
        private readonly ILogger<GlobalExceptionHandler> _logger;
        private readonly IStringLocalizer<ExceptionsResource> _localizer;

        public GlobalExceptionHandler(ILogger<GlobalExceptionHandler> logger, IStringLocalizer<ExceptionsResource> localizer)
        {
            _logger = logger;
            _localizer = localizer;
        }

        public async ValueTask<bool> TryHandleAsync(HttpContext httpContext, Exception exception, CancellationToken cancellationToken)
        {
            // 1. Log the original exception message in English (as it came from the service layer)
            _logger.LogError(exception, "An unhandled exception occurred: {Message}", exception.Message);

            // 2. Determine HTTP Status Code
            var statusCode = exception switch
            {
                ArgumentNullException => StatusCodes.Status400BadRequest,
                ArgumentException => StatusCodes.Status400BadRequest,
                InvalidOperationException => StatusCodes.Status400BadRequest,
                UnauthorizedAccessException => StatusCodes.Status403Forbidden,
                KeyNotFoundException => StatusCodes.Status404NotFound,
                TimeoutException => StatusCodes.Status408RequestTimeout,
                OperationCanceledException => StatusCodes.Status408RequestTimeout,
                QuotaExceededException => StatusCodes.Status402PaymentRequired,
                NotImplementedException => StatusCodes.Status501NotImplemented,
                _ => StatusCodes.Status500InternalServerError
            };

            // 3. Localize the message
            var localizedMessage = _localizer[exception.Message];
            
            if (statusCode == StatusCodes.Status500InternalServerError && localizedMessage.ResourceNotFound)
            {
                localizedMessage = _localizer["InternalServerError"];
            }

            // 4. Write Response
            httpContext.Response.StatusCode = statusCode;
            var response = new ApiResponse
            {
                Message = localizedMessage.Value
            };

            await httpContext.Response.WriteAsJsonAsync(response, cancellationToken);

            return true;
        }
    }
}
