using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace AllocServer.DTOs.Tasks
{
    public class CreateTaskCommentRequest
    {
        [Required]
        [JsonPropertyName("content")]
        public string Content { get; set; } = string.Empty;

        [JsonPropertyName("parentCommentId")]
        public int? ParentCommentId { get; set; }
    }
}
