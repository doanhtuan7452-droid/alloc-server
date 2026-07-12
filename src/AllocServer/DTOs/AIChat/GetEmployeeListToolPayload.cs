using System.Text.Json.Serialization;

namespace AllocServer.DTOs.AIChat
{
    public class GetEmployeeListToolPayload
    {
        [JsonPropertyName("workspaceId")]
        public int? WorkspaceId { get; set; }

        [JsonPropertyName("projectId")]
        public int? ProjectId { get; set; }

        [JsonPropertyName("userId")]
        public int UserId { get; set; }
    }
}
