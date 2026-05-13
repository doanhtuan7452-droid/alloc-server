using AllocServer.Contexts;
using AllocServer.DTOs.Auth;
using AllocServer.Models;

namespace AllocServer.Interfaces.Register
{
    /// <summary>
    /// Strategy Pattern — Interface chung cho tất cả các loại đăng ký tài khoản.
    /// Mỗi loại (Local, Google, Microsoft...) sẽ implement interface này.
    /// </summary>
    public interface IRegistrationStrategy
    {
        Task<RegisterResponse> RegisterAsync(RegisterContext context);
    }
}
