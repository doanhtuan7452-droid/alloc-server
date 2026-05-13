using System.Text.Json.Serialization;

namespace AllocServer.DTOs.Timesheets
{
    public class PagedTimesheetsResponse
    {
        [JsonPropertyName("page")]
        public int Page { get; set; }

        [JsonPropertyName("pageSize")]
        public int PageSize { get; set; }

        [JsonPropertyName("totalItems")]
        public int TotalItems { get; set; }

        [JsonPropertyName("totalPages")]
        public int TotalPages { get; set; }

        [JsonPropertyName("fromDate")]
        public DateOnly FromDate { get; set; }

        [JsonPropertyName("toDate")]
        public DateOnly ToDate { get; set; }

        [JsonPropertyName("items")]
        public List<TimesheetListItemResponse> Items { get; set; } = new();
    }
}
