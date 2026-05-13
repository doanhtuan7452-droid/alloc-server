using System.ComponentModel.DataAnnotations;

namespace AllocServer.DTOs.Accounts
{
    public class GetAccountsQuery
    {
        [Range(1, int.MaxValue, ErrorMessage = "Page must be greater than or equal to 1.")]
        public int Page { get; set; } = 1;

        [Range(1, 100, ErrorMessage = "PageSize must be between 1 and 100.")]
        public int PageSize { get; set; } = 20;

        [StringLength(100, ErrorMessage = "Keyword must not exceed 100 characters.")]
        public string? Keyword { get; set; }

        [StringLength(20, ErrorMessage = "AccountStatus must not exceed 20 characters.")]
        public string? AccountStatus { get; set; }

        [StringLength(20, ErrorMessage = "AuthType must not exceed 20 characters.")]
        public string? AuthType { get; set; }

        public bool? IsSystemAccount { get; set; }
    }
}
