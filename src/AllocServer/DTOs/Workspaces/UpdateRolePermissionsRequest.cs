using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace AllocServer.DTOs.Workspaces
{
    public class UpdateRolePermissionsRequest
    {
        [Required(ErrorMessage = "PermissionIdsRequired")]
        [JsonPropertyName("permissionIds")]
        public List<string> PermissionIds { get; set; } = new();
    }
}
