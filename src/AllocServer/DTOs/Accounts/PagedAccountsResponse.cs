using System.Text.Json.Serialization;

namespace AllocServer.DTOs.Accounts
{
    public class PagedAccountsResponse
    {
        [JsonPropertyName("page")]
        public int Page { get; set; }

        [JsonPropertyName("pageSize")]
        public int PageSize { get; set; }

        [JsonPropertyName("totalItems")]
        public int TotalItems { get; set; }

        [JsonPropertyName("totalPages")]
        public int TotalPages { get; set; }

        [JsonPropertyName("items")]
        public List<AccountListItemResponse> Items { get; set; } = new();
    }

    public class AccountListItemResponse
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
        public ResourceProfileResponse? Profile { get; set; }
    }
}
