using System;
using System.Linq;
using System.Reflection;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
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
using AllocServer.Filters;

namespace AllocServer.Extensions
{
    public static class DependencyInjectionExtensions
    {
        public static IServiceCollection AddBusinessServices(this IServiceCollection services)
        {
            // Business Services (Sub-components of Facade)
            services.AddScoped<ITokenService, TokenService>();
            services.AddScoped<IAccountService, AccountService>();
            services.AddScoped<IWorkspaceService, WorkspaceService>();
            services.AddScoped<IWorkspaceMemberProfileService, WorkspaceMemberProfileService>();
            services.AddScoped<IProjectService, ProjectService>();
            services.AddScoped<ITaskService, TaskService>();
            services.AddScoped<ITimesheetService, TimesheetService>();
            services.AddScoped<IExpenseService, ExpenseService>();
            services.AddScoped<IRevenueService, RevenueService>();
            services.AddScoped<IRiskService, RiskService>();
            services.AddScoped<IConversationService, ConversationService>();
            services.AddScoped<INotificationService, NotificationService>();
            services.AddScoped<IFirebasePushService, FirebasePushService>();
            services.AddSingleton<INotificationQueue, NotificationQueue>();
            services.AddHostedService<NotificationDispatcherService>();
            services.AddSingleton<IProfileCalculationQueue, ProfileCalculationQueue>();
            services.AddHostedService<ProfileCalculationDispatcherService>();
            services.AddHostedService<ProfilePeriodicalBackgroundService>();
            services.AddScoped<IMessageService, MessageService>();
            services.AddScoped<IProjectAssetService, ProjectAssetService>();
            services.AddScoped<IAIInsightService, AIInsightService>();
            services.AddScoped<IAIAnalysisService, AIAnalysisService>();
            services.AddScoped<IAIProvider, MockAIProvider>();
            services.AddScoped<IRequestService, RequestService>();
            services.AddScoped<ISessionService, SessionService>();
            services.AddScoped<ITokenDenylistService, TokenDenylistService>();
            services.AddScoped<RequireActiveAccountFilter>();
            services.AddScoped<RequireSystemAccountFilter>();

            // Storage Strategy + Factory
            services.AddScoped<AzureBlobStorageStrategy>();
            services.AddScoped<IStorageStrategy, AzureBlobStorageStrategy>();
            services.AddScoped<StorageFactory>();

            // Simple Factory + Strategy Pattern (Auth)
            services.AddScoped<LocalLoginStrategy>();
            services.AddScoped<LocalRegisterStrategy>();
            services.AddScoped<GoogleAuthStrategy>();
            services.AddScoped<IAuthStrategyFactory, AuthStrategyFactory>();

            // Chain of Responsibility (Refresh Token Validation)
            services.AddScoped<TokenExistsHandler>();
            services.AddScoped<TokenNotRevokedHandler>();
            services.AddScoped<TokenNotExpiredHandler>();
            services.AddScoped<AccountActiveHandler>();

            // Command Pattern (Logout / Revoke)
            services.AddScoped<ILogoutCommandHandler, RevokeSessionCommandHandler>();

            // Facade (combining all components above)
            services.AddScoped<IAuthFacade, AuthFacade>();

            // Quota Services
            services.AddScoped<IFeatureQuotaService, FeatureQuotaService>();

            // Event-Driven Framework
            services.AddScoped<IEventPublisher, EventPublisher>();

            // HttpClient for Google OAuth2 API
            services.AddHttpClient("Google", client =>
            {
                client.BaseAddress = new Uri("https://oauth2.googleapis.com/");
                client.Timeout = TimeSpan.FromSeconds(10);
            });

            return services;
        }

        public static IServiceCollection AddEventHandlers(this IServiceCollection services, params Assembly[] assemblies)
        {
            if (assemblies == null || assemblies.Length == 0)
            {
                // Fallback to checking the entry assembly of the application
                assemblies = new[] { typeof(Program).Assembly };
            }

            var loggerFactory = services.BuildServiceProvider().GetService<ILoggerFactory>();
            var logger = loggerFactory?.CreateLogger("EventHandlersRegistration") 
                         ?? NullLogger.Instance;

            int registeredCount = 0;

            foreach (var assembly in assemblies)
            {
                var eventHandlerTypes = assembly.GetTypes()
                    .Where(t => !t.IsAbstract && !t.IsInterface &&
                                t.GetInterfaces().Any(i => i.IsGenericType && i.GetGenericTypeDefinition() == typeof(IEventHandler<>)))
                    .ToList();

                foreach (var handlerType in eventHandlerTypes)
                {
                    var interfaces = handlerType.GetInterfaces()
                        .Where(i => i.IsGenericType && i.GetGenericTypeDefinition() == typeof(IEventHandler<>));

                    foreach (var interfaceType in interfaces)
                    {
                        services.AddScoped(interfaceType, handlerType);
                        logger.LogDebug("[DI] Registered event handler '{HandlerName}' for event '{EventName}'", 
                            handlerType.Name, interfaceType.GenericTypeArguments[0].Name);
                        registeredCount++;
                    }
                }
            }

            if (registeredCount == 0)
            {
                logger.LogWarning("[DI] No event handlers implementing IEventHandler<> were found in scanned assemblies.");
            }
            else
            {
                logger.LogInformation("[DI] Successfully registered {Count} event handler mappings via reflection.", registeredCount);
            }

            return services;
        }
    }
}
