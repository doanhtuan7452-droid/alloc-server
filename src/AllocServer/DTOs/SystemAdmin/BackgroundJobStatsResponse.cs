namespace AllocServer.DTOs.SystemAdmin
{
    public class BackgroundJobStatsResponse
    {
        public int NotificationQueueLength { get; set; }
        public int NotificationCompensationQueueLength { get; set; }
        public int ProfileCalculationQueueLength { get; set; }
        public int AIQuotaCompensationQueueLength { get; set; }
    }
}
