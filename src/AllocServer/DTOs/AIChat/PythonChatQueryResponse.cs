using System.Text.Json.Serialization;

namespace AllocServer.DTOs.AIChat
{
    public class PythonChatQueryResponse
    {
        [JsonPropertyName("response")]
        public string Response { get; set; } = string.Empty;

        [JsonPropertyName("conversation_id")]
        public string ConversationId { get; set; } = string.Empty;

        [JsonPropertyName("status")]
        public string Status { get; set; } = string.Empty;

        [JsonPropertyName("usage")]
        public PythonChatUsageDto? Usage { get; set; }

        [JsonPropertyName("title_status")]
        public string? TitleStatus { get; set; }

        [JsonPropertyName("remaining_quota")]
        public int? RemainingQuota { get; set; }
    }

    public class PythonChatUsageDto
    {
        [JsonPropertyName("prompt_tokens")]
        public int PromptTokens { get; set; }

        [JsonPropertyName("completion_tokens")]
        public int CompletionTokens { get; set; }

        [JsonPropertyName("total_tokens")]
        public int TotalTokens { get; set; }
    }
}
