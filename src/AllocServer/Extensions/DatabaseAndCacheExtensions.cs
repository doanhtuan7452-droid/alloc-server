using AllocServer.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace AllocServer.Extensions
{
    public static class DatabaseAndCacheExtensions
    {
        public static IServiceCollection AddDatabaseAndCache(this IServiceCollection services, IConfiguration configuration, IHostEnvironment environment)
        {
            services.AddDbContext<ApplicationDbContext>(options =>
                options.UseSqlServer(configuration.GetConnectionString("DefaultConnection")));

            if (environment.IsDevelopment())
            {
                services.AddDistributedMemoryCache();
            }
            else
            {
                var redisConn = configuration.GetConnectionString("Redis");
                services.AddStackExchangeRedisCache(options =>
                {
                    options.Configuration = redisConn;
                    options.InstanceName = "DemoWebAPI:";
                });

                services.AddSingleton<StackExchange.Redis.IConnectionMultiplexer>(
                    StackExchange.Redis.ConnectionMultiplexer.Connect(redisConn));
            }

            return services;
        }
    }
}
