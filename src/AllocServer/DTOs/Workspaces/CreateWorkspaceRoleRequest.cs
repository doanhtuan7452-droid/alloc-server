using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace AllocServer.DTOs.Workspaces
{
    public class CreateWorkspaceRoleRequest
    {
        [Required(ErrorMessage = "RoleNameRequired")]
        [StringLength(100, ErrorMessage = "RoleNameTooLong")]
        [JsonPropertyName("roleName")]
        public string RoleName { get; set; } = string.Empty;
    }
}
