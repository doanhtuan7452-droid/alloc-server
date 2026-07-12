using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace AllocServer.DTOs.AIChat
{
    public class PythonChatQueryRequest
    {
        [Required]
        [JsonPropertyName("workspaceId")]
        public int WorkspaceId { get; set; }

        [JsonPropertyName("conversation_id")]
        public string? ConversationId { get; set; }

        [Required]
        [JsonPropertyName("message")]
        public string Message { get; set; } = string.Empty;

        [JsonPropertyName("attachments")]
        public List<PythonAttachmentDto>? Attachments { get; set; }

        [JsonPropertyName("provider")]
        public string? Provider { get; set; }

        [JsonPropertyName("model")]
        public string? Model { get; set; }

        [JsonPropertyName("temperature")]
        public float? Temperature { get; set; }

        [JsonPropertyName("force_new")]
        public bool? ForceNew { get; set; }

        [JsonPropertyName("dynamic_tools_metadata")]
        public Dictionary<string, object>? DynamicToolsMetadata { get; set; }
    }
}
