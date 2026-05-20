namespace AllocServer.DTOs.Messages
{
    public class MessageResponse
    {
        public int MessageId { get; set; }
        public int ConversationId { get; set; }
        public int SenderId { get; set; }
        public string? SenderName { get; set; }
        public string? SenderAvatarUrl { get; set; }
        public string? Content { get; set; }
        public DateTime CreatedAt { get; set; }
        public bool IsEdited { get; set; }
        public bool IsDeleted { get; set; }
        public List<AssetResponse>? Assets { get; set; }
    }
}
