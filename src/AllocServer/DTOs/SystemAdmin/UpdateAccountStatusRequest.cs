using System.ComponentModel.DataAnnotations;

namespace AllocServer.DTOs.SystemAdmin
{
    public class UpdateAccountStatusRequest
    {
        [Required]
        [RegularExpression("Active|Locked|Suspended", ErrorMessage = "Trạng thái tài khoản không hợp lệ.")]
        public string Status { get; set; } = string.Empty;
    }
}
