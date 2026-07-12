using System;

namespace AllocServer.Models.AI
{
    public class AIToolResult
    {
        public bool IsSuccess { get; set; }
        public int StatusCode { get; set; }
        public object? Data { get; set; }
        public string? ErrorCode { get; set; }
        public string? ErrorMessage { get; set; }

        public static AIToolResult Success(object? data = null)
        {
            return new AIToolResult
            {
                IsSuccess = true,
                StatusCode = 200,
                Data = data
            };
        }

        public static AIToolResult Failure(int statusCode, string errorCode, string errorMessage)
        {
            return new AIToolResult
            {
                IsSuccess = false,
                StatusCode = statusCode,
                ErrorCode = errorCode,
                ErrorMessage = errorMessage
            };
        }
    }
}
