namespace AllocServer.DTOs.Conversations
{
    public class ConversationDetailResponse
    {
        public int ConversationId { get; set; }
        public int WorkspaceId { get; set; }
        public int? ProjectId { get; set; }
        public string? Name { get; set; }
        public string Type { get; set; } = null!;
        public DateTime CreatedAt { get; set; }

        public List<ConversationMemberDto> Members { get; set; } = new List<ConversationMemberDto>();
    }

}
