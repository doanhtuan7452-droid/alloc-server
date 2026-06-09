using AllocServer.Data;
using AllocServer.DTOs.Accounts;
using AllocServer.Interfaces.Auth;
using AllocServer.Models;
using Microsoft.EntityFrameworkCore;

namespace AllocServer.Services.Auth_Services
{
    public class AccountService : IAccountService
    {
        private readonly ApplicationDbContext _context;
        private readonly IAvatarGenerationService _avatarGenerationService;

        public AccountService(ApplicationDbContext context, IAvatarGenerationService avatarGenerationService)
        {
            _context = context;
            _avatarGenerationService = avatarGenerationService;
        }

        // =============================================
        // LOGIN
        // =============================================

        public async Task<Account?> GetAccountByEmailAsync(string email)
        {
            return await _context.Accounts
                .AsNoTracking()
                .FirstOrDefaultAsync(a => a.Email == email);
        }

        public bool VerifyPassword(string password, string passwordHash)
        {
            return BCrypt.Net.BCrypt.Verify(password, passwordHash);
        }

        public async Task<PagedAccountsResponse> GetAccountsAsync(GetAccountsQuery query)
        {
            var page = Math.Max(query.Page, 1);
            var pageSize = Math.Clamp(query.PageSize, 1, 100);
            var keyword = NormalizeOptionalString(query.Keyword);
            var accountStatus = NormalizeOptionalString(query.AccountStatus);
            var authType = NormalizeOptionalString(query.AuthType);

            var accountsQuery =
                from account in _context.Accounts.AsNoTracking()
                join resource in _context.Resources.AsNoTracking()
                    on account.AccountID equals resource.AccountID into resourceGroup
                from resource in resourceGroup.DefaultIfEmpty()
                select new
                {
                    Account = account,
                    Resource = resource
                };

            if (!string.IsNullOrEmpty(keyword))
            {
                accountsQuery = accountsQuery.Where(item =>
                    item.Account.Email.Contains(keyword)
                    || (item.Resource != null && item.Resource.FullName.Contains(keyword)));
            }

            if (!string.IsNullOrEmpty(accountStatus))
            {
                accountsQuery = accountsQuery.Where(item =>
                    item.Account.AccountStatus == accountStatus);
            }

            if (!string.IsNullOrEmpty(authType))
            {
                accountsQuery = accountsQuery.Where(item =>
                    item.Account.AuthType == authType);
            }

            if (query.IsSystemAccount.HasValue)
            {
                accountsQuery = accountsQuery.Where(item =>
                    (item.Account.IsSystemAccount ?? false) == query.IsSystemAccount.Value);
            }

            var totalItems = await accountsQuery.CountAsync();
            var items = await accountsQuery
                .OrderByDescending(item => item.Account.CreatedAt)
                .ThenByDescending(item => item.Account.AccountID)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .Select(item => new AccountListItemResponse
                {
                    AccountID = item.Account.AccountID,
                    Email = item.Account.Email,
                    AuthType = item.Account.AuthType,
                    IsEmailVerified = item.Account.IsEmailVerified,
                    AccountStatus = item.Account.AccountStatus,
                    IsSystemAccount = item.Account.IsSystemAccount,
                    LastLoginAt = item.Account.LastLoginAt,
                    CreatedAt = item.Account.CreatedAt,
                    UpdatedAt = item.Account.UpdatedAt,
                    Profile = item.Resource == null
                        ? null
                        : new ResourceProfileResponse
                        {
                            ResourceID = item.Resource.ResourceID,
                            FullName = item.Resource.FullName,
                            PhoneNumber = item.Resource.PhoneNumber,
                            AvatarURL = item.Resource.AvatarURL,
                            Timezone = item.Resource.Timezone,
                            CreatedAt = item.Resource.CreatedAt
                        }
                })
                .ToListAsync();

            return new PagedAccountsResponse
            {
                Page = page,
                PageSize = pageSize,
                TotalItems = totalItems,
                TotalPages = (int)Math.Ceiling(totalItems / (double)pageSize),
                Items = items
            };
        }

        public async Task<Account?> GetAccountByIdAsync(int accountId)
        {
            return await _context.Accounts
                .AsNoTracking()
                .FirstOrDefaultAsync(a => a.AccountID == accountId);
        }

        public async Task<AccountProfileResponse?> GetCurrentAccountProfileAsync(int accountId)
        {
            return await (
                from account in _context.Accounts.AsNoTracking()
                join resource in _context.Resources.AsNoTracking()
                    on account.AccountID equals resource.AccountID
                where account.AccountID == accountId
                select new AccountProfileResponse
                {
                    AccountID = account.AccountID,
                    Email = account.Email,
                    AuthType = account.AuthType,
                    IsEmailVerified = account.IsEmailVerified,
                    AccountStatus = account.AccountStatus,
                    IsSystemAccount = account.IsSystemAccount,
                    LastLoginAt = account.LastLoginAt,
                    CreatedAt = account.CreatedAt,
                    UpdatedAt = account.UpdatedAt,
                    Profile = new ResourceProfileResponse
                    {
                        ResourceID = resource.ResourceID,
                        FullName = resource.FullName,
                        PhoneNumber = resource.PhoneNumber,
                        AvatarURL = resource.AvatarURL,
                        Timezone = resource.Timezone,
                        CreatedAt = resource.CreatedAt
                    }
                })
                .FirstOrDefaultAsync();
        }

        public async Task<AccountProfileResponse?> UpdateCurrentAccountProfileAsync(
            int accountId,
            UpdateAccountProfileRequest request)
        {
            var account = await _context.Accounts
                .FirstOrDefaultAsync(a => a.AccountID == accountId);

            if (account == null)
                return null;

            var resource = await _context.Resources
                .FirstOrDefaultAsync(r => r.AccountID == accountId);

            if (resource == null)
                return null;

            var oldFullName = resource.FullName;
            var oldAvatarUrl = resource.AvatarURL;

            var newFullName = request.FullName.Trim();
            var newAvatarUrl = NormalizeOptionalString(request.AvatarURL);

            resource.FullName = newFullName;
            resource.PhoneNumber = NormalizeOptionalString(request.PhoneNumber);
            resource.Timezone = NormalizeOptionalString(request.Timezone) ?? "UTC";
            account.UpdatedAt = DateTime.UtcNow;

            // Sync Avatar URL logic:
            if (string.IsNullOrEmpty(newAvatarUrl))
            {
                // If the user cleared the avatar or it was not provided, regenerate the default avatar
                resource.AvatarURL = _avatarGenerationService.GenerateAvatarUrl(newFullName, account.Email);
            }
            else
            {
                // If a new custom avatar is provided
                if (newAvatarUrl != oldAvatarUrl)
                {
                    resource.AvatarURL = newAvatarUrl;
                }
                else
                {
                    // Unchanged custom avatar OR unchanged generated avatar.
                    // If it was a generated avatar and the FullName changed, we regenerate it to match the new name's initials.
                    if (_avatarGenerationService.IsGeneratedAvatar(oldAvatarUrl) && oldFullName != newFullName)
                    {
                        resource.AvatarURL = _avatarGenerationService.GenerateAvatarUrl(newFullName, account.Email);
                    }
                    else
                    {
                        resource.AvatarURL = newAvatarUrl;
                    }
                }
            }

            await _context.SaveChangesAsync();

            return await GetCurrentAccountProfileAsync(accountId);
        }

        // REGISTER
        // =============================================

        public async Task<bool> IsEmailExistsAsync(string email)
        {
            // Kiểm tra kể cả tài khoản đã xóa mềm để tránh đăng ký trùng email mọi trường hợp
            return await _context.Accounts
                .AsNoTracking()
                .AnyAsync(a => a.Email == email);
        }

        public async Task<bool> IsActiveEmailExistsAsync(string email)
        {
            return await _context.Accounts
                .AsNoTracking()
                .AnyAsync(a => a.Email == email && !a.IsDeleted);
        }

        public async Task<bool> VerifyEmailAsync(string email)
        {
            var account = await _context.Accounts
                .FirstOrDefaultAsync(a => a.Email == email && !a.IsDeleted);

            if (account == null)
                return false;

            account.IsEmailVerified = true;
            account.UpdatedAt = DateTime.UtcNow;

            await _context.SaveChangesAsync();
            return true;
        }

        public async Task VerifyEmailAndLoginAsync(int accountId)
        {
            var account = await _context.Accounts
                .FirstOrDefaultAsync(a => a.AccountID == accountId);

            if (account != null)
            {
                account.IsEmailVerified = true;
                account.LastLoginAt = DateTime.UtcNow;
                account.UpdatedAt = DateTime.UtcNow;
                await _context.SaveChangesAsync();
            }
        }

        public async Task<Account> CreateAccountAsync(Account account)
        {
            account.CreatedAt = DateTime.UtcNow;
            account.UpdatedAt = DateTime.UtcNow;
            _context.Accounts.Add(account);
            await _context.SaveChangesAsync();
            return account;
        }

        public async Task<Resource> CreateResourceAsync(Resource resource)
        {
            resource.CreatedAt = DateTime.UtcNow;
            _context.Resources.Add(resource);
            await _context.SaveChangesAsync();
            return resource;
        }

        public async Task UpdateLastLoginAsync(int accountId)
        {
            var account = await _context.Accounts
                .FirstOrDefaultAsync(a => a.AccountID == accountId);

            if (account != null)
            {
                account.LastLoginAt = DateTime.UtcNow;
                account.UpdatedAt = DateTime.UtcNow;
                await _context.SaveChangesAsync();
            }
        }

        private static string? NormalizeOptionalString(string? value)
        {
            return string.IsNullOrWhiteSpace(value)
                ? null
                : value.Trim();
        }
    }
}
