using System.Text.Json.Serialization;

namespace AllocServer.DTOs.Revenues
{
    public class RevenueListItemResponse
    {
        [JsonPropertyName("revenueId")]
        public int RevenueId { get; set; }

        [JsonPropertyName("projectId")]
        public int ProjectId { get; set; }

        [JsonPropertyName("projectName")]
        public string ProjectName { get; set; } = string.Empty;

        [JsonPropertyName("type")]
        public string Type { get; set; } = string.Empty;

        [JsonPropertyName("amount")]
        public decimal Amount { get; set; }

        [JsonPropertyName("expectedDate")]
        public DateOnly? ExpectedDate { get; set; }

        [JsonPropertyName("status")]
        public string Status { get; set; } = string.Empty;
    }
}
