using System.Text.Json.Serialization;

namespace AllocServer.DTOs.AIChat
{
    public class GetWorkspaceProjectsToolPayload
    {
        [JsonPropertyName("workspaceId")]
        public int WorkspaceId { get; set; }

        [JsonPropertyName("userId")]
        public int UserId { get; set; }

        [JsonPropertyName("status")]
        public string? Status { get; set; }

        [JsonPropertyName("limit")]
        public int Limit { get; set; } = 10;

        [JsonPropertyName("skip")]
        public int Skip { get; set; } = 0;

        [JsonPropertyName("sortBy")]
        public string? SortBy { get; set; } = "newest";
    }
}
