using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace AllocServer.DTOs.AIChat
{
    public class PythonMessagesResponse
    {
        [JsonPropertyName("conversation_id")]
        public string ConversationId { get; set; } = string.Empty;

        [JsonPropertyName("title")]
        public string Title { get; set; } = string.Empty;

        [JsonPropertyName("summary")]
        public string? Summary { get; set; }

        [JsonPropertyName("total")]
        public int Total { get; set; }

        [JsonPropertyName("messages")]
        public List<PythonMessageDto> Messages { get; set; } = new();
    }
}
