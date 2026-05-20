using System.ComponentModel.DataAnnotations;

namespace AllocServer.DTOs.Messages
{
    public class UpdateMessageRequest
    {
        [Required]
        [MaxLength(4000)]
        public string Content { get; set; } = string.Empty;
    }
}
