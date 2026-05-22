using System.Text.Json.Serialization;

namespace AllocServer.DTOs.Workspaces
{
    public class WorkspaceMemberResourceResponse
    {
        [JsonPropertyName("resourceId")]
        public int ResourceID { get; set; }

        [JsonPropertyName("fullName")]
        public string FullName { get; set; } = string.Empty;

        [JsonPropertyName("phoneNumber")]
        public string? PhoneNumber { get; set; }

        [JsonPropertyName("avatarUrl")]
        public string? AvatarURL { get; set; }

        [JsonPropertyName("timezone")]
        public string Timezone { get; set; } = "UTC";
    }
}
