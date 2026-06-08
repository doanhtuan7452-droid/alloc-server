namespace AllocServer.Configurations
{
    public class RateLimitSettings
    {
        public RateLimitPolicySettings Global { get; set; } = new();
        public RateLimitPolicySettings Auth { get; set; } = new();
    }

    public class RateLimitPolicySettings
    {
        public int TokenLimit { get; set; }
        public int TokensPerPeriod { get; set; }
        public int ReplenishmentPeriodSeconds { get; set; }
        public int QueueLimit { get; set; }
    }
}
