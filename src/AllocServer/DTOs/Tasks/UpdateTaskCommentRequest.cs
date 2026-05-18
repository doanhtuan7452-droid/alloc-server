using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace AllocServer.DTOs.Tasks
{
    public class UpdateTaskCommentRequest
    {
        [Required]
        [JsonPropertyName("content")]
        public string Content { get; set; } = string.Empty;
    }
}
