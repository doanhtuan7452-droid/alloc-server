using AllocServer.Models;
using AllocServer.Contexts;
using AllocServer.DTOs.Auth;

namespace AllocServer.Interfaces.Register
{
    /// <summary>
    /// Strategy Pattern — Context interface điều phối việc thực thi Strategy.
    /// AuthFacade sẽ dùng interface này để chạy đúng Strategy mà không cần biết chi tiết.
    /// </summary>
    public interface IRegisterStrategyContext
    {
        /// <summary>Thiết lập strategy sẽ được dùng</summary>
        void SetStrategy(IRegistrationStrategy strategy);

        /// <summary>Thực thi strategy đang được set</summary>
        Task<RegisterResponse> ExecuteAsync(RegisterContext context);
    }
}
