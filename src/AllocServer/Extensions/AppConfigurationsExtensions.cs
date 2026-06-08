using AllocServer.Configurations;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace AllocServer.Extensions
{
    public static class AppConfigurationsExtensions
    {
        public static IServiceCollection AddAppConfigurations(this IServiceCollection services, IConfiguration configuration)
        {
            services.Configure<JwtSettings>(configuration.GetSection("JwtSettings"));
            services.Configure<TestTokenSettings>(configuration.GetSection("TestTokenSettings"));
            services.Configure<GoogleSettings>(configuration.GetSection("GoogleSettings"));
            services.Configure<RateLimitSettings>(configuration.GetSection("RateLimiting"));

            return services;
        }
    }
}
