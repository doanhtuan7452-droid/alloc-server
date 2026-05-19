using System.ComponentModel.DataAnnotations;

namespace AllocServer.DTOs.Conversations
{
    public class AddConversationMembersRequest
    {
        [Required(ErrorMessage = "Danh sách thành viên không được để trống.")]
        [MinLength(1, ErrorMessage = "Danh sách thành viên không được để trống.")]
        public List<int> WorkspaceMemberIds { get; set; } = new List<int>();
    }
}
