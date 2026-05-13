using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace AllocServer.DTOs.Expenses
{
    public class GetProjectExpensesQuery
    {
        [Range(1, int.MaxValue)]
        [JsonPropertyName("page")]
        public int Page { get; set; } = 1;

        [Range(1, 100)]
        [JsonPropertyName("pageSize")]
        public int PageSize { get; set; } = 20;

        [JsonPropertyName("category")]
        public string? Category { get; set; }

        [JsonPropertyName("fromDate")]
        public DateOnly? FromDate { get; set; }

        [JsonPropertyName("toDate")]
        public DateOnly? ToDate { get; set; }

        [JsonPropertyName("minAmount")]
        public decimal? MinAmount { get; set; }

        [JsonPropertyName("maxAmount")]
        public decimal? MaxAmount { get; set; }

        [JsonPropertyName("search")]
        public string? Search { get; set; }
    }
}
