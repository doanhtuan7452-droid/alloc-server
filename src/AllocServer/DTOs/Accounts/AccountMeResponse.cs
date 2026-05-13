using System.Text.Json.Serialization;

namespace AllocServer.DTOs.Accounts
{
    public class AccountMeResponse
    {
        [JsonPropertyName("accountId")]
        public int AccountID { get; set; }

        [JsonPropertyName("email")]
        public string Email { get; set; } = string.Empty;

        [JsonPropertyName("authType")]
        public string AuthType { get; set; } = string.Empty;

        [JsonPropertyName("isEmailVerified")]
        public bool? IsEmailVerified { get; set; }

        [JsonPropertyName("accountStatus")]
        public string AccountStatus { get; set; } = string.Empty;

        [JsonPropertyName("isSystemAccount")]
        public bool? IsSystemAccount { get; set; }

        [JsonPropertyName("lastLoginAt")]
        public DateTime? LastLoginAt { get; set; }

        [JsonPropertyName("createdAt")]
        public DateTime? CreatedAt { get; set; }

        [JsonPropertyName("updatedAt")]
        public DateTime? UpdatedAt { get; set; }

        [JsonPropertyName("profile")]
        public ResourceProfileResponse Profile { get; set; } = new();
    }

    public class ResourceProfileResponse
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

        [JsonPropertyName("createdAt")]
        public DateTime? CreatedAt { get; set; }
    }
}
