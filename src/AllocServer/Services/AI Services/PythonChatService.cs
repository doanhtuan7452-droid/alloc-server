using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Http.Json;
using System.Threading;
using System.Threading.Tasks;
using AllocServer.Constants.Permissions;
using AllocServer.Data;
using AllocServer.DTOs.AIChat;
using AllocServer.Exceptions;
using AllocServer.Interfaces;
using AllocServer.Interfaces.AI;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;

namespace AllocServer.Services.AI_Services
{
    public class PythonChatService : IPythonChatService
    {
        private const string AIChatQuotaFeatureCode = "AI_CHAT_QUOTA";

        private readonly HttpClient _httpClient;
        private readonly HttpClient _llmClient;
        private readonly ApplicationDbContext _context;
        private readonly IFeatureQuotaService _featureQuotaService;
        private readonly IConfiguration _configuration;
        private readonly IAIQuotaCompensationQueue _quotaCompensationQueue;

        public PythonChatService(
            IHttpClientFactory httpClientFactory,
            ApplicationDbContext context,
            IFeatureQuotaService featureQuotaService,
            IConfiguration configuration,
            IAIQuotaCompensationQueue quotaCompensationQueue)
        {
            _httpClient = httpClientFactory.CreateClient("PythonAIClient");
            _llmClient = httpClientFactory.CreateClient("PythonLLMClient");
            _context = context;
            _featureQuotaService = featureQuotaService;
            _configuration = configuration;
            _quotaCompensationQueue = quotaCompensationQueue;
        }

        public async Task<PythonConversationsResponse> GetConversationsAsync(string userId, int limit, int skip)
        {
            var url = $"internal/v1/chat/conversations?user_id={Uri.EscapeDataString(userId)}&limit={limit}&skip={skip}";
            try
            {
                var response = await _httpClient.GetAsync(url);
                if (response.IsSuccessStatusCode)
                {
                    var result = await response.Content.ReadFromJsonAsync<PythonConversationsResponse>();
                    return result ?? new PythonConversationsResponse();
                }

                await HandleDownstreamErrorAsync(response);
                return new PythonConversationsResponse();
            }
            catch (HttpRequestException ex)
            {
                throw new InvalidOperationException("DownstreamServiceError", ex);
            }
        }

        public async Task<PythonMessagesResponse> GetMessagesAsync(string conversationId, string userId, int limit, int skip, string order)
        {
            var url = $"internal/v1/chat/conversations/{Uri.EscapeDataString(conversationId)}/messages?user_id={Uri.EscapeDataString(userId)}&limit={limit}&skip={skip}&order={Uri.EscapeDataString(order)}";
            try
            {
                var response = await _httpClient.GetAsync(url);
                if (response.IsSuccessStatusCode)
                {
                    var result = await response.Content.ReadFromJsonAsync<PythonMessagesResponse>();
                    return result ?? new PythonMessagesResponse();
                }

                await HandleDownstreamErrorAsync(response);
                return new PythonMessagesResponse();
            }
            catch (HttpRequestException ex)
            {
                throw new InvalidOperationException("DownstreamServiceError", ex);
            }
        }

        public async Task<PythonChatQueryResponse> ChatAsync(string userId, PythonChatQueryRequest request, CancellationToken cancellationToken = default)
        {
            // 1. Kiểm tra tư cách thành viên active và phân quyền ai:ask trong Workspace
            var accountIdInt = int.Parse(userId);
            var membership = await ResolveActiveMembershipAsync(accountIdInt, request.WorkspaceId);
            await EnsureAskPermissionAsync(membership);

            var billingMonth = GetCurrentBillingMonth();
            var effectiveLimit = await ResolveEffectiveQuotaLimitAsync(request.WorkspaceId);

            // 2. Trực tiếp thực hiện trừ Quota và giải phóng DB Lock ngay lập tức (không giữ SQL Transaction)
            await EnsureMonthlyUsageRowAsync(request.WorkspaceId, billingMonth);
            var newCount = await TryConsumeAIQuotaAsync(request.WorkspaceId, billingMonth, effectiveLimit);

            try
            {
                // 3. Chuẩn bị bản sao an toàn của DynamicToolsMetadata và tiêm các ID context cùng chữ ký
                var dynamicToolsMetadata = request.DynamicToolsMetadata != null 
                    ? new Dictionary<string, object>(request.DynamicToolsMetadata) 
                    : new Dictionary<string, object>();
                
                if (request.WorkspaceId > 0)
                {
                    dynamicToolsMetadata["workspaceId"] = request.WorkspaceId;
                }
                
                if (accountIdInt > 0)
                {
                    dynamicToolsMetadata["userId"] = accountIdInt;
                    
                    var secretKey = _configuration["PythonServiceSettings:Internal:Secret"] ?? "default_secret";
                    dynamicToolsMetadata["contextSignature"] = GenerateContextSignature(accountIdInt, request.WorkspaceId, secretKey);
                }

                // Chuẩn bị payload gửi cho Python Server (ép kiểu UserId sang string cẩn thận)
                var payload = new PythonChatQueryPayload
                {
                    ConversationId = request.ConversationId,
                    UserId = userId,
                    Message = request.Message,
                    Attachments = request.Attachments,
                    Provider = request.Provider,
                    Model = request.Model,
                    Temperature = request.Temperature,
                    ForceNew = request.ForceNew,
                    DynamicToolsMetadata = dynamicToolsMetadata
                };

                // Gọi Python LLM Server sử dụng client có timeout lớn (60-120s)
                var response = await _llmClient.PostAsJsonAsync("api/v1/chat/agent/mongo-query", payload, cancellationToken);
                
                if (!response.IsSuccessStatusCode)
                {
                    await HandleDownstreamErrorAsync(response);
                }

                var result = await response.Content.ReadFromJsonAsync<PythonChatQueryResponse>(cancellationToken: cancellationToken);
                if (result == null)
                {
                    throw new InvalidOperationException("DownstreamServiceError");
                }

                result.RemainingQuota = CalculateRemainingQuota(effectiveLimit, newCount);
                return result;
            }
            catch (Exception ex)
            {
                // 4. Giao dịch bù trừ (Compensating Transaction): Đẩy vào hàng đợi ngầm xử lý retry an toàn
                _quotaCompensationQueue.QueueCompensation(request.WorkspaceId, billingMonth);

                throw new InvalidOperationException("AI server hien tai khong kha dung, he thong dang hoan lai luot chat cua ban.", ex);
            }
        }

        private async Task<ActiveMembership> ResolveActiveMembershipAsync(int accountId, int workspaceId)
        {
            var membership = await _context.WorkspaceMembers
                .AsNoTracking()
                .Where(member =>
                    member.WorkspaceID == workspaceId
                    && member.Resource.AccountID == accountId
                    && member.Status == "Active"
                    && !member.Workspace.IsDeleted
                    && !member.Resource.IsDeleted)
                .Select(member => new ActiveMembership
                {
                    WorkspaceMemberID = member.WorkspaceMemberID,
                    WorkspaceRoleID = member.WorkspaceRoleID,
                    RoleName = member.WorkspaceRole.RoleName
                })
                .FirstOrDefaultAsync();

            if (membership == null)
            {
                throw new UnauthorizedAccessException("Ban khong phai thanh vien active cua workspace nay.");
            }

            return membership;
        }

        private async Task EnsureAskPermissionAsync(ActiveMembership membership)
        {
            if (string.Equals(membership.RoleName, "Owner", StringComparison.OrdinalIgnoreCase))
            {
                return;
            }

            var hasPermission = await _context.RolePermissions
                .AsNoTracking()
                .AnyAsync(rolePermission =>
                    rolePermission.WorkspaceRoleID == membership.WorkspaceRoleID
                    && rolePermission.PermissionID == AIPermissionIds.Ask);

            if (!hasPermission)
            {
                throw new UnauthorizedAccessException("Ban khong co quyen su dung AI trong workspace nay.");
            }
        }

        private async Task<int> ResolveEffectiveQuotaLimitAsync(int workspaceId)
        {
            var isStrictCheckEnabled = _configuration.GetValue<bool>("FeatureToggles:EnableStrictQuotaCheck");
            if (!isStrictCheckEnabled)
            {
                return -1;
            }

            var limit = await _featureQuotaService.GetFeatureLimitAsync(workspaceId, AIChatQuotaFeatureCode);
            if (limit == null)
            {
                throw new QuotaExceededException("Khong tim thay cau hinh quota AI cho workspace nay.");
            }

            if (limit.LimitValue == -1)
            {
                return -1;
            }

            if (limit.LimitValue <= 0)
            {
                throw new QuotaExceededException("Workspace da het quota hoi dap AI cua goi cuoc hien tai.");
            }

            return limit.LimitValue;
        }

        private async Task EnsureMonthlyUsageRowAsync(int workspaceId, DateOnly billingMonth)
        {
            await _context.Database.ExecuteSqlInterpolatedAsync($@"
IF NOT EXISTS (
    SELECT 1
    FROM WorkspaceMonthlyUsages WITH (UPDLOCK, HOLDLOCK)
    WHERE WorkspaceID = {workspaceId} AND BillingMonth = {billingMonth}
)
BEGIN
    INSERT INTO WorkspaceMonthlyUsages (WorkspaceID, BillingMonth, AIQueryCount, StorageUsedMB, UpdatedAt)
    VALUES ({workspaceId}, {billingMonth}, 0, 0, SYSUTCDATETIME())
END");
        }

        private async Task<int> TryConsumeAIQuotaAsync(int workspaceId, DateOnly billingMonth, int effectiveLimit)
        {
            var affectedRows = await _context.Database.ExecuteSqlInterpolatedAsync($@"
UPDATE WorkspaceMonthlyUsages
SET AIQueryCount = ISNULL(AIQueryCount, 0) + 1,
    UpdatedAt = SYSUTCDATETIME()
WHERE WorkspaceID = {workspaceId}
  AND BillingMonth = {billingMonth}
  AND ({effectiveLimit} = -1 OR ISNULL(AIQueryCount, 0) < {effectiveLimit})");

            if (affectedRows == 0)
            {
                throw new QuotaExceededException("Workspace da vuot quota hoi dap AI cua thang hien tai.");
            }

            return await _context.WorkspaceMonthlyUsages
                .AsNoTracking()
                .Where(usage =>
                    usage.WorkspaceID == workspaceId
                    && usage.BillingMonth == billingMonth)
                .Select(usage => usage.AIQueryCount)
                .SingleAsync();
        }

        private static int? CalculateRemainingQuota(int effectiveLimit, int newCount)
        {
            return effectiveLimit == -1 ? null : Math.Max(effectiveLimit - newCount, 0);
        }

        private static DateOnly GetCurrentBillingMonth()
        {
            var today = DateOnly.FromDateTime(DateTime.UtcNow);
            return new DateOnly(today.Year, today.Month, 1);
        }

        private static async Task HandleDownstreamErrorAsync(HttpResponseMessage response)
        {
            if (response.StatusCode == HttpStatusCode.NotFound)
            {
                throw new KeyNotFoundException("ConversationNotFound");
            }

            if (response.StatusCode == HttpStatusCode.Unauthorized || response.StatusCode == HttpStatusCode.Forbidden)
            {
                throw new UnauthorizedAccessException("ConversationAccessDenied");
            }

            if (response.StatusCode == HttpStatusCode.BadRequest)
            {
                throw new ArgumentException("InvalidRequest");
            }

            throw new InvalidOperationException("DownstreamServiceError");
        }

        private string GenerateContextSignature(int userId, int workspaceId, string secretKey)
        {
            var payload = $"{userId}:{workspaceId}";
            var keyBytes = System.Text.Encoding.UTF8.GetBytes(secretKey);
            var payloadBytes = System.Text.Encoding.UTF8.GetBytes(payload);
            
            byte[] hash = System.Security.Cryptography.HMACSHA256.HashData(keyBytes, payloadBytes);
            return Convert.ToBase64String(hash);
        }

        private sealed class ActiveMembership
        {
            public int WorkspaceMemberID { get; set; }
            public int WorkspaceRoleID { get; set; }
            public string RoleName { get; set; } = string.Empty;
        }
    }
}
