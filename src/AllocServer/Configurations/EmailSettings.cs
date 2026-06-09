namespace AllocServer.Configurations
{
    public class EmailSettings
    {
        public string Provider { get; set; } = "GmailSmtp";
        public GmailSmtpSettings GmailSmtp { get; set; } = new();
        public AzureCommunicationSettings AzureCommunication { get; set; } = new();
    }

    public class GmailSmtpSettings
    {
        public string Host { get; set; } = "smtp.gmail.com";
        public int Port { get; set; } = 587;
        public string SenderName { get; set; } = "Alloc PM System";
        public string SenderEmail { get; set; } = string.Empty;
        public string Password { get; set; } = string.Empty;
    }

    public class AzureCommunicationSettings
    {
        public string ConnectionString { get; set; } = string.Empty;
        public string SenderEmail { get; set; } = string.Empty;
        public string SenderName { get; set; } = "Alloc PM System";
    }
}
