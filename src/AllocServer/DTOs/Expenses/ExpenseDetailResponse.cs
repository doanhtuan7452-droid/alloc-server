using System.Text.Json.Serialization;

namespace AllocServer.DTOs.Expenses
{
    public class ExpenseDetailResponse
    {
        [JsonPropertyName("expenseId")]
        public int ExpenseId { get; set; }

        [JsonPropertyName("projectId")]
        public int ProjectId { get; set; }

        [JsonPropertyName("projectName")]
        public string ProjectName { get; set; } = string.Empty;

        [JsonPropertyName("category")]
        public string Category { get; set; } = string.Empty;

        [JsonPropertyName("amount")]
        public decimal Amount { get; set; }

        [JsonPropertyName("expenseDate")]
        public DateOnly ExpenseDate { get; set; }

        [JsonPropertyName("description")]
        public string? Description { get; set; }
    }
}
