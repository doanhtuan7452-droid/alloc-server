using AllocServer.Extensions;
using AllocServer.Middleware;
using AllocServer.Hubs;
using Microsoft.AspNetCore.HttpOverrides;
using AllocServer.Exceptions;

var builder = WebApplication.CreateBuilder(args);

// 1. Infrastructure (Database & Distributed Cache)
builder.Services.AddDatabaseAndCache(builder.Configuration, builder.Environment);

// 2. App Configurations (Options Pattern mapping)
builder.Services.AddAppConfigurations(builder.Configuration);

// 3. Dependency Injection (Business Services, Strategies, Event Handlers, Events)
builder.Services.AddBusinessServices();
builder.Services.AddEventHandlers();

// 4. Authentication & Security (JWT and Rate Limiting)
builder.Services.AddAuthAndSecurity(builder.Configuration);

// 5. API presentation & documentation (Controllers, SignalR, Swagger)
builder.Services.AddApiDocumentation(builder.Configuration);

// CORS Policy registration
builder.Services.AddCors(options =>
{
    var allowedOrigins = builder.Configuration.GetSection("CorsSettings:AllowedOrigins").Get<string[]>()
                         ?? new[] { "http://localhost:3000", "https://alloc.doanhtuan.com" };
    options.AddPolicy("CorsPolicy", policy =>
    {
        policy.WithOrigins(allowedOrigins)
              .AllowAnyMethod()
              .AllowAnyHeader()
              .AllowCredentials();
    });
});

// 6. Localization & Exception Handling
builder.Services.AddLocalization(options => options.ResourcesPath = "Resources");
builder.Services.AddExceptionHandler<GlobalExceptionHandler>();
builder.Services.AddProblemDetails();

var app = builder.Build();

// ============================================================
// HTTP Middleware Pipeline Configuration
// (Strict order: ForwardedHeaders -> Swagger -> Routing -> Cors -> Authentication -> RateLimiter -> Denylist -> Authorization -> Controllers/Hubs)
// ============================================================

// Proxy headers handling - Secure proxy configuration
var forwardedOptions = new ForwardedHeadersOptions
{
    ForwardedHeaders = ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto
};

var trustedProxies = app.Configuration.GetSection("SecuritySettings:TrustedProxies").Get<string[]>();
if (trustedProxies != null && trustedProxies.Length > 0)
{
    forwardedOptions.KnownProxies.Clear();
    foreach (var proxy in trustedProxies)
    {
        forwardedOptions.KnownProxies.Add(System.Net.IPAddress.Parse(proxy));
    }
}
else if (app.Environment.IsDevelopment())
{
    forwardedOptions.KnownNetworks.Clear();
    forwardedOptions.KnownProxies.Clear();
}
app.UseForwardedHeaders(forwardedOptions);

// Exception Handler (Phải đặt sớm trong pipeline)
app.UseExceptionHandler();

// Safe idempotent startup database permissions seeding
await app.SeedDatabasePermissionsAsync();

// Test Token Validation Middleware (Bắt buộc vô hiệu hóa trên Production)
if (!app.Environment.IsProduction())
{
    var testTokenSettings = app.Configuration.GetSection("TestTokenSettings");
    if (testTokenSettings.GetValue<bool>("Enabled", false))
    {
        app.UseMiddleware<TestTokenValidationMiddleware>();
    }
}

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();

app.UseWebSockets();

// Chèn Routing tường minh trước khi chạy Authentication & CORS & RateLimiter
app.UseRouting();

// Sử dụng CORS trước Authentication & Rate Limiting
app.UseCors("CorsPolicy");

// 1. Authentication
app.UseAuthentication();

// 2. Rate Limiting (Sau UseRouting và UseAuthentication)
app.UseRateLimiter();

// 3. Custom Token Denylist
app.UseMiddleware<TokenDenylistMiddleware>();

// 4. Authorization
app.UseAuthorization();

// 5. Routing mapping
app.MapControllers();
app.MapHub<ConversationHub>("/hubs/conversation");
app.MapHub<NotificationHub>("/hubs/notifications");

app.Run();
