using AllocServer.Contexts;
using AllocServer.DTOs.Auth;
using AllocServer.Interfaces.Auth;
using AllocServer.Interfaces.Register;
using AllocServer.Models;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace AllocServer.Services.Register_Strategies
{
    /// <summary>
    /// Strategy Pattern — Chiến lược đăng ký / đăng nhập bằng Google ID Token.
    /// Luồng: Xác thực token với Google API → Lấy thông tin user → 
    ///        Nếu email đã có (Local) → Link tài khoản | Nếu chưa có → Tạo mới Account + Resource → Trả token
    /// </summary>
    public class GoogleRegistrationStrategy : IRegistrationStrategy
    {
        private readonly IAccountService _accountService;
        private readonly ITokenService _tokenService;
        private readonly ISessionService _sessionService;
        private readonly IConfiguration _configuration;
        private readonly HttpClient _httpClient;

        public GoogleRegistrationStrategy(
            IAccountService accountService,
            ITokenService tokenService,
            ISessionService sessionService,
            IConfiguration configuration,
            IHttpClientFactory httpClientFactory)
        {
            _accountService = accountService;
            _tokenService = tokenService;
            _sessionService = sessionService;
            _configuration = configuration;
            _httpClient = httpClientFactory.CreateClient("Google");
        }

        public async Task<RegisterResponse> RegisterAsync(RegisterContext context)
        {
            // 1. Xác thực Google ID Token với Google API thật
            var googlePayload = await VerifyGoogleTokenAsync(context.IdToken!);

            if (googlePayload == null)
            {
                return new RegisterResponse
                {
                    Success = false,
                    ErrorMessage = "Google ID Token không hợp lệ hoặc đã hết hạn."
                };
            }

            // 2. Kiểm tra Client ID khớp (bảo mật: tránh token từ app khác)
            var expectedClientId = _configuration["GoogleSettings:ClientId"];
            if (!string.IsNullOrEmpty(expectedClientId) && googlePayload.Audience != expectedClientId)
            {
                return new RegisterResponse
                {
                    Success = false,
                    ErrorMessage = "Google ID Token không thuộc ứng dụng này."
                };
            }

            // 3. Kiểm tra email đã được xác thực từ Google
            if (!googlePayload.EmailVerified)
            {
                return new RegisterResponse
                {
                    Success = false,
                    ErrorMessage = "Email Google chưa được xác thực. Vui lòng xác thực email Google trước."
                };
            }

            var googleEmail = googlePayload.Email;
            var googleSub = googlePayload.Sub;      // Google User ID (unique)
            var googleName = googlePayload.Name ?? googleEmail;
            var googlePicture = googlePayload.Picture;

            // 4. Kiểm tra email đã tồn tại trong hệ thống chưa
            var existingAccount = await _accountService.GetAccountByEmailAsync(googleEmail);

            if (existingAccount != null)
            {
                // ============================================================
                // ACCOUNT LINKING: Email trùng với tài khoản Local/khác
                // → Xác thực thành công → Cấp token cho tài khoản hiện có
                // ============================================================
                await _accountService.UpdateLastLoginAsync(existingAccount.AccountID);

                var accessToken = _tokenService.GenerateJwtToken(existingAccount);
                var refreshToken = _tokenService.GenerateRefreshToken();
                var refreshTokenDays = double.Parse(
                    _configuration.GetSection("JwtSettings")["RefreshTokenExpirationDays"] ?? "7");

                await _sessionService.CreateSessionAsync(
                    existingAccount.AccountID, refreshToken,
                    context.DeviceInfo, context.IpAddress,
                    (int)refreshTokenDays);

                return new RegisterResponse
                {
                    Success = true,
                    AccountID = existingAccount.AccountID,
                    Email = existingAccount.Email,
                    AuthType = existingAccount.AuthType,
                    AccessToken = accessToken,
                    RefreshToken = refreshToken,
                    IsLinked = true    // Thông báo rõ cho client biết đây là link account
                };
            }

            // 5. Email chưa tồn tại → Tạo Account mới bằng Google
            var newAccount = new Account
            {
                Email = googleEmail,
                // PasswordHash lưu Google Sub (User ID) — không phải password thật
                PasswordHash = BCrypt.Net.BCrypt.HashPassword(googleSub),
                AuthType = "Google",
                IsEmailVerified = true,     // Google đã xác thực email rồi
                AccountStatus = "Active",
                IsSystemAccount = false
            };

            var createdAccount = await _accountService.CreateAccountAsync(newAccount);

            // 6. Tự động tạo Resource (profile) lấy thông tin từ Google
            var newResource = new Resource
            {
                AccountID = createdAccount.AccountID,
                FullName = googleName,
                AvatarURL = googlePicture,
                Timezone = "UTC"
            };

            await _accountService.CreateResourceAsync(newResource);

            // 7. Sinh token và tạo session
            var newAccessToken = _tokenService.GenerateJwtToken(createdAccount);
            var newRefreshToken = _tokenService.GenerateRefreshToken();
            var refreshDays = double.Parse(
                _configuration.GetSection("JwtSettings")["RefreshTokenExpirationDays"] ?? "7");

            await _sessionService.CreateSessionAsync(
                createdAccount.AccountID, newRefreshToken,
                context.DeviceInfo, context.IpAddress,
                (int)refreshDays);

            return new RegisterResponse
            {
                Success = true,
                AccountID = createdAccount.AccountID,
                Email = createdAccount.Email,
                AuthType = "Google",
                AccessToken = newAccessToken,
                RefreshToken = newRefreshToken,
                IsLinked = false
            };
        }

        /// <summary>
        /// Gọi Google tokeninfo API để xác thực ID Token.
        /// Docs: https://developers.google.com/identity/sign-in/web/backend-auth
        /// </summary>
        private async Task<GoogleTokenPayload?> VerifyGoogleTokenAsync(string idToken)
        {
            try
            {
                var url = $"https://oauth2.googleapis.com/tokeninfo?id_token={idToken}";
                var response = await _httpClient.GetAsync(url);

                if (!response.IsSuccessStatusCode)
                    return null;

                var json = await response.Content.ReadAsStringAsync();
                var payload = JsonSerializer.Deserialize<GoogleTokenPayload>(json,
                    new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

                // Kiểm tra token chưa hết hạn
                if (payload == null) return null;
                if (long.TryParse(payload.Exp, out var expUnix))
                {
                    var expTime = DateTimeOffset.FromUnixTimeSeconds(expUnix).UtcDateTime;
                    if (expTime < DateTime.UtcNow) return null;
                }

                return payload;
            }
            catch
            {
                return null;
            }
        }
    }

    /// <summary>
    /// Mapping với JSON response từ Google tokeninfo API
    /// </summary>
    internal class GoogleTokenPayload
    {
        /// <summary>Google User ID — unique identifier cho user</summary>
        [JsonPropertyName("sub")]
        public string Sub { get; set; } = string.Empty;

        [JsonPropertyName("email")]
        public string Email { get; set; } = string.Empty;

        [JsonPropertyName("email_verified")]
        public bool EmailVerified { get; set; }

        [JsonPropertyName("name")]
        public string? Name { get; set; }

        [JsonPropertyName("picture")]
        public string? Picture { get; set; }

        /// <summary>Audience — phải khớp với ClientId của app</summary>
        [JsonPropertyName("aud")]
        public string Audience { get; set; } = string.Empty;

        /// <summary>Expiration Unix timestamp</summary>
        [JsonPropertyName("exp")]
        public string Exp { get; set; } = string.Empty;
    }
}
