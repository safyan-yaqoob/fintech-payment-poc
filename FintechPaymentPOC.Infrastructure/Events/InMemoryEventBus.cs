using FintechPaymentPOC.Application.Interfaces;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace FintechPaymentPOC.Infrastructure.Events;

public class InMemoryEventBus : IEventBus
{
    private readonly Dictionary<Type, List<Func<object, Task>>> _handlers = new();
    private readonly ILogger<InMemoryEventBus> _logger;
    private readonly EventHandlerRegistry? _handlerRegistry;

    public InMemoryEventBus(ILogger<InMemoryEventBus> logger, IServiceProvider? serviceProvider = null)
    {
        _logger = logger;
        if (serviceProvider != null)
        {
            _handlerRegistry = new EventHandlerRegistry(serviceProvider, 
                serviceProvider.GetRequiredService<ILogger<EventHandlerRegistry>>());
        }
    }

    public async Task PublishAsync<T>(T eventData) where T : class
    {
        var eventType = typeof(T);
        _logger.LogInformation(
            "🚀 EVENT BUS: Publishing event '{EventType}' at {Timestamp}",
            eventType.Name,
            DateTime.UtcNow);

        // Auto-invoke handlers from registry if available
        if (_handlerRegistry != null)
        {
            await _handlerRegistry.InvokeHandlersAsync(eventData);
        }

        // Also invoke manually subscribed handlers
        if (_handlers.TryGetValue(eventType, out var handlers))
        {
            _logger.LogInformation(
                "📡 EVENT BUS: Found {HandlerCount} manually subscribed handler(s) for event '{EventType}'",
                handlers.Count,
                eventType.Name);

            var tasks = handlers.Select(async handler =>
            {
                try
                {
                    await handler(eventData);
                }
                catch (Exception ex)
                {
                    _logger.LogError(
                        ex,
                        "❌ EVENT BUS: Error in event handler for '{EventType}'",
                        eventType.Name);
                }
            });

            await Task.WhenAll(tasks);

            _logger.LogInformation(
                "✅ EVENT BUS: All handlers completed for event '{EventType}'",
                eventType.Name);
        }
    }

    public void Subscribe<T>(Func<T, Task> handler) where T : class
    {
        var eventType = typeof(T);
        if (!_handlers.ContainsKey(eventType))
        {
            _handlers[eventType] = new List<Func<object, Task>>();
        }

        _handlers[eventType].Add(async obj =>
        {
            if (obj is T typedEvent)
            {
                await handler(typedEvent);
            }
        });

        _logger.LogInformation(
            "✅ EVENT BUS: Subscribed handler to event '{EventType}'. Total subscribers: {SubscriberCount}",
            eventType.Name,
            _handlers[eventType].Count);
    }
}

