using System.Text.Json.Serialization;

namespace AllocServer.DTOs.Tasks
{
    public class PagedProjectTasksResponse
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
        public List<ProjectTaskListItemResponse> Items { get; set; } = new();
    }
}
