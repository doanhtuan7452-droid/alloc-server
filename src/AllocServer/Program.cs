using AllocServer.Data;
using AllocServer.Filters;
using AllocServer.Interfaces;
using AllocServer.Interfaces.AI;
using AllocServer.Interfaces.AIInsights;
using AllocServer.Middleware;
using AllocServer.Services;
using AllocServer.Interfaces.Auth;
using AllocServer.Interfaces.Commands;
using AllocServer.Interfaces.Expenses;
using AllocServer.Interfaces.Facade;
using AllocServer.Interfaces.ProjectAssets;
using AllocServer.Interfaces.Projects;
using AllocServer.Interfaces.Register;
using AllocServer.Interfaces.Revenues;
using AllocServer.Interfaces.Requests;
using AllocServer.Interfaces.Risks;
using AllocServer.Interfaces.Storage;
using AllocServer.Interfaces.Tasks;
using AllocServer.Interfaces.Timesheets;
using AllocServer.Interfaces.Workspaces;
using AllocServer.Models;
using AllocServer.Services.AI_Services;
using AllocServer.Services.AIInsight_Services;
using AllocServer.Services.Auth_Services;
using AllocServer.Services.Command_Handlers;
using AllocServer.Services.Expense_Services;
using AllocServer.Services.Facade_Services;
using AllocServer.Services.ProjectAsset_Services;
using AllocServer.Services.Project_Services;
using AllocServer.Services.Revenue_Services;
using AllocServer.Services.Request_Services;
using AllocServer.Services.Register_Strategies;
using AllocServer.Services.Risk_Services;
using AllocServer.Services.Storage;
using AllocServer.Services.Task_Services;
using AllocServer.Services.Timesheet_Services;
using AllocServer.Services.Workspace_Services;
using AllocServer.Services.Token_Validation_Handlers;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using System.Text;

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
builder.Services.AddScoped<IProjectService, ProjectService>();
builder.Services.AddScoped<ITaskService, TaskService>();
builder.Services.AddScoped<ITimesheetService, TimesheetService>();
builder.Services.AddScoped<IExpenseService, ExpenseService>();
builder.Services.AddScoped<IRevenueService, RevenueService>();
builder.Services.AddScoped<IRiskService, RiskService>();
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
// Dependency Injection — Strategy Pattern (Register)
// ============================================================
builder.Services.AddScoped<LocalRegistrationStrategy>();
builder.Services.AddScoped<GoogleRegistrationStrategy>();
builder.Services.AddScoped<IRegisterStrategyContext, RegisterStrategyContext>();

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
});

// ============================================================
// Add services
// ============================================================
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var app = builder.Build();

await SeedTimesheetPermissionsAsync(app.Services);
await SeedExpensePermissionsAsync(app.Services);
await SeedRevenuePermissionsAsync(app.Services);
await SeedRequestPermissionsAsync(app.Services);
await SeedRiskPermissionsAsync(app.Services);
await SeedAIPermissionsAsync(app.Services);
await SeedAssetPermissionsAsync(app.Services);

// ============================================================
// HTTP Pipeline
// ============================================================
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();

// 1. Xác thực JWT (validate signature, lifetime...)
app.UseAuthentication();

// 2. Kiểm tra JWT Denylist (sau khi xác thực, trước khi authorize)
//    Chặn ngay nếu JTI có trong In-Memory/Redis cache
app.UseMiddleware<TokenDenylistMiddleware>();

// 3. Phân quyền
app.UseAuthorization();

app.MapControllers();

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
