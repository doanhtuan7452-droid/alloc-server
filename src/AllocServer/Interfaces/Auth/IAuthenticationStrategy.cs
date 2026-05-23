using AllocServer.Models.Auth;

namespace AllocServer.Interfaces.Auth
{
    /// <summary>
    /// Strategy Pattern — Interface chung cho tất cả hình thức xác thực.
    /// Mỗi loại (LocalLogin, LocalRegister, GoogleAuth) sẽ implement interface này.
    /// Trả về AuthStrategyResult (internal model) — Facade sẽ map sang DTO public.
    /// </summary>
    public interface IAuthenticationStrategy
    {
        Task<AuthStrategyResult> ExecuteAsync(AuthStrategyContext context);
    }
}
