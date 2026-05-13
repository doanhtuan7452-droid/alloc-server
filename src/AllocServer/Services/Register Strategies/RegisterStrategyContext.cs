using AllocServer.Contexts;
using AllocServer.Interfaces.Register;
using AllocServer.DTOs.Auth;

namespace AllocServer.Services.Register_Strategies
{
    /// <summary>
    /// Strategy Pattern — Context class thực thi Strategy được thiết lập.
    /// AuthFacade sẽ SetStrategy() đúng loại rồi gọi ExecuteAsync().
    /// </summary>
    public class RegisterStrategyContext : IRegisterStrategyContext
    {
        private IRegistrationStrategy? _strategy;

        /// <summary>Thiết lập strategy trước khi gọi ExecuteAsync</summary>
        public void SetStrategy(IRegistrationStrategy strategy)
        {
            _strategy = strategy;
        }

        /// <summary>Thực thi strategy đang được set với RegisterContext được cung cấp</summary>
        public async Task<RegisterResponse> ExecuteAsync(RegisterContext context)
        {
            if (_strategy == null)
                throw new InvalidOperationException(
                    "Strategy chưa được thiết lập. Hãy gọi SetStrategy() trước khi ExecuteAsync().");

            return await _strategy.RegisterAsync(context);
        }
    }
}
