using System.Text.Json.Serialization;

namespace AllocServer.DTOs.Workspaces
{
    public class WorkspacePermissionResponse
    {
        [JsonPropertyName("permissionId")]
        public string PermissionID { get; set; } = string.Empty;

        [JsonPropertyName("displayName")]
        public string DisplayName { get; set; } = string.Empty;
    }
}
