using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace AllocServer.Events
{
    public class EventPublisher : IEventPublisher
    {
        private readonly IServiceProvider _serviceProvider;
        private readonly ILogger<EventPublisher> _logger;

        public EventPublisher(IServiceProvider serviceProvider, ILogger<EventPublisher> logger)
        {
            _serviceProvider = serviceProvider;
            _logger = logger;
        }

        public async Task PublishAsync<TEvent>(TEvent domainEvent, CancellationToken cancellationToken = default)
            where TEvent : IDomainEvent
        {
            var handlers = _serviceProvider.GetServices<IEventHandler<TEvent>>();

            foreach (var handler in handlers)
            {
                try
                {
                    await handler.HandleAsync(domainEvent, cancellationToken);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error handling event {EventType} with handler {HandlerType}", typeof(TEvent).Name, handler.GetType().Name);
                }
            }
        }
    }
}
