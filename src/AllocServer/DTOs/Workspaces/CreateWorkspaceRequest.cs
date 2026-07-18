using System.ComponentModel.DataAnnotations;

namespace AllocServer.DTOs.Workspaces
{
    public class CreateWorkspaceRequest
    {
        [Required(ErrorMessage = "Tên Workspace không được để trống")]
        public string Name { get; set; }

        [Required(ErrorMessage = "Loại Workspace không được để trống")]
        public string Type { get; set; } // 'Personal', 'Company'

        [Range(0.00, 24.00, ErrorMessage = "Số giờ làm việc tiêu chuẩn phải từ 0 đến 24.")]
        public decimal? StandardHours { get; set; }
    }
}
