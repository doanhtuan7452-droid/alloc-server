using System.ComponentModel.DataAnnotations;

namespace AllocServer.DTOs.Workspaces
{
    public class InviteWorkspaceMemberRequest
    {
        [Required(ErrorMessage = "Email khong duoc de trong.")]
        [EmailAddress(ErrorMessage = "Email khong hop le.")]
        [StringLength(100, ErrorMessage = "Email khong duoc vuot qua 100 ky tu.")]
        public string Email { get; set; } = string.Empty;

        [Range(1, int.MaxValue, ErrorMessage = "WorkspaceRoleID khong hop le.")]
        public int WorkspaceRoleID { get; set; }

        [Range(0, double.MaxValue, ErrorMessage = "Luong co ban khong duoc am.")]
        public decimal? BaseSalaryMonth { get; set; }

        [Range(0, double.MaxValue, ErrorMessage = "Rate tang ca khong duoc am.")]
        public decimal? OTRatePerHour { get; set; }
    }
}
