namespace AllocServer.Configurations
{
    public class TestTokenSettings
    {
        public bool Enabled { get; set; }
        public string HeaderName { get; set; } = "X-Alloc-Test-Token";
        public string SecretToken { get; set; } = string.Empty;
    }
}
