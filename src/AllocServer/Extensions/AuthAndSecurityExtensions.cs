using System;
using System.Text;
using System.Threading.Tasks;
using System.Threading.RateLimiting;
using System.Security.Claims;
using System.IdentityModel.Tokens.Jwt;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using AllocServer.Configurations;
using AllocServer.DTOs.Common;

namespace AllocServer.Extensions
{
    public static class AuthAndSecurityExtensions
    {
        public static IServiceCollection AddAuthAndSecurity(this IServiceCollection services, IConfiguration configuration)
        {
            var serviceProvider = services.BuildServiceProvider();
            var jwtOptions = serviceProvider.GetRequiredService<IOptions<JwtSettings>>().Value;
            var rateLimitOptions = serviceProvider.GetRequiredService<IOptions<RateLimitSettings>>().Value;

            // JWT Authentication setup
            var secretKey = jwtOptions.SecretKey;
            if (string.IsNullOrEmpty(secretKey))
            {
                throw new ArgumentNullException(nameof(secretKey), "JwtSettings:SecretKey is missing in configurations.");
            }

            services.AddAuthentication(options =>
            {
                options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
                options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
            })
            .AddJwtBearer(options =>
            {
                options.MapInboundClaims = false;

                options.TokenValidationParameters = new TokenValidationParameters
                {
                    ValidateIssuer = true,
                    ValidateAudience = true,
                    ValidateLifetime = true,
                    ValidateIssuerSigningKey = true,
                    ValidIssuer = jwtOptions.Issuer,
                    ValidAudience = jwtOptions.Audience,
                    IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(secretKey)),
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

            // Rate Limiting (Spam Prevention)
            services.AddRateLimiter(options =>
            {
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

                options.GlobalLimiter = PartitionedRateLimiter.Create<HttpContext, string>(httpContext =>
                {
                    string partitionKey;
                    var path = httpContext.Request.Path.Value ?? "";

                    bool isAuthRoute = path.Contains("/api/v1/auth/", StringComparison.OrdinalIgnoreCase);

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

                    partitionKey = isAuthRoute ? $"Auth_{partitionKey}" : $"Global_{partitionKey}";

                    if (isAuthRoute)
                    {
                        var auth = rateLimitOptions.Auth;
                        return RateLimitPartition.GetTokenBucketLimiter(partitionKey, _ => new TokenBucketRateLimiterOptions
                        {
                            TokenLimit = auth.TokenLimit > 0 ? auth.TokenLimit : 10,
                            TokensPerPeriod = auth.TokensPerPeriod > 0 ? auth.TokensPerPeriod : 2,
                            ReplenishmentPeriod = TimeSpan.FromSeconds(auth.ReplenishmentPeriodSeconds > 0 ? auth.ReplenishmentPeriodSeconds : 15),
                            QueueLimit = auth.QueueLimit,
                            AutoReplenishment = true
                        });
                    }
                    else
                    {
                        var global = rateLimitOptions.Global;
                        return RateLimitPartition.GetTokenBucketLimiter(partitionKey, _ => new TokenBucketRateLimiterOptions
                        {
                            TokenLimit = global.TokenLimit > 0 ? global.TokenLimit : 100,
                            TokensPerPeriod = global.TokensPerPeriod > 0 ? global.TokensPerPeriod : 20,
                            ReplenishmentPeriod = TimeSpan.FromSeconds(global.ReplenishmentPeriodSeconds > 0 ? global.ReplenishmentPeriodSeconds : 10),
                            QueueLimit = global.QueueLimit,
                            AutoReplenishment = true
                        });
                    }
                });
            });

            return services;
        }
    }
}
