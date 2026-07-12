using System.Text.Json.Serialization;

namespace AllocServer.DTOs.AIChat
{
    public class GetProjectInfoToolPayload
    {
        [JsonPropertyName("projectId")]
        public int ProjectId { get; set; }

        [JsonPropertyName("userId")]
        public int UserId { get; set; }
    }
}
