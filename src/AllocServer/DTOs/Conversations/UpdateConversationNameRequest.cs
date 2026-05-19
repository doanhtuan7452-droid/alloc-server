using System.ComponentModel.DataAnnotations;

namespace AllocServer.DTOs.Conversations
{
    public class UpdateConversationNameRequest
    {
        [Required(ErrorMessage = "Tên nhóm không được để trống.")]
        [MaxLength(100, ErrorMessage = "Tên nhóm không được vượt quá 100 ký tự.")]
        public string Name { get; set; } = string.Empty;
    }
}
