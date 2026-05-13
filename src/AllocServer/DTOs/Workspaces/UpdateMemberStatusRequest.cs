using System.ComponentModel.DataAnnotations;

namespace AllocServer.DTOs.Workspaces
{
    public class UpdateMemberStatusRequest
    {
        [Required(ErrorMessage = "Status khong duoc de trong.")]
        [RegularExpression("^(Active|Deactivated)$", ErrorMessage = "Status chi nhan Active hoac Deactivated.")]
        public string Status { get; set; } = string.Empty;
    }
}
