using System.Text.Json.Serialization;

namespace AllocServer.DTOs.Expenses
{
    public class PagedProjectExpensesResponse
    {
        [JsonPropertyName("page")]
        public int Page { get; set; }

        [JsonPropertyName("pageSize")]
        public int PageSize { get; set; }

        [JsonPropertyName("totalItems")]
        public int TotalItems { get; set; }

        [JsonPropertyName("totalPages")]
        public int TotalPages { get; set; }

        [JsonPropertyName("items")]
        public List<ExpenseListItemResponse> Items { get; set; } = new();
    }
}
