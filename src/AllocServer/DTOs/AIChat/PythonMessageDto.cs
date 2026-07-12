using System;
using System.Collections.Generic;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace AllocServer.DTOs.AIChat
{
    public class PythonMessageDto
    {
        [JsonPropertyName("id")]
        public string Id { get; set; } = string.Empty;

        [JsonPropertyName("conversation_id")]
        public string ConversationId { get; set; } = string.Empty;

        [JsonPropertyName("role")]
        public string Role { get; set; } = string.Empty;

        [JsonPropertyName("content")]
        public string Content { get; set; } = string.Empty;

        [JsonPropertyName("timestamp")]
        public DateTime Timestamp { get; set; }

        [JsonPropertyName("tokens_count")]
        public int? TokensCount { get; set; }

        [JsonPropertyName("attachments")]
        public List<PythonAttachmentDto>? Attachments { get; set; }

        [JsonPropertyName("rag_sources")]
        public List<string>? RagSources { get; set; }

        [JsonPropertyName("metadata")]
        public JsonElement? Metadata { get; set; }
    }
}
