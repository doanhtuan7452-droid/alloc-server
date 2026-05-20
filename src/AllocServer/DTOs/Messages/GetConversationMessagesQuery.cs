using System.ComponentModel.DataAnnotations;

namespace AllocServer.DTOs.Messages
{
    public class GetConversationMessagesQuery
    {
        [Range(1, 100)]
        public int PageSize { get; set; } = 20;

        [Range(1, int.MaxValue)]
        public int? BeforeMessageId { get; set; }

        public string? Keyword { get; set; }
    }
}
