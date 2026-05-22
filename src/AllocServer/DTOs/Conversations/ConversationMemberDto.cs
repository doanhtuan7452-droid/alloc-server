namespace AllocServer.DTOs.Conversations
{
    public class ConversationMemberDto
    {
        public int WorkspaceMemberId { get; set; }
        public int ResourceId { get; set; }
        public string FullName { get; set; } = null!;
        public string? AvatarUrl { get; set; }
        public DateTime JoinedAt { get; set; }
        public DateTime LastReadAt { get; set; }
    }
}
