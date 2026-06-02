using System.Text.Json.Serialization;

namespace AllocServer.DTOs.Workspaces
{
    public class ProjectDetailResponse
    {
        [JsonPropertyName("projectId")]
        public int ProjectID { get; set; }

        [JsonPropertyName("workspaceId")]
        public int WorkspaceID { get; set; }

        [JsonPropertyName("projectName")]
        public string ProjectName { get; set; } = null!;

        [JsonPropertyName("expectedBudget")]
        public decimal ExpectedBudget { get; set; }

        [JsonPropertyName("totalRevenue")]
        public decimal TotalRevenue { get; set; }

        [JsonPropertyName("startDate")]
        public DateOnly StartDate { get; set; }

        [JsonPropertyName("endDate")]
        public DateOnly EndDate { get; set; }

        [JsonPropertyName("status")]
        public string Status { get; set; } = null!;

        [JsonPropertyName("baselineData")]
        public string? BaselineData { get; set; }

        [JsonPropertyName("originalCurrencyCode")]
        public string OriginalCurrencyCode { get; set; } = null!;

        [JsonPropertyName("exchangeRateToUSD")]
        public decimal ExchangeRateToUSD { get; set; }

        [JsonPropertyName("methodology")]
        public string Methodology { get; set; } = null!;

        [JsonPropertyName("createdAt")]
        public DateTime CreatedAt { get; set; }
    }
}
