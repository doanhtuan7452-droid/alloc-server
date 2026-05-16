namespace AllocServer.Exceptions
{
    public class QuotaExceededException : Exception
    {
        public string ErrorCode { get; }

        public QuotaExceededException(string message, string errorCode = "QUOTA_EXCEEDED") : base(message)
        {
            ErrorCode = errorCode;
        }
    }
}
