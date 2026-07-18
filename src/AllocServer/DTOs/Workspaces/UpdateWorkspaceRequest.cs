using System.ComponentModel.DataAnnotations;

namespace AllocServer.DTOs.Workspaces
{
    public class UpdateWorkspaceRequest
    {
        [Required(ErrorMessage = "Tên Workspace không được để trống")]
        [StringLength(100, MinimumLength = 2, ErrorMessage = "Tên Workspace phải từ 2 đến 100 ký tự")]
        public string Name { get; set; }

        [Range(0.00, 24.00, ErrorMessage = "Số giờ làm việc tiêu chuẩn phải từ 0 đến 24.")]
        public decimal? StandardHours { get; set; }
    }
}
