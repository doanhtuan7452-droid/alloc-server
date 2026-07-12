using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Net.Http.Json;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace AllocServer.Services.AI_Services
{
    public class AIBackchannelToolSyncService : IHostedService
    {
        private readonly IHttpClientFactory _httpClientFactory;
        private readonly IConfiguration _configuration;
        private readonly ILogger<AIBackchannelToolSyncService> _logger;
        private readonly IServiceScopeFactory _scopeFactory;
        private CancellationTokenSource? _cts;

        public AIBackchannelToolSyncService(
            IHttpClientFactory httpClientFactory,
            IConfiguration configuration,
            ILogger<AIBackchannelToolSyncService> logger,
            IServiceScopeFactory scopeFactory)
        {
            _httpClientFactory = httpClientFactory;
            _configuration = configuration;
            _logger = logger;
            _scopeFactory = scopeFactory;
        }

        public Task StartAsync(CancellationToken cancellationToken)
        {
            _logger.LogInformation("[AI Tool Sync] Khởi động background service đồng bộ tools.");
            _cts = new CancellationTokenSource();

            // Chạy tiến trình đồng bộ ngầm để không block luồng startup chính của Web Host
            _ = SyncToolsWithRetryAsync(_cts.Token);

            return Task.CompletedTask;
        }

        public Task StopAsync(CancellationToken cancellationToken)
        {
            _logger.LogInformation("[AI Tool Sync] Dừng background service đồng bộ tools.");
            _cts?.Cancel();
            return Task.CompletedTask;
        }

        private async Task SyncToolsWithRetryAsync(CancellationToken cancellationToken)
        {
            // Tránh race condition bằng cách chờ 3-5 giây để Kestrel Server khởi chạy hoàn tất trước khi đăng ký
            try
            {
                await Task.Delay(3000, cancellationToken);
            }
            catch (TaskCanceledException)
            {
                return;
            }

            var settings = _configuration.GetSection("PythonServiceSettings");
            var callbackBaseUrl = settings["ToolsCallbackBaseUrl"] ?? "http://host.docker.internal:5000/";
            if (!callbackBaseUrl.EndsWith("/"))
            {
                callbackBaseUrl += "/";
            }

            var internalSection = settings.GetSection("Internal");
            var headerName = internalSection["HeaderName"] ?? settings["HeaderName"] ?? "X-Internal-Token";
            var secret = internalSection["Secret"] ?? settings["Secret"] ?? "testkey123";

            // 1. Tạo payload cấu hình công cụ động theo chuẩn JSON Schema (Hợp đồng LLM)
            var registerPayload = new RegisterToolsPayload
            {
                Tools = new List<DynamicToolDto>
                {
                    new DynamicToolDto
                    {
                        Name = "create_project",
                        Description = "Tạo một dự án mới trong Workspace được chỉ định. LLM bắt buộc phải điền đầy đủ các tham số được yêu cầu. Hợp đồng quy chuẩn bắt buộc:\n" +
                                      "- projectName: Tên dự án, không được để trống, tối đa 255 ký tự.\n" +
                                      "- startDate & endDate: Định dạng 'yyyy-MM-dd', bắt buộc truyền cả hai và ngày kết thúc phải lớn hơn hoặc bằng ngày bắt đầu (endDate >= startDate).\n" +
                                      "- expectedBudget: Ngân sách dự kiến không được âm (>= 0).\n" +
                                      "- methodology: Phương pháp luận chỉ được phép nhận một trong các giá trị: 'Agile', 'Waterfall', 'Scrum', 'Kanban', 'Hybrid'. Mặc định: 'Agile'.\n" +
                                      "- originalCurrencyCode: Mã tiền tệ gốc, tối đa 5 ký tự. Mặc định: 'USD'.\n" +
                                      "- exchangeRateToUSD: Tỷ giá quy đổi sang USD, số dương (> 0). Mặc định: 1.0.",
                        EndpointUrl = $"{callbackBaseUrl}api/v1/internal-tools/create-project",
                        Method = "POST",
                        Headers = new Dictionary<string, string> { { headerName, secret } },
                        BundleGroup = "Project_Management_Tools",
                        Parameters = new Dictionary<string, object>
                        {
                            { "type", "object" },
                            { "properties", new Dictionary<string, object>
                                {
                                    { "workspaceId", new { type = "integer", description = "ID của Workspace chứa dự án" } },
                                    { "userId", new { type = "integer", description = "ID Account của người thực hiện tạo" } },
                                    { "projectName", new { type = "string", maxLength = 255, description = "Tên dự án" } },
                                    { "expectedBudget", new { type = "number", minimum = 0, description = "Ngân sách dự kiến của dự án" } },
                                    { "startDate", new { type = "string", format = "date", description = "Ngày bắt đầu dự án (yyyy-MM-dd)" } },
                                    { "endDate", new { type = "string", format = "date", description = "Ngày kết thúc dự án (yyyy-MM-dd)" } },
                                    { "originalCurrencyCode", new { type = "string", maxLength = 5, @default = "USD", description = "Mã tiền tệ gốc" } },
                                    { "exchangeRateToUSD", new { type = "number", minimum = 0.0001, @default = 1.0, description = "Tỷ giá quy đổi sang USD" } },
                                    { "methodology", new { type = "string", @enum = new[] { "Agile", "Waterfall", "Scrum", "Kanban", "Hybrid" }, @default = "Agile", description = "Phương pháp quản lý dự án" } }
                                }
                            },
                            { "required", new[] { "workspaceId", "userId", "projectName", "startDate", "endDate" } }
                        }
                    },
                    new DynamicToolDto
                    {
                        Name = "create_task",
                        Description = "Tạo một nhiệm vụ (task) mới trong dự án được chỉ định. LLM bắt buộc điền các tham số được yêu cầu. Hợp đồng quy chuẩn bắt buộc:\n" +
                                      "- projectId & taskName: ID dự án và tên nhiệm vụ không được để trống, tên nhiệm vụ tối đa 255 ký tự.\n" +
                                      "- estimatedValue: Giá trị ước tính phải từ 0.01 trở lên (>= 0.01).\n" +
                                      "- durationType: Loại thời lượng, chỉ chấp nhận một trong: 'Hour', 'Day', 'StoryPoint'. Bắt buộc điền.\n" +
                                      "- startDate & endDate: Định dạng 'yyyy-MM-dd'. Ngày kết thúc phải lớn hơn hoặc bằng ngày bắt đầu nếu điền cả hai.\n" +
                                      "- status: Chỉ nhận: 'To-do', 'In Progress', 'Review', 'Done'. Mặc định: 'To-do'.\n" +
                                      "- complexity: Chỉ nhận: 'Low', 'Medium', 'High', 'Critical'. Mặc định: 'Medium'.\n" +
                                      "- requiredSkillLevel: Chỉ nhận: 'Low', 'Medium', 'High', 'Expert'. Mặc định: 'Medium'.\n" +
                                      "- priority: Chỉ nhận: 'Low', 'Medium', 'High', 'Critical'. Mặc định: 'Medium'.\n" +
                                      "- expectedTeamSize: Số nhân sự dự kiến tối thiểu là 1. Mặc định: 1.",
                        EndpointUrl = $"{callbackBaseUrl}api/v1/internal-tools/create-task",
                        Method = "POST",
                        Headers = new Dictionary<string, string> { { headerName, secret } },
                        BundleGroup = "Project_Management_Tools",
                        Parameters = new Dictionary<string, object>
                        {
                            { "type", "object" },
                            { "properties", new Dictionary<string, object>
                                {
                                    { "projectId", new { type = "integer", description = "ID của Dự án chứa task này" } },
                                    { "userId", new { type = "integer", description = "ID Account của người thực hiện tạo task" } },
                                    { "taskName", new { type = "string", maxLength = 255, description = "Tên nhiệm vụ" } },
                                    { "estimatedValue", new { type = "number", minimum = 0.01, description = "Giá trị ước tính (>= 0.01)" } },
                                    { "durationType", new { type = "string", @enum = new[] { "Hour", "Day", "StoryPoint" }, description = "Đơn vị thời lượng thực hiện" } },
                                    { "startDate", new { type = "string", format = "date", description = "Ngày bắt đầu (yyyy-MM-dd)" } },
                                    { "endDate", new { type = "string", format = "date", description = "Ngày kết thúc (yyyy-MM-dd)" } },
                                    { "status", new { type = "string", @enum = new[] { "To-do", "In Progress", "Review", "Done" }, @default = "To-do", description = "Trạng thái task" } },
                                    { "complexity", new { type = "string", @enum = new[] { "Low", "Medium", "High", "Critical" }, @default = "Medium", description = "Độ phức tạp task" } },
                                    { "requiredSkillLevel", new { type = "string", @enum = new[] { "Low", "Medium", "High", "Expert" }, @default = "Medium", description = "Yêu cầu cấp bậc trình độ kỹ năng" } },
                                    { "priority", new { type = "string", @enum = new[] { "Low", "Medium", "High", "Critical" }, @default = "Medium", description = "Mức độ ưu tiên" } },
                                    { "expectedTeamSize", new { type = "integer", minimum = 1, @default = 1, description = "Số nhân viên dự kiến tối thiểu" } }
                                }
                            },
                            { "required", new[] { "projectId", "userId", "taskName", "estimatedValue", "durationType" } }
                        }
                    },
                    new DynamicToolDto
                    {
                        Name = "get_project_info",
                        Description = "Lấy thông tin tổng quan của một dự án bao gồm thời gian thực hiện, ngân sách, số lượng task, số lượng rủi ro đang mở, điểm rủi ro trung bình và tổng chi phí tối ưu hóa qua View ProjectRiskFeatures. Lưu ý: Trường tasksByStatus có thể trả về một đối tượng rỗng '{}' nếu dự án mới được khởi tạo và chưa có công việc nào. Trong trường hợp này, LLM nên giải thích tự nhiên cho người dùng rằng dự án hiện chưa được phân bổ công việc nào. Yêu cầu truyền projectId và userId.",
                        EndpointUrl = $"{callbackBaseUrl}api/v1/internal-tools/get-project-info",
                        Method = "POST",
                        Headers = new Dictionary<string, string> { { headerName, secret } },
                        BundleGroup = "Project_Analysis_Tools",
                        Parameters = new Dictionary<string, object>
                        {
                            { "type", "object" },
                            { "properties", new Dictionary<string, object>
                                {
                                    { "projectId", new { type = "integer", description = "ID của dự án" } },
                                    { "userId", new { type = "integer", description = "ID tài khoản yêu cầu lấy dữ liệu" } }
                                }
                            },
                            { "required", new[] { "projectId", "userId" } }
                        }
                    },
                    new DynamicToolDto
                    {
                        Name = "get_employee_list",
                        Description = "Lấy danh sách các nhân sự đang hoạt động trong Workspace của dự án. Không chứa các trường thông tin nhạy cảm. Yêu cầu truyền userId và một trong hai tham số: workspaceId hoặc projectId.",
                        EndpointUrl = $"{callbackBaseUrl}api/v1/internal-tools/get-employee-list",
                        Method = "POST",
                        Headers = new Dictionary<string, string> { { headerName, secret } },
                        BundleGroup = "Project_Analysis_Tools",
                        Parameters = new Dictionary<string, object>
                        {
                            { "type", "object" },
                            { "properties", new Dictionary<string, object>
                                {
                                    { "workspaceId", new { type = "integer", description = "ID Workspace cần tra cứu" } },
                                    { "projectId", new { type = "integer", description = "ID Dự án (nếu không truyền workspaceId)" } },
                                    { "userId", new { type = "integer", description = "ID tài khoản yêu cầu lấy dữ liệu" } }
                                }
                            },
                            { "required", new[] { "userId" } }
                        }
                    },
                    new DynamicToolDto
                    {
                        Name = "get_employee_detail",
                        Description = "Lấy chi tiết hồ sơ nhân sự bao gồm trình độ chuyên môn, điểm kỹ năng, điểm đánh giá hiệu năng, danh sách kỹ năng thực tế, hiệu năng làm việc lịch sử và tải trọng công việc hiện tại. Yêu cầu bảo mật cao, chỉ tài khoản có quyền xem hồ sơ mới gọi được. Yêu cầu truyền workspaceId, userId và một trong hai: workspaceMemberId hoặc employeeCode.",
                        EndpointUrl = $"{callbackBaseUrl}api/v1/internal-tools/get-employee-detail",
                        Method = "POST",
                        Headers = new Dictionary<string, string> { { headerName, secret } },
                        BundleGroup = "Project_Analysis_Tools",
                        Parameters = new Dictionary<string, object>
                        {
                            { "type", "object" },
                            { "properties", new Dictionary<string, object>
                                {
                                    { "workspaceId", new { type = "integer", description = "ID Workspace chứa thành viên" } },
                                    { "workspaceMemberId", new { type = "integer", description = "ID WorkspaceMember của nhân viên" } },
                                    { "employeeCode", new { type = "string", description = "Mã nhân viên (nếu không có workspaceMemberId)" } },
                                    { "userId", new { type = "integer", description = "ID tài khoản yêu cầu lấy dữ liệu" } }
                                }
                            },
                            { "required", new[] { "workspaceId", "userId" } }
                        }
                    },
                    new DynamicToolDto
                    {
                        Name = "get_workspace_projects",
                        Description = "Lấy danh sách các dự án trong một Workspace hiện tại của bạn, bao gồm thông tin chi tiết (tên, ngày bắt đầu/kết thúc, ngân sách, doanh thu, phương pháp luận) và các chỉ số phân tích rủi ro/độ phức tạp tổng hợp từ View ProjectRiskFeatures. Hỗ trợ lọc theo trạng thái dự án, sắp xếp, và phân trang kết quả.",
                        EndpointUrl = $"{callbackBaseUrl}api/v1/internal-tools/get-workspace-projects",
                        Method = "POST",
                        Headers = new Dictionary<string, string> { { headerName, secret } },
                        BundleGroup = "Project_Analysis_Tools",
                        Parameters = new Dictionary<string, object>
                        {
                            { "type", "object" },
                            { "properties", new Dictionary<string, object>
                                {
                                    { "workspaceId", new { type = "integer", description = "ID của Workspace cần lấy danh sách dự án (Được hệ thống tự động điền từ ngữ cảnh, LLM không cần tự nhập)" } },
                                    { "userId", new { type = "integer", description = "ID tài khoản yêu cầu (Được hệ thống tự động điền từ ngữ cảnh, LLM không cần tự nhập)" } },
                                    { "status", new { type = "string", @enum = new[] { "Planning", "In Progress", "On Hold", "Completed", "Cancelled" }, description = "Bộ lọc trạng thái dự án (tùy chọn)" } },
                                    { "limit", new { type = "integer", @default = 10, description = "Số lượng kết quả tối đa muốn trả về để tránh tràn token context (tùy chọn, mặc định 10)" } },
                                    { "skip", new { type = "integer", @default = 0, description = "Số lượng kết quả bỏ qua để phân trang (tùy chọn)" } },
                                    { "sortBy", new { type = "string", @enum = new[] { "risk_score_desc", "budget_desc", "newest" }, @default = "newest", description = "Tiêu chí sắp xếp kết quả (tùy chọn: risk_score_desc là điểm rủi ro từ cao xuống thấp, budget_desc là ngân sách từ cao xuống thấp, newest là dự án mới tạo trước)" } }
                                }
                            },
                            { "required", new[] { "workspaceId", "userId" } }
                        }
                    }
                }
            };

            // 2. Exponential Backoff Retry để giải quyết Startup Race Conditions
            int maxAttempts = 5;
            double delaySeconds = 2.0;
            var client = _httpClientFactory.CreateClient("PythonAIClient");

            for (int attempt = 1; attempt <= maxAttempts; attempt++)
            {
                if (cancellationToken.IsCancellationRequested)
                {
                    _logger.LogWarning("[AI Tool Sync] Tiến trình đồng bộ bị hủy bỏ do dịch vụ shutdown.");
                    return;
                }

                try
                {
                    _logger.LogInformation($"[AI Tool Sync] Đang đồng bộ công cụ động sang Python AI Server. Thử lần {attempt}/{maxAttempts}...");
                    var response = await client.PostAsJsonAsync("api/v1/tools/register", registerPayload, cancellationToken);

                    if (response.IsSuccessStatusCode)
                    {
                        _logger.LogInformation("[AI Tool Sync] Đăng ký công cụ thành công sang Python AI Server (200 OK).");
                        return;
                    }

                    var responseBody = await response.Content.ReadAsStringAsync(cancellationToken);
                    _logger.LogWarning($"[AI Tool Sync] Đăng ký công cụ thất bại. Mã phản hồi: {response.StatusCode}. Chi tiết: {responseBody}");
                }
                catch (Exception ex)
                {
                    _logger.LogWarning($"[AI Tool Sync] Lỗi kết nối khi đồng bộ công cụ sang Python server: {ex.Message}");
                }

                if (attempt < maxAttempts)
                {
                    var backoffTime = TimeSpan.FromSeconds(Math.Pow(delaySeconds, attempt));
                    _logger.LogInformation($"[AI Tool Sync] Đang thử lại sau {backoffTime.TotalSeconds} giây...");
                    try
                    {
                        await Task.Delay(backoffTime, cancellationToken);
                    }
                    catch (TaskCanceledException)
                    {
                        return;
                    }
                }
            }

            _logger.LogError("[AI Tool Sync] Quá số lần thử lại tối đa. Không thể đồng bộ công cụ động sang Python AI Server. LLM sẽ không thể dùng các công cụ này.");
        }

        // --- Các lớp DTO nội bộ phục vụ việc Serialize đăng ký ---
        private class RegisterToolsPayload
        {
            public List<DynamicToolDto> Tools { get; set; } = new();
        }

        private class DynamicToolDto
        {
            public string Name { get; set; } = string.Empty;
            public string Description { get; set; } = string.Empty;
            public string EndpointUrl { get; set; } = string.Empty;
            public string Method { get; set; } = "POST";
            public Dictionary<string, object> Parameters { get; set; } = new();
            public Dictionary<string, string> Headers { get; set; } = new();
            public string BundleGroup { get; set; } = string.Empty;
        }
    }
}
