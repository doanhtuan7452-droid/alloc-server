using AllocServer.Data;
using AllocServer.DTOs.Common;
using AllocServer.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace AllocServer.Services
{
    public class FeatureQuotaService : IFeatureQuotaService
    {
        private readonly ApplicationDbContext _dbContext;
        private readonly IConfiguration _configuration;

        public FeatureQuotaService(ApplicationDbContext dbContext, IConfiguration configuration)
        {
            _dbContext = dbContext;
            _configuration = configuration;
        }

        public async Task<bool> CheckFeatureQuotaAsync(int workspaceId, string featureCode, int currentCount = 0)
        {
            // 1. Kiểm tra MVP Mode từ biến môi trường / cấu hình
            var isStrictCheckEnabled = _configuration.GetValue<bool>("FeatureToggles:EnableStrictQuotaCheck");

            if (!isStrictCheckEnabled)
            {
                // Nếu đang tắt kiểm tra, luôn cho phép
                return true;
            }

            // 2. Query View để lấy thông tin giới hạn hiện tại
            var limitInfo = await _dbContext.WorkspaceCurrentLimits
                .FirstOrDefaultAsync(l => l.WorkspaceID == workspaceId && l.FeatureCode == featureCode);

            // Nếu không tìm thấy thông tin gói (có thể là lỗi data hoặc chưa có gói), 
            // an toàn nhất là từ chối (hoặc tùy logic kinh doanh có thể cho phép)
            if (limitInfo == null) return false;

            // 3. Xử lý logic theo ValueType
            // Dựa vào DB thiết kế, LimitValue = 0 (và ValueType = Boolean thì IsIncluded sẽ quyết định)
            // Trong View vw_WorkspaceCurrentLimits, ta có IsIncluded và LimitValue
            
            // Tính năng Boolean (VD: AI_RISK_MGT) - Nếu LimitValue = 0 thì dựa vào IsIncluded
            if (limitInfo.LimitValue == 0)
            {
                return limitInfo.IsIncluded;
            }

            // Tính năng Numeric (VD: MAX_MEMBERS)
            if (limitInfo.LimitValue == -1)
            {
                return true; // -1 là không giới hạn
            }

            // So sánh với số lượng hiện tại
            return currentCount < limitInfo.LimitValue;
        }

        public async Task<FeatureLimitInfo?> GetFeatureLimitAsync(int workspaceId, string featureCode)
        {
            return await _dbContext.WorkspaceCurrentLimits
                .AsNoTracking()
                .Where(limit =>
                    limit.WorkspaceID == workspaceId
                    && limit.FeatureCode == featureCode)
                .Select(limit => new FeatureLimitInfo
                {
                    IsIncluded = limit.IsIncluded,
                    LimitValue = limit.LimitValue
                })
                .FirstOrDefaultAsync();
        }
    }
}
