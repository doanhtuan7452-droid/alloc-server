using System.Text.Json.Serialization;

namespace AllocServer.DTOs.Common
{
    public class ApiResponse
    {
        public string Message { get; set; } = string.Empty;

        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public string? ErrorCode { get; set; }
    }
}
