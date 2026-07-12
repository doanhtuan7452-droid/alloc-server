using System;
using System.Text.Json.Serialization;

namespace AllocServer.DTOs.AIChat
{
    public class CreateProjectToolPayload
    {
        [JsonPropertyName("workspaceId")]
        public int WorkspaceId { get; set; }

        [JsonPropertyName("userId")]
        public int UserId { get; set; }

        [JsonPropertyName("projectName")]
        public string ProjectName { get; set; } = string.Empty;

        [JsonPropertyName("expectedBudget")]
        public decimal ExpectedBudget { get; set; }

        [JsonPropertyName("startDate")]
        public DateOnly? StartDate { get; set; }

        [JsonPropertyName("endDate")]
        public DateOnly? EndDate { get; set; }

        [JsonPropertyName("originalCurrencyCode")]
        public string OriginalCurrencyCode { get; set; } = "USD";

        [JsonPropertyName("exchangeRateToUSD")]
        public decimal ExchangeRateToUSD { get; set; } = 1.0m;

        [JsonPropertyName("methodology")]
        public string Methodology { get; set; } = "Agile";
    }
}
