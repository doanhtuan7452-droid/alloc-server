using System.ComponentModel.DataAnnotations;

namespace AllocServer.DTOs.Workspaces
{
    public class UpdateMemberRoleRequest
    {
        [Range(1, int.MaxValue, ErrorMessage = "WorkspaceRoleID không hợp lệ.")]
        public int WorkspaceRoleID { get; set; }
    }
}
