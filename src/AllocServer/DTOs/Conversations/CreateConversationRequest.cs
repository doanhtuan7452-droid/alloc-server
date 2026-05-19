using System.ComponentModel.DataAnnotations;

namespace AllocServer.DTOs.Conversations
{
    public class CreateConversationRequest
    {
        public int? ProjectId { get; set; }
        
        [MaxLength(100)]
        public string? Name { get; set; }
        
        [Required]
        [RegularExpression("^(Direct|Group|Project_Channel)$", ErrorMessage = "Type must be 'Direct', 'Group', or 'Project_Channel'.")]
        public string Type { get; set; } = null!;
        
        [Required]
        public List<int> WorkspaceMemberIds { get; set; } = new List<int>();
    }
}
