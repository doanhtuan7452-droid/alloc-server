using AllocServer.Configurations;
using AllocServer.Interfaces.Auth;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Options;
using StackExchange.Redis;
using System.Security.Cryptography;

namespace AllocServer.Services.Auth_Services
{
    public class OtpService : IOtpService
    {
        private readonly IDistributedCache _cache;
        private readonly IEmailSender _emailSender;
        private readonly IAccountService _accountService;
        private readonly IConnectionMultiplexer? _redisConnection;
        private readonly EmailSettings _emailSettings;

        private const string CacheKeyPrefix = "otp:";
        private const string CooldownKeyPrefix = "otp_cooldown:";
        private const int OtpExpiryMinutes = 5;
        private const int CooldownSeconds = 60;
        private const string RedisInstancePrefix = "DemoWebAPI:";
        private static readonly SemaphoreSlim _localLock = new(1, 1);

        public OtpService(
            IDistributedCache cache,
            IEmailSender emailSender,
            IAccountService accountService,
            IOptions<EmailSettings> emailOptions,
            IConnectionMultiplexer? redisConnection = null)
        {
            _cache = cache;
            _emailSender = emailSender;
            _accountService = accountService;
            _emailSettings = emailOptions.Value;
            _redisConnection = redisConnection;
        }

        public async Task<bool> RequestOtpAsync(string email)
        {
            // 1. Kiểm tra tài khoản tồn tại trong DB (Chống Spam và lọc bỏ tài khoản đã xóa mềm)
            var emailExists = await _accountService.IsActiveEmailExistsAsync(email);
            if (!emailExists)
            {
                // Trả về true để giả lập thành công (Chống lỗ hổng Account Enumeration)
                return true;
            }

            // 1.1 Cơ chế Cooldown nội bộ chống Spam lách luật Rate Limiter (Denial of Wallet)
            var cooldownKey = $"{CooldownKeyPrefix}{email.ToLowerInvariant()}";
            var isCooldownActive = await _cache.GetStringAsync(cooldownKey);
            if (isCooldownActive != null)
            {
                // Đang trong thời gian cooldown, từ chối gửi email mới
                return false;
            }

            // 2. Tạo mã OTP ngẫu nhiên 6 chữ số an toàn (Cận trên 1,000,000 để bao gồm cả số 999999)
            var otpCode = RandomNumberGenerator.GetInt32(100000, 1000000).ToString();

            // 3. Lưu trữ vào cache với thời hạn 5 phút
            var cacheKey = $"{CacheKeyPrefix}{email.ToLowerInvariant()}";
            var otpOptions = new DistributedCacheEntryOptions
            {
                AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(OtpExpiryMinutes)
            };
            await _cache.SetStringAsync(cacheKey, otpCode, otpOptions);

            // 3.1 Đăng ký Cooldown trong cache với TTL 60 giây
            var cooldownOptions = new DistributedCacheEntryOptions
            {
                AbsoluteExpirationRelativeToNow = TimeSpan.FromSeconds(CooldownSeconds)
            };
            await _cache.SetStringAsync(cooldownKey, "1", cooldownOptions);

            // 4. Soạn nội dung HTML Email
            var subject = $"[{otpCode}] Mã xác thực OTP ứng dụng Alloc";
            var bodyHtml = $@"
                <div style='font-family: Arial, sans-serif; max-width: 600px; margin: 0 auto; padding: 20px; border: 1px solid #ddd; border-radius: 8px;'>
                    <h2 style='color: #4A90E2; text-align: center;'>Xác Thực Tài Khoản Alloc</h2>
                    <p>Xin chào,</p>
                    <p>Bạn đã yêu cầu mã OTP để xác thực trên hệ thống Alloc. Vui lòng sử dụng mã bảo mật dưới đây:</p>
                    <div style='text-align: center; margin: 30px 0;'>
                        <span style='font-size: 32px; font-weight: bold; letter-spacing: 5px; color: #2C3E50; background: #ECF0F1; padding: 10px 20px; border-radius: 5px; border: 1px dashed #BDC3C7;'>
                            {otpCode}
                        </span>
                    </div>
                    <p style='color: #E74C3C;'>* Lưu ý: Mã OTP này có hiệu lực trong vòng <b>{OtpExpiryMinutes} phút</b> và chỉ sử dụng được 1 lần duy nhất.</p>
                    <p>Nếu bạn không thực hiện yêu cầu này, vui lòng bỏ qua email này.</p>
                    <hr style='border: none; border-top: 1px solid #eee; margin-top: 30px;' />
                    <p style='font-size: 12px; color: #7F8C8D; text-align: center;'>Hệ thống quản lý dự án AllocServer. Tất cả các quyền được bảo lưu.</p>
                </div>";

            // 5. Gửi mail
            await _emailSender.SendEmailAsync(email, subject, bodyHtml);
            return true;
        }

        public async Task<bool> VerifyOtpAsync(string email, string code)
        {
            var relativeKey = $"{CacheKeyPrefix}{email.ToLowerInvariant()}";
            bool isValid = false;

            if (_redisConnection != null)
            {
                // MÔI TRƯỜNG PRODUCTION (DÙNG REDIS):
                // Sử dụng Lua Script để đọc và xóa mã OTP nguyên tử (Atomic Read & Delete) chống Race Condition
                var db = _redisConnection.GetDatabase();
                var absoluteKey = $"{RedisInstancePrefix}{relativeKey}";

                var luaScript = @"
                    local val = redis.call('GET', KEYS[1])
                    if val and val == ARGV[1] then
                        redis.call('DEL', KEYS[1])
                        return 1
                    else
                        return 0
                    end";

                var evalResult = (int)await db.ScriptEvaluateAsync(
                    luaScript,
                    new RedisKey[] { absoluteKey },
                    new RedisValue[] { code });

                isValid = (evalResult == 1);
            }
            else
            {
                // MÔI TRƯỜNG DEV (IN-MEMORY CACHE):
                // Sử dụng SemaphoreSlim để đảm bảo thao tác Đọc-Xóa nguyên tử
                await _localLock.WaitAsync();
                try
                {
                    var cachedCode = await _cache.GetStringAsync(relativeKey);
                    if (cachedCode != null && cachedCode == code)
                    {
                        await _cache.RemoveAsync(relativeKey);
                        isValid = true;
                    }
                }
                finally
                {
                    _localLock.Release();
                }
            }

            if (isValid)
            {
                // Cập nhật trạng thái xác minh email trong CSDL
                await _accountService.VerifyEmailAsync(email);

                // Giải phóng khóa cooldown để người dùng có thể gửi yêu cầu khác ngay nếu cần thiết sau này
                var cooldownKey = $"{CooldownKeyPrefix}{email.ToLowerInvariant()}";
                await _cache.RemoveAsync(cooldownKey);
                return true;
            }

            return false;
        }
    }
}
