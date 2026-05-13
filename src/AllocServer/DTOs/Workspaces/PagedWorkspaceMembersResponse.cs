using System.Text.Json.Serialization;

namespace AllocServer.DTOs.Workspaces
{
    public class PagedWorkspaceMembersResponse
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
        public List<WorkspaceMemberDetailResponse> Items { get; set; } = new();
    }
}
