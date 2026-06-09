namespace AllocServer.Interfaces.Auth
{
    public interface IOtpService
    {
        Task<bool> RequestOtpAsync(string email);
        Task<bool> VerifyOtpAsync(string email, string code);
    }
}
