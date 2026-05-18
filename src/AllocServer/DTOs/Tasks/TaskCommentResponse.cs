using System.Text.Json.Serialization;

namespace AllocServer.DTOs.Tasks
{
    public class TaskCommentResponse
    {
        [JsonPropertyName("commentId")]
        public int CommentId { get; set; }

        [JsonPropertyName("taskId")]
        public int TaskId { get; set; }

        [JsonPropertyName("memberId")]
        public int MemberId { get; set; }

        [JsonPropertyName("memberName")]
        public string MemberName { get; set; } = string.Empty;

        [JsonPropertyName("memberAvatarUrl")]
        public string? MemberAvatarUrl { get; set; }

        [JsonPropertyName("parentCommentId")]
        public int? ParentCommentId { get; set; }

        [JsonPropertyName("content")]
        public string Content { get; set; } = string.Empty;

        [JsonPropertyName("createdAt")]
        public DateTime CreatedAt { get; set; }

        [JsonPropertyName("updatedAt")]
        public DateTime? UpdatedAt { get; set; }

        [JsonPropertyName("replies")]
        public List<TaskCommentResponse> Replies { get; set; } = new();
    }
}
