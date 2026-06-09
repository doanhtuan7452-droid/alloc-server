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

        Task<AccountProfileResponse?> GetCurrentAccountProfileAsync(int accountId);

        Task<AccountProfileResponse?> UpdateCurrentAccountProfileAsync(
            int accountId,
            UpdateAccountProfileRequest request);

        // --- Register ---
        /// <summary>Kiểm tra email đã tồn tại (kể cả đã xóa mềm hay chưa)</summary>
        Task<bool> IsEmailExistsAsync(string email);

        /// <summary>Kiểm tra email tồn tại và tài khoản đang hoạt động (không bị xóa mềm)</summary>
        Task<bool> IsActiveEmailExistsAsync(string email);

        /// <summary>Xác minh email và cập nhật trạng thái IsEmailVerified = true</summary>
        Task<bool> VerifyEmailAsync(string email);

        /// <summary>Xác minh email và cập nhật mốc LastLogin chỉ trong 1 truy vấn DB</summary>
        Task VerifyEmailAndLoginAsync(int accountId);

        /// <summary>Tạo và lưu Account mới vào DB</summary>
        Task<Account> CreateAccountAsync(Account account);

        /// <summary>Tạo và lưu Resource (profile) cho Account vừa tạo</summary>
        Task<Resource> CreateResourceAsync(Resource resource);

        /// <summary>Cập nhật thời gian đăng nhập cuối</summary>
        Task UpdateLastLoginAsync(int accountId);
    }
}
