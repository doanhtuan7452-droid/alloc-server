namespace AllocServer.DTOs.Conversations
{
    public class ConversationListItemResponse
    {
        public int ConversationId { get; set; }
        public int WorkspaceId { get; set; }
        public int? ProjectId { get; set; }
        public string? Name { get; set; }
        public string Type { get; set; } = null!;
        public string? LastMessageContent { get; set; }
        public DateTime? LastMessageAt { get; set; }
        public int UnreadCount { get; set; }
    }
}
