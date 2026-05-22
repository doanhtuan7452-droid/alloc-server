using AllocServer.Interfaces.Notifications;
using FirebaseAdmin;
using FirebaseAdmin.Messaging;
using Google.Apis.Auth.OAuth2;
using System.Text;

namespace AllocServer.Services.Notification_Services
{
    public class FirebasePushService : IFirebasePushService
    {
        private const string DefaultAppName = "AllocServerNotifications";

        private readonly FirebaseMessaging? _messaging;
        private readonly ILogger<FirebasePushService> _logger;
        private readonly bool _isEnabled;

        public FirebasePushService(
            IConfiguration configuration,
            ILogger<FirebasePushService> logger)
        {
            _logger = logger;
            var firebaseSection = configuration.GetSection("Firebase");
            _isEnabled = firebaseSection.GetValue<bool>("Enabled");

            if (!_isEnabled)
            {
                _logger.LogInformation("Firebase push notifications are disabled.");
                return;
            }

            try
            {
                var appName = firebaseSection["AppName"] ?? DefaultAppName;
                var app = GetOrCreateFirebaseApp(firebaseSection, appName);
                _messaging = FirebaseMessaging.GetMessaging(app);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to initialize Firebase Admin SDK.");
                _isEnabled = false;
            }
        }

        public async Task<(bool IsSuccess, string? ErrorMessage)> SendPushNotificationAsync(
            string deviceToken,
            string title,
            string body,
            string referenceType,
            int referenceId)
        {
            if (!_isEnabled || _messaging == null)
            {
                return (true, null);
            }

            try
            {
                var message = new Message
                {
                    Token = deviceToken,
                    Notification = new FirebaseAdmin.Messaging.Notification
                    {
                        Title = title,
                        Body = body
                    },
                    Data = new Dictionary<string, string>
                    {
                        ["referenceType"] = referenceType,
                        ["referenceId"] = referenceId.ToString()
                    }
                };

                await _messaging.SendAsync(message);
                return (true, null);
            }
            catch (FirebaseMessagingException ex)
            {
                _logger.LogWarning(ex, "Firebase rejected device token.");
                return (false, ex.Message);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to send Firebase push notification.");
                return (false, ex.Message);
            }
        }

        private static FirebaseApp GetOrCreateFirebaseApp(
            IConfiguration firebaseSection,
            string appName)
        {
            try
            {
                return FirebaseApp.GetInstance(appName);
            }
            catch (InvalidOperationException)
            {
                var options = new AppOptions
                {
                    Credential = BuildGoogleCredential(firebaseSection),
                    ProjectId = firebaseSection["ProjectId"]
                };

                return FirebaseApp.Create(options, appName);
            }
        }

        private static GoogleCredential BuildGoogleCredential(IConfiguration firebaseSection)
        {
            var serviceAccountJson = firebaseSection["ServiceAccountJson"];
            if (!string.IsNullOrWhiteSpace(serviceAccountJson))
            {
                using var stream = new MemoryStream(Encoding.UTF8.GetBytes(serviceAccountJson));
                var serviceAccountCredential = ServiceAccountCredential.FromServiceAccountData(stream);
                return GoogleCredential.FromServiceAccountCredential(serviceAccountCredential);
            }

            var credentialPath = firebaseSection["CredentialPath"];
            if (!string.IsNullOrWhiteSpace(credentialPath))
            {
                using var stream = File.OpenRead(credentialPath);
                var serviceAccountCredential = ServiceAccountCredential.FromServiceAccountData(stream);
                return GoogleCredential.FromServiceAccountCredential(serviceAccountCredential);
            }

            return GoogleCredential.GetApplicationDefault();
        }
    }
}
