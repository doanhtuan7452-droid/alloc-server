using System.Text.Json.Serialization;

namespace AllocServer.DTOs.Workspaces
{
    public class WorkspaceRoleSummaryResponse
    {
        [JsonPropertyName("workspaceRoleId")]
        public int WorkspaceRoleID { get; set; }

        [JsonPropertyName("roleName")]
        public string RoleName { get; set; } = string.Empty;
    }
}
