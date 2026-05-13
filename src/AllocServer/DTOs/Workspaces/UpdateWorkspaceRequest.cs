using System.ComponentModel.DataAnnotations;

namespace AllocServer.DTOs.Workspaces
{
    public class UpdateWorkspaceRequest
    {
        [Required(ErrorMessage = "Tên Workspace không được để trống")]
        [StringLength(100, MinimumLength = 2, ErrorMessage = "Tên Workspace phải từ 2 đến 100 ký tự")]
        public string Name { get; set; }
    }
}
