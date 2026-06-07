using AllocServer.Data;
using AllocServer.Filters;
using AllocServer.Interfaces;
using AllocServer.Interfaces.AI;
using AllocServer.Interfaces.AIInsights;
using AllocServer.Middleware;
using AllocServer.Services;
using AllocServer.Interfaces.Auth;
using AllocServer.Interfaces.Commands;
using AllocServer.Interfaces.Conversations;
using AllocServer.Interfaces.Expenses;
using AllocServer.Interfaces.Facade;
using AllocServer.Interfaces.Messages;
using AllocServer.Interfaces.Notifications;
using AllocServer.Interfaces.ProjectAssets;
using AllocServer.Interfaces.Projects;
using AllocServer.Services.Auth_Strategies;
using AllocServer.Interfaces.Revenues;
using AllocServer.Interfaces.Requests;
using AllocServer.Interfaces.Risks;
using AllocServer.Interfaces.Storage;
using AllocServer.Interfaces.Tasks;
using AllocServer.Interfaces.Timesheets;
using AllocServer.Interfaces.Workspaces;
using AllocServer.Interfaces.WorkspaceMemberProfiles;
using AllocServer.Models;
using AllocServer.Hubs;
using AllocServer.Events;
using AllocServer.Services.AI_Services;
using AllocServer.Services.AIInsight_Services;
using AllocServer.Services.Auth_Services;
using AllocServer.Services.Command_Handlers;
using AllocServer.Services.Conversations;
using AllocServer.Services.Expense_Services;
using AllocServer.Services.Facade_Services;
using AllocServer.Services.Message_Services;
using AllocServer.Services.Notification_Services;
using AllocServer.Services.ProjectAsset_Services;
using AllocServer.Services.Project_Services;
using AllocServer.Services.Revenue_Services;
using AllocServer.Services.Request_Services;

using AllocServer.Services.Risk_Services;
using AllocServer.Services.Storage;
using AllocServer.Services.Task_Services;
using AllocServer.Services.Timesheet_Services;
using AllocServer.Services.Workspace_Services;
using AllocServer.Services.WorkspaceMemberProfile_Services;
using AllocServer.Services.Token_Validation_Handlers;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using AllocServer.Constants.Permissions;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using System.Text;
using Microsoft.AspNetCore.HttpOverrides;
using System.Threading.RateLimiting;
using Microsoft.AspNetCore.RateLimiting;
using System.Security.Claims;
using System.IdentityModel.Tokens.Jwt;
using AllocServer.DTOs.Common;
using Microsoft.OpenApi.Models;

var builder = WebApplication.CreateBuilder(args);

// ============================================================
// Database
// ============================================================
builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));

// ============================================================
// IDistributedCache — Dev: In-Memory | Production: Redis
// Cùng 1 interface IDistributedCache → TokenDenylistService không thay đổi
// ============================================================
if (builder.Environment.IsDevelopment())
{
    // Dev: In-Memory cache, không cần cài Redis server
    builder.Services.AddDistributedMemoryCache();
}
else
{
    // Production: Redis thật — đọc connection string từ appsettings.json
    builder.Services.AddStackExchangeRedisCache(options =>
    {
        options.Configuration = builder.Configuration.GetConnectionString("Redis");
        options.InstanceName = "DemoWebAPI:"; // Prefix key trong Redis
    });
}

// ============================================================
// Dependency Injection — Auth Services (Sub-components của Facade)
// ============================================================
builder.Services.AddScoped<ITokenService, TokenService>();
builder.Services.AddScoped<IAccountService, AccountService>();
builder.Services.AddScoped<IWorkspaceService, WorkspaceService>();
builder.Services.AddScoped<IWorkspaceMemberProfileService, WorkspaceMemberProfileService>();
builder.Services.AddScoped<IProjectService, ProjectService>();
builder.Services.AddScoped<ITaskService, TaskService>();
builder.Services.AddScoped<ITimesheetService, TimesheetService>();
builder.Services.AddScoped<IExpenseService, ExpenseService>();
builder.Services.AddScoped<IRevenueService, RevenueService>();
builder.Services.AddScoped<IRiskService, RiskService>();
builder.Services.AddScoped<IConversationService, ConversationService>();
builder.Services.AddScoped<INotificationService, NotificationService>();
builder.Services.AddScoped<IFirebasePushService, FirebasePushService>();
builder.Services.AddSingleton<INotificationQueue, NotificationQueue>();
builder.Services.AddHostedService<NotificationDispatcherService>();
builder.Services.AddSingleton<IProfileCalculationQueue, ProfileCalculationQueue>();
builder.Services.AddHostedService<ProfileCalculationDispatcherService>();
builder.Services.AddHostedService<ProfilePeriodicalBackgroundService>();
builder.Services.AddScoped<IMessageService, MessageService>();
builder.Services.AddScoped<IProjectAssetService, ProjectAssetService>();
builder.Services.AddScoped<IAIInsightService, AIInsightService>();
builder.Services.AddScoped<IAIAnalysisService, AIAnalysisService>();
builder.Services.AddScoped<IAIProvider, MockAIProvider>();
builder.Services.AddScoped<IRequestService, RequestService>();
builder.Services.AddScoped<ISessionService, SessionService>();
builder.Services.AddScoped<ITokenDenylistService, TokenDenylistService>();
builder.Services.AddScoped<RequireActiveAccountFilter>();
builder.Services.AddScoped<RequireSystemAccountFilter>();

// ============================================================
// Dependency Injection — Storage Strategy + Factory
// ============================================================
builder.Services.AddScoped<AzureBlobStorageStrategy>();
builder.Services.AddScoped<IStorageStrategy, AzureBlobStorageStrategy>();
builder.Services.AddScoped<StorageFactory>();

// ============================================================
// Dependency Injection — Simple Factory + Strategy Pattern (Auth)
// ============================================================
builder.Services.AddScoped<LocalLoginStrategy>();
builder.Services.AddScoped<LocalRegisterStrategy>();
builder.Services.AddScoped<GoogleAuthStrategy>();
builder.Services.AddScoped<IAuthStrategyFactory, AuthStrategyFactory>();

// ============================================================
// Dependency Injection — Chain of Responsibility (Refresh Token Validation)
// ============================================================
builder.Services.AddScoped<TokenExistsHandler>();
builder.Services.AddScoped<TokenNotRevokedHandler>();
builder.Services.AddScoped<TokenNotExpiredHandler>();
builder.Services.AddScoped<AccountActiveHandler>();

// ============================================================
// Dependency Injection — Command Pattern (Logout / Revoke)
// ============================================================
builder.Services.AddScoped<ILogoutCommandHandler, RevokeSessionCommandHandler>();

// ============================================================
// Dependency Injection — Facade (tích hợp tất cả bên trên)
// ============================================================
builder.Services.AddScoped<IAuthFacade, AuthFacade>();

// ============================================================
// Dependency Injection — Quota Services
// ============================================================
builder.Services.AddScoped<IFeatureQuotaService, FeatureQuotaService>();

// ============================================================
// Dependency Injection — Event-Driven Framework
// ============================================================
builder.Services.AddScoped<IEventPublisher, EventPublisher>();

// Auto-register all IEventHandler<T> implementations
var eventHandlerTypes = typeof(Program).Assembly.GetTypes()
    .Where(t => !t.IsAbstract && !t.IsInterface &&
                t.GetInterfaces().Any(i => i.IsGenericType && i.GetGenericTypeDefinition() == typeof(IEventHandler<>)))
    .ToList();

foreach (var handlerType in eventHandlerTypes)
{
    var interfaces = handlerType.GetInterfaces()
        .Where(i => i.IsGenericType && i.GetGenericTypeDefinition() == typeof(IEventHandler<>));

    foreach (var interfaceType in interfaces)
    {
        builder.Services.AddScoped(interfaceType, handlerType);
    }
}

// HttpClient cho Google OAuth2 API
builder.Services.AddHttpClient("Google", client =>
{
    client.BaseAddress = new Uri("https://oauth2.googleapis.com/");
    client.Timeout = TimeSpan.FromSeconds(10);
});

// ============================================================
// JWT Authentication
// MapInboundClaims = false → giữ tên claim gốc ("sub", "jti", "exp")
// Không bị .NET map sang ClaimTypes.NameIdentifier v.v.
// ============================================================
var jwtSettings = builder.Configuration.GetSection("JwtSettings");
var secretKey = jwtSettings["SecretKey"] ?? throw new ArgumentNullException("SecretKey is missing");

builder.Services.AddAuthentication(options =>
{
    options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
    options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
})
.AddJwtBearer(options =>
{
    // Giữ tên claim gốc từ JWT — quan trọng để Controller đọc "sub", "jti", "exp" đúng
    options.MapInboundClaims = false;

    options.TokenValidationParameters = new TokenValidationParameters
    {
        ValidateIssuer = true,
        ValidateAudience = true,
        ValidateLifetime = true,
        ValidateIssuerSigningKey = true,
        ValidIssuer = jwtSettings["Issuer"],
        ValidAudience = jwtSettings["Audience"],
        IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(secretKey)),
        // Không có ClockSkew — token hết hạn là hết ngay (không dung sai thêm)
        ClockSkew = TimeSpan.Zero
    };

    options.Events = new JwtBearerEvents
    {
        OnMessageReceived = context =>
        {
            var accessToken = context.Request.Query["access_token"];
            var path = context.HttpContext.Request.Path;

            if (!string.IsNullOrEmpty(accessToken)
                && (path.StartsWithSegments("/hubs/conversation") || path.StartsWithSegments("/hubs/notifications")))
            {
                context.Token = accessToken;
            }

            return Task.CompletedTask;
        }
    };
});

// ============================================================
// Rate Limiting (Spam Prevention)
// ============================================================
var rateLimitSettings = builder.Configuration.GetSection("RateLimiting");
var globalSettings = rateLimitSettings.GetSection("Global");
var authSettings = rateLimitSettings.GetSection("Auth");

builder.Services.AddRateLimiter(options =>
{
    // Phản hồi 429 định dạng JSON chuẩn ApiResponse
    options.OnRejected = async (context, token) =>
    {
        context.HttpContext.Response.StatusCode = StatusCodes.Status429TooManyRequests;
        context.HttpContext.Response.ContentType = "application/json";

        var response = new ApiResponse
        {
            Message = "Too many requests. Please try again later.",
            ErrorCode = "TOO_MANY_REQUESTS"
        };

        await context.HttpContext.Response.WriteAsJsonAsync(response, cancellationToken: token);
    };

    // Đăng ký bộ giới hạn dùng chung (Global) phân vùng theo User ID / IP sử dụng Token Bucket
    options.GlobalLimiter = PartitionedRateLimiter.Create<HttpContext, string>(httpContext =>
    {
        string partitionKey;
        var path = httpContext.Request.Path.Value ?? "";

        // Kiểm tra xem yêu cầu có thuộc API Auth không để áp dụng giới hạn nghiêm ngặt hơn
        bool isAuthRoute = path.Contains("/api/v1/auth/", StringComparison.OrdinalIgnoreCase);

        // 1. Phân loại định danh (Partition Key)
        // Nếu user đã login -> dùng User ID làm key. Ngược lại -> dùng IP thực của client.
        var userIdClaim = httpContext.User.FindFirst(JwtRegisteredClaimNames.Sub)?.Value 
                          ?? httpContext.User.FindFirst(ClaimTypes.NameIdentifier)?.Value;

        if (!string.IsNullOrEmpty(userIdClaim))
        {
            partitionKey = $"User_{userIdClaim}";
        }
        else
        {
            var ip = httpContext.Connection.RemoteIpAddress?.ToString() ?? "unknown_ip";
            partitionKey = $"IP_{ip}";
        }

        // Thêm nhãn loại route vào key để tách biệt quota của auth và global
        partitionKey = isAuthRoute ? $"Auth_{partitionKey}" : $"Global_{partitionKey}";

        // 2. Trả về TokenBucketRateLimiter tương ứng với chính sách cấu hình
        if (isAuthRoute)
        {
            return RateLimitPartition.GetTokenBucketLimiter(partitionKey, _ => new TokenBucketRateLimiterOptions
            {
                TokenLimit = authSettings.GetValue<int>("TokenLimit", 10),
                TokensPerPeriod = authSettings.GetValue<int>("TokensPerPeriod", 2),
                ReplenishmentPeriod = TimeSpan.FromSeconds(authSettings.GetValue<int>("ReplenishmentPeriodSeconds", 15)),
                QueueLimit = authSettings.GetValue<int>("QueueLimit", 0),
                AutoReplenishment = true
            });
        }
        else
        {
            return RateLimitPartition.GetTokenBucketLimiter(partitionKey, _ => new TokenBucketRateLimiterOptions
            {
                TokenLimit = globalSettings.GetValue<int>("TokenLimit", 100),
                TokensPerPeriod = globalSettings.GetValue<int>("TokensPerPeriod", 20),
                ReplenishmentPeriod = TimeSpan.FromSeconds(globalSettings.GetValue<int>("ReplenishmentPeriodSeconds", 10)),
                QueueLimit = globalSettings.GetValue<int>("QueueLimit", 0),
                AutoReplenishment = true
            });
        }
    });
});

// ============================================================
// Add services
// ============================================================
builder.Services.AddControllers();
builder.Services.AddSignalR();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(options =>
{
    var xmlFilename = $"{System.Reflection.Assembly.GetExecutingAssembly().GetName().Name}.xml";
    options.IncludeXmlComments(System.IO.Path.Combine(AppContext.BaseDirectory, xmlFilename));

    // 1. JWT Bearer token security definition
    options.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        Name = "Authorization",
        Type = SecuritySchemeType.ApiKey,
        Scheme = "Bearer",
        BearerFormat = "JWT",
        In = ParameterLocation.Header,
        Description = "JWT Authorization header using the Bearer scheme. \r\n\r\n Enter 'Bearer' [space] and then your token in the text input below.\r\n\r\nExample: \"Bearer 12345abcdef\""
    });

    // 2. Test validation header security definition (dynamic from configuration)
    var headerName = builder.Configuration["TestTokenSettings:HeaderName"] ?? "X-Alloc-Test-Token";
    options.AddSecurityDefinition("TestToken", new OpenApiSecurityScheme
    {
        Name = headerName,
        Type = SecuritySchemeType.ApiKey,
        In = ParameterLocation.Header,
        Description = $"Security validation header required in non-Production environments.\r\n\r\nEnter the configured secret test validation token (Header: '{headerName}')."
    });

    // 3. Apply these security requirements globally to all API operations
    options.AddSecurityRequirement(new OpenApiSecurityRequirement
    {
        {
            new OpenApiSecurityScheme
            {
                Reference = new OpenApiReference
                {
                    Type = ReferenceType.SecurityScheme,
                    Id = "Bearer"
                }
            },
            System.Array.Empty<string>()
        },
        {
            new OpenApiSecurityScheme
            {
                Reference = new OpenApiReference
                {
                    Type = ReferenceType.SecurityScheme,
                    Id = "TestToken"
                }
            },
            System.Array.Empty<string>()
        }
    });
});

var app = builder.Build();

app.UseForwardedHeaders(new ForwardedHeadersOptions
{
    ForwardedHeaders = ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto
});

await SeedTimesheetPermissionsAsync(app.Services);
await SeedExpensePermissionsAsync(app.Services);
await SeedRevenuePermissionsAsync(app.Services);
await SeedRequestPermissionsAsync(app.Services);
await SeedRiskPermissionsAsync(app.Services);
await SeedAIPermissionsAsync(app.Services);
await SeedAssetPermissionsAsync(app.Services);
await SeedTaskPermissionsAsync(app.Services);
await SeedConversationPermissionsAsync(app.Services);
await SeedMemberProfilePermissionsAsync(app.Services);

// ============================================================
// HTTP Pipeline
// ============================================================
var testTokenSettings = app.Configuration.GetSection("TestTokenSettings");
if (testTokenSettings.GetValue<bool>("Enabled", false) && !app.Environment.IsProduction())
{
    app.UseMiddleware<TestTokenValidationMiddleware>();
}

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();

// 1. Xác thực JWT (validate signature, lifetime...)
app.UseAuthentication();

// 2. Giới hạn tần suất request (Rate Limiting) chống spam
app.UseRateLimiter();

// 3. Kiểm tra JWT Denylist (sau khi xác thực, trước khi authorize)
//    Chặn ngay nếu JTI có trong In-Memory/Redis cache
app.UseMiddleware<TokenDenylistMiddleware>();

// 3. Phân quyền
app.UseAuthorization();

app.MapControllers();
app.MapHub<ConversationHub>("/hubs/conversation");
app.MapHub<NotificationHub>("/hubs/notifications");

app.Run();

static async Task SeedTimesheetPermissionsAsync(IServiceProvider services)
{
    using var scope = services.CreateScope();
    var dbContext = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

    var permissions = new[]
    {
        new WorkspacePermission
        {
            PermissionID = TimesheetPermissionIds.ViewAll,
            DisplayName = "View all workspace timesheets"
        },
        new WorkspacePermission
        {
            PermissionID = TimesheetPermissionIds.EditAll,
            DisplayName = "Edit all workspace timesheets"
        }
    };

    foreach (var permission in permissions)
    {
        var existingPermission = await dbContext.WorkspacePermissions
            .FirstOrDefaultAsync(item => item.PermissionID == permission.PermissionID);

        if (existingPermission == null)
        {
            dbContext.WorkspacePermissions.Add(permission);
        }
        else
        {
            existingPermission.DisplayName = permission.DisplayName;
        }
    }

    await dbContext.SaveChangesAsync();

    var ownerRoleIds = await dbContext.WorkspaceRoles
        .Where(role =>
            role.RoleName == "Owner"
            && !role.IsDeleted)
        .Select(role => role.WorkspaceRoleID)
        .ToListAsync();

    foreach (var ownerRoleId in ownerRoleIds)
    {
        foreach (var permission in permissions)
        {
            var exists = await dbContext.RolePermissions
                .AnyAsync(item =>
                    item.WorkspaceRoleID == ownerRoleId
                    && item.PermissionID == permission.PermissionID);

            if (!exists)
            {
                dbContext.RolePermissions.Add(new RolePermission
                {
                    WorkspaceRoleID = ownerRoleId,
                    PermissionID = permission.PermissionID
                });
            }
        }
    }

    await dbContext.SaveChangesAsync();
}

static async Task SeedExpensePermissionsAsync(IServiceProvider services)
{
    using var scope = services.CreateScope();
    var dbContext = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

    var permissions = new[]
    {
        new WorkspacePermission
        {
            PermissionID = ExpensePermissionIds.View,
            DisplayName = "View project expenses"
        },
        new WorkspacePermission
        {
            PermissionID = ExpensePermissionIds.Create,
            DisplayName = "Create project expenses"
        }
    };

    foreach (var permission in permissions)
    {
        var existingPermission = await dbContext.WorkspacePermissions
            .FirstOrDefaultAsync(item => item.PermissionID == permission.PermissionID);

        if (existingPermission == null)
        {
            dbContext.WorkspacePermissions.Add(permission);
        }
        else
        {
            existingPermission.DisplayName = permission.DisplayName;
        }
    }

    await dbContext.SaveChangesAsync();

    var ownerRoleIds = await dbContext.WorkspaceRoles
        .Where(role =>
            role.RoleName == "Owner"
            && !role.IsDeleted)
        .Select(role => role.WorkspaceRoleID)
        .ToListAsync();

    foreach (var ownerRoleId in ownerRoleIds)
    {
        foreach (var permission in permissions)
        {
            var exists = await dbContext.RolePermissions
                .AnyAsync(item =>
                    item.WorkspaceRoleID == ownerRoleId
                    && item.PermissionID == permission.PermissionID);

            if (!exists)
            {
                dbContext.RolePermissions.Add(new RolePermission
                {
                    WorkspaceRoleID = ownerRoleId,
                    PermissionID = permission.PermissionID
                });
            }
        }
    }

    await dbContext.SaveChangesAsync();
}

static async Task SeedRevenuePermissionsAsync(IServiceProvider services)
{
    using var scope = services.CreateScope();
    var dbContext = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

    var permissions = new[]
    {
        new WorkspacePermission
        {
            PermissionID = RevenuePermissionIds.View,
            DisplayName = "View project revenues"
        }
    };

    foreach (var permission in permissions)
    {
        var existingPermission = await dbContext.WorkspacePermissions
            .FirstOrDefaultAsync(item => item.PermissionID == permission.PermissionID);

        if (existingPermission == null)
        {
            dbContext.WorkspacePermissions.Add(permission);
        }
        else
        {
            existingPermission.DisplayName = permission.DisplayName;
        }
    }

    await dbContext.SaveChangesAsync();

    var ownerRoleIds = await dbContext.WorkspaceRoles
        .Where(role =>
            role.RoleName == "Owner"
            && !role.IsDeleted)
        .Select(role => role.WorkspaceRoleID)
        .ToListAsync();

    foreach (var ownerRoleId in ownerRoleIds)
    {
        foreach (var permission in permissions)
        {
            var exists = await dbContext.RolePermissions
                .AnyAsync(item =>
                    item.WorkspaceRoleID == ownerRoleId
                    && item.PermissionID == permission.PermissionID);

            if (!exists)
            {
                dbContext.RolePermissions.Add(new RolePermission
                {
                    WorkspaceRoleID = ownerRoleId,
                    PermissionID = permission.PermissionID
                });
            }
        }
    }

    await dbContext.SaveChangesAsync();
}

static async Task SeedRequestPermissionsAsync(IServiceProvider services)
{
    using var scope = services.CreateScope();
    var dbContext = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

    var permissions = new[]
    {
        new WorkspacePermission
        {
            PermissionID = RequestPermissionIds.Approve,
            DisplayName = "Approve or reject workspace requests"
        }
    };

    foreach (var permission in permissions)
    {
        var existingPermission = await dbContext.WorkspacePermissions
            .FirstOrDefaultAsync(item => item.PermissionID == permission.PermissionID);

        if (existingPermission == null)
        {
            dbContext.WorkspacePermissions.Add(permission);
        }
        else
        {
            existingPermission.DisplayName = permission.DisplayName;
        }
    }

    await dbContext.SaveChangesAsync();

    var ownerRoleIds = await dbContext.WorkspaceRoles
        .Where(role =>
            role.RoleName == "Owner"
            && !role.IsDeleted)
        .Select(role => role.WorkspaceRoleID)
        .ToListAsync();

    foreach (var ownerRoleId in ownerRoleIds)
    {
        foreach (var permission in permissions)
        {
            var exists = await dbContext.RolePermissions
                .AnyAsync(item =>
                    item.WorkspaceRoleID == ownerRoleId
                    && item.PermissionID == permission.PermissionID);

            if (!exists)
            {
                dbContext.RolePermissions.Add(new RolePermission
                {
                    WorkspaceRoleID = ownerRoleId,
                    PermissionID = permission.PermissionID
                });
            }
        }
    }

    await dbContext.SaveChangesAsync();
}

static async Task SeedRiskPermissionsAsync(IServiceProvider services)
{
    using var scope = services.CreateScope();
    var dbContext = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

    var permissions = new[]
    {
        new WorkspacePermission
        {
            PermissionID = RiskPermissionIds.View,
            DisplayName = "View project risks"
        },
        new WorkspacePermission
        {
            PermissionID = RiskPermissionIds.Create,
            DisplayName = "Create project risks"
        }
    };

    foreach (var permission in permissions)
    {
        var existingPermission = await dbContext.WorkspacePermissions
            .FirstOrDefaultAsync(item => item.PermissionID == permission.PermissionID);

        if (existingPermission == null)
        {
            dbContext.WorkspacePermissions.Add(permission);
        }
        else
        {
            existingPermission.DisplayName = permission.DisplayName;
        }
    }

    await dbContext.SaveChangesAsync();

    var ownerRoleIds = await dbContext.WorkspaceRoles
        .Where(role =>
            role.RoleName == "Owner"
            && !role.IsDeleted)
        .Select(role => role.WorkspaceRoleID)
        .ToListAsync();

    foreach (var ownerRoleId in ownerRoleIds)
    {
        foreach (var permission in permissions)
        {
            var exists = await dbContext.RolePermissions
                .AnyAsync(item =>
                    item.WorkspaceRoleID == ownerRoleId
                    && item.PermissionID == permission.PermissionID);

            if (!exists)
            {
                dbContext.RolePermissions.Add(new RolePermission
                {
                    WorkspaceRoleID = ownerRoleId,
                    PermissionID = permission.PermissionID
                });
            }
        }
    }

    await dbContext.SaveChangesAsync();
}

static async Task SeedAIPermissionsAsync(IServiceProvider services)
{
    using var scope = services.CreateScope();
    var dbContext = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

    var permissions = new[]
    {
        new WorkspacePermission
        {
            PermissionID = AIPermissionIds.View,
            DisplayName = "View project AI insights"
        },
        new WorkspacePermission
        {
            PermissionID = AIPermissionIds.Ask,
            DisplayName = "Ask project AI assistant"
        }
    };

    foreach (var permission in permissions)
    {
        var existingPermission = await dbContext.WorkspacePermissions
            .FirstOrDefaultAsync(item => item.PermissionID == permission.PermissionID);

        if (existingPermission == null)
        {
            dbContext.WorkspacePermissions.Add(permission);
        }
        else
        {
            existingPermission.DisplayName = permission.DisplayName;
        }
    }

    await dbContext.SaveChangesAsync();

    var ownerRoleIds = await dbContext.WorkspaceRoles
        .Where(role =>
            role.RoleName == "Owner"
            && !role.IsDeleted)
        .Select(role => role.WorkspaceRoleID)
        .ToListAsync();

    foreach (var ownerRoleId in ownerRoleIds)
    {
        foreach (var permission in permissions)
        {
            var exists = await dbContext.RolePermissions
                .AnyAsync(item =>
                    item.WorkspaceRoleID == ownerRoleId
                    && item.PermissionID == permission.PermissionID);

            if (!exists)
            {
                dbContext.RolePermissions.Add(new RolePermission
                {
                    WorkspaceRoleID = ownerRoleId,
                    PermissionID = permission.PermissionID
                });
            }
        }
    }

    await dbContext.SaveChangesAsync();
}

static async Task SeedAssetPermissionsAsync(IServiceProvider services)
{
    using var scope = services.CreateScope();
    var dbContext = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

    var permissions = new[]
    {
        new WorkspacePermission
        {
            PermissionID = AssetPermissionIds.View,
            DisplayName = "View project assets"
        },
        new WorkspacePermission
        {
            PermissionID = AssetPermissionIds.Create,
            DisplayName = "Upload project assets"
        },
        new WorkspacePermission
        {
            PermissionID = AssetPermissionIds.Delete,
            DisplayName = "Delete project assets"
        }
    };

    foreach (var permission in permissions)
    {
        var existingPermission = await dbContext.WorkspacePermissions
            .FirstOrDefaultAsync(item => item.PermissionID == permission.PermissionID);

        if (existingPermission == null)
        {
            dbContext.WorkspacePermissions.Add(permission);
        }
        else
        {
            existingPermission.DisplayName = permission.DisplayName;
        }
    }

    await dbContext.SaveChangesAsync();

    var ownerRoleIds = await dbContext.WorkspaceRoles
        .Where(role =>
            role.RoleName == "Owner"
            && !role.IsDeleted)
        .Select(role => role.WorkspaceRoleID)
        .ToListAsync();

    foreach (var ownerRoleId in ownerRoleIds)
    {
        foreach (var permission in permissions)
        {
            var exists = await dbContext.RolePermissions
                .AnyAsync(item =>
                    item.WorkspaceRoleID == ownerRoleId
                    && item.PermissionID == permission.PermissionID);

            if (!exists)
            {
                dbContext.RolePermissions.Add(new RolePermission
                {
                    WorkspaceRoleID = ownerRoleId,
                    PermissionID = permission.PermissionID
                });
            }
        }
    }

    await dbContext.SaveChangesAsync();
}

static async Task SeedTaskPermissionsAsync(IServiceProvider services)
{
    using var scope = services.CreateScope();
    var dbContext = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

    var permissions = new[]
    {
        new WorkspacePermission
        {
            PermissionID = TaskPermissionIds.View,
            DisplayName = "View project tasks"
        },
        new WorkspacePermission
        {
            PermissionID = TaskPermissionIds.Create,
            DisplayName = "Create project tasks"
        },
        new WorkspacePermission
        {
            PermissionID = TaskPermissionIds.Update,
            DisplayName = "Update project tasks"
        },
        new WorkspacePermission
        {
            PermissionID = TaskPermissionIds.Delete,
            DisplayName = "Delete project tasks"
        }
    };

    foreach (var permission in permissions)
    {
        var existingPermission = await dbContext.WorkspacePermissions
            .FirstOrDefaultAsync(item => item.PermissionID == permission.PermissionID);

        if (existingPermission == null)
        {
            dbContext.WorkspacePermissions.Add(permission);
        }
        else
        {
            existingPermission.DisplayName = permission.DisplayName;
        }
    }

    await dbContext.SaveChangesAsync();

    var ownerRoleIds = await dbContext.WorkspaceRoles
        .Where(role =>
            role.RoleName == "Owner"
            && !role.IsDeleted)
        .Select(role => role.WorkspaceRoleID)
        .ToListAsync();

    foreach (var ownerRoleId in ownerRoleIds)
    {
        foreach (var permission in permissions)
        {
            var exists = await dbContext.RolePermissions
                .AnyAsync(item =>
                    item.WorkspaceRoleID == ownerRoleId
                    && item.PermissionID == permission.PermissionID);

            if (!exists)
            {
                dbContext.RolePermissions.Add(new RolePermission
                {
                    WorkspaceRoleID = ownerRoleId,
                    PermissionID = permission.PermissionID
                });
            }
        }
    }

    await dbContext.SaveChangesAsync();
}

static async Task SeedConversationPermissionsAsync(IServiceProvider services)
{
    using var scope = services.CreateScope();
    var dbContext = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

    var permissions = new[]
    {
        new WorkspacePermission
        {
            PermissionID = ConversationPermissionIds.Manage,
            DisplayName = "Manage workspace conversations"
        }
    };

    foreach (var permission in permissions)
    {
        var existingPermission = await dbContext.WorkspacePermissions
            .FirstOrDefaultAsync(item => item.PermissionID == permission.PermissionID);

        if (existingPermission == null)
        {
            dbContext.WorkspacePermissions.Add(permission);
        }
        else
        {
            existingPermission.DisplayName = permission.DisplayName;
        }
    }

    await dbContext.SaveChangesAsync();

    var ownerRoleIds = await dbContext.WorkspaceRoles
        .Where(role =>
            role.RoleName == "Owner"
            && !role.IsDeleted)
        .Select(role => role.WorkspaceRoleID)
        .ToListAsync();

    foreach (var ownerRoleId in ownerRoleIds)
    {
        foreach (var permission in permissions)
        {
            var exists = await dbContext.RolePermissions
                .AnyAsync(item =>
                    item.WorkspaceRoleID == ownerRoleId
                    && item.PermissionID == permission.PermissionID);

            if (!exists)
            {
                dbContext.RolePermissions.Add(new RolePermission
                {
                    WorkspaceRoleID = ownerRoleId,
                    PermissionID = permission.PermissionID
                });
            }
        }
    }

    await dbContext.SaveChangesAsync();
}

static async Task SeedMemberProfilePermissionsAsync(IServiceProvider services)
{
    using var scope = services.CreateScope();
    var dbContext = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

    var permissions = new[]
    {
        new WorkspacePermission
        {
            PermissionID = MemberProfilePermissionIds.View,
            DisplayName = "View workspace member profiles"
        },
        new WorkspacePermission
        {
            PermissionID = MemberProfilePermissionIds.Manage,
            DisplayName = "Manage and edit workspace member profiles"
        }
    };

    foreach (var permission in permissions)
    {
        var existingPermission = await dbContext.WorkspacePermissions
            .FirstOrDefaultAsync(item => item.PermissionID == permission.PermissionID);

        if (existingPermission == null)
        {
            dbContext.WorkspacePermissions.Add(permission);
        }
        else
        {
            existingPermission.DisplayName = permission.DisplayName;
        }
    }

    await dbContext.SaveChangesAsync();

    var ownerRoleIds = await dbContext.WorkspaceRoles
        .Where(role =>
            role.RoleName == "Owner"
            && !role.IsDeleted)
        .Select(role => role.WorkspaceRoleID)
        .ToListAsync();

    foreach (var ownerRoleId in ownerRoleIds)
    {
        foreach (var permission in permissions)
        {
            var exists = await dbContext.RolePermissions
                .AnyAsync(item =>
                    item.WorkspaceRoleID == ownerRoleId
                    && item.PermissionID == permission.PermissionID);

            if (!exists)
            {
                dbContext.RolePermissions.Add(new RolePermission
                {
                    WorkspaceRoleID = ownerRoleId,
                    PermissionID = permission.PermissionID
                });
            }
        }
    }

    await dbContext.SaveChangesAsync();
}
