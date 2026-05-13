using AllocServer.DTOs.Accounts;
using AllocServer.Models;

namespace AllocServer.Interfaces.Auth
{
    public interface IAccountService
    {
        // --- Login ---
        Task<Account?> GetAccountByEmailAsync(string email);
        bool VerifyPassword(string password, string passwordHash);

        Task<PagedAccountsResponse> GetAccountsAsync(GetAccountsQuery query);

        /// <summary>Tìm Account theo ID (dùng bởi AccountActiveHandler trong chain)</summary>
        Task<Account?> GetAccountByIdAsync(int accountId);

        Task<AccountMeResponse?> GetCurrentAccountProfileAsync(int accountId);

        Task<AccountMeResponse?> UpdateCurrentAccountProfileAsync(
            int accountId,
            UpdateAccountProfileRequest request);

        // --- Register ---
        /// <summary>Kiểm tra email đã tồn tại (kể cả đã xóa mềm hay chưa)</summary>
        Task<bool> IsEmailExistsAsync(string email);

        /// <summary>Tạo và lưu Account mới vào DB</summary>
        Task<Account> CreateAccountAsync(Account account);

        /// <summary>Tạo và lưu Resource (profile) cho Account vừa tạo</summary>
        Task<Resource> CreateResourceAsync(Resource resource);

        /// <summary>Cập nhật thời gian đăng nhập cuối</summary>
        Task UpdateLastLoginAsync(int accountId);
    }
}
