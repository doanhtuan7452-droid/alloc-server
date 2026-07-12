using System;

namespace AllocServer.Models.AI
{
    public class GuardValidationResult
    {
        public bool IsPassed { get; set; }
        public string? ErrorCode { get; set; }
        public string? ErrorMessage { get; set; }
        public int StatusCode { get; set; } = 400;
        public object? CachedResult { get; set; } // If idempotency key is matched, return cached JSON directly

        public static GuardValidationResult Pass()
        {
            return new GuardValidationResult { IsPassed = true };
        }

        public static GuardValidationResult Fail(string errorCode, string errorMessage, int statusCode = 400)
        {
            return new GuardValidationResult
            {
                IsPassed = false,
                ErrorCode = errorCode,
                ErrorMessage = errorMessage,
                StatusCode = statusCode
            };
        }

        public static GuardValidationResult HitIdempotency(object cachedResult)
        {
            return new GuardValidationResult
            {
                IsPassed = true,
                CachedResult = cachedResult
            };
        }
    }
}
