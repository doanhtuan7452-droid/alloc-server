using System;
using System.IO;
using System.Reflection;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Microsoft.OpenApi.Models;
using AllocServer.Configurations;

namespace AllocServer.Extensions
{
    public static class SwaggerExtensions
    {
        public static IServiceCollection AddApiDocumentation(this IServiceCollection services, IConfiguration configuration)
        {
            services.AddControllers();
            services.AddSignalR();
            services.AddEndpointsApiExplorer();
            services.AddSwaggerGen(options =>
            {
                var xmlFilename = $"{Assembly.GetExecutingAssembly().GetName().Name}.xml";
                var xmlPath = Path.Combine(AppContext.BaseDirectory, xmlFilename);
                if (File.Exists(xmlPath))
                {
                    options.IncludeXmlComments(xmlPath);
                }

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

                // 2. Test validation header security definition (dynamic from configuration options)
                var serviceProvider = services.BuildServiceProvider();
                var testTokenOptions = serviceProvider.GetRequiredService<IOptions<TestTokenSettings>>().Value;
                var headerName = !string.IsNullOrEmpty(testTokenOptions.HeaderName) 
                    ? testTokenOptions.HeaderName 
                    : "X-Alloc-Test-Token";

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
                        Array.Empty<string>()
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
                        Array.Empty<string>()
                    }
                });
            });

            return services;
        }
    }
}
