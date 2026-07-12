using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace AllocServer.DTOs.AIChat
{
    public class PythonConversationsResponse
    {
        [JsonPropertyName("total")]
        public int Total { get; set; }

        [JsonPropertyName("conversations")]
        public List<PythonConversationDto> Conversations { get; set; } = new();
    }
}
