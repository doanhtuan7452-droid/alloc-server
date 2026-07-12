using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace AllocServer.DTOs.Workspaces
{
    public class WorkspaceRoleDetailResponse
    {
        [JsonPropertyName("workspaceRoleId")]
        public int WorkspaceRoleID { get; set; }

        [JsonPropertyName("workspaceId")]
        public int? WorkspaceID { get; set; }

        [JsonPropertyName("roleName")]
        public string RoleName { get; set; } = string.Empty;

        [JsonPropertyName("isTemplate")]
        public bool IsTemplate { get; set; }

        [JsonPropertyName("permissions")]
        public List<string> Permissions { get; set; } = new();
    }
}
