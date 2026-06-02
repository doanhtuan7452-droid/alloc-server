using System.Text.Json.Serialization;

namespace AllocServer.DTOs.Workspaces
{
    public class WorkspaceProjectListItemResponse
    {
        [JsonPropertyName("projectId")]
        public int ProjectID { get; set; }

        [JsonPropertyName("projectName")]
        public string ProjectName { get; set; } = string.Empty;

        [JsonPropertyName("expectedBudget")]
        public decimal ExpectedBudget { get; set; }

        [JsonPropertyName("totalRevenue")]
        public decimal TotalRevenue { get; set; }

        [JsonPropertyName("startDate")]
        public DateOnly StartDate { get; set; }

        [JsonPropertyName("endDate")]
        public DateOnly EndDate { get; set; }

        [JsonPropertyName("status")]
        public string Status { get; set; } = string.Empty;

        [JsonPropertyName("originalCurrencyCode")]
        public string OriginalCurrencyCode { get; set; } = string.Empty;

        [JsonPropertyName("exchangeRateToUSD")]
        public decimal ExchangeRateToUSD { get; set; }

        [JsonPropertyName("methodology")]
        public string Methodology { get; set; } = string.Empty;

        [JsonPropertyName("createdAt")]
        public DateTime CreatedAt { get; set; }
    }
}
