using FintechPaymentPOC.Application.Interfaces;
using FintechPaymentPOC.Domain.Events;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace FintechPaymentPOC.Infrastructure.Events;

/// <summary>
/// Automatically discovers and registers event handlers when events are published
/// </summary>
public class EventHandlerRegistry
{
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<EventHandlerRegistry> _logger;
    private readonly Dictionary<Type, List<Type>> _handlerTypes = new();

    public EventHandlerRegistry(IServiceProvider serviceProvider, ILogger<EventHandlerRegistry> logger)
    {
        _serviceProvider = serviceProvider;
        _logger = logger;
        DiscoverHandlers();
    }

    private void DiscoverHandlers()
    {
        // Discover PaymentRequestedEvent handlers
        _handlerTypes[typeof(PaymentRequestedEvent)] = new List<Type>
        {
            typeof(PaymentEventHandler),
            typeof(NotificationEventHandler),
            typeof(AuditEventHandler)
        };

        _logger.LogInformation("✅ Event handlers discovered and registered");
    }

    public async Task InvokeHandlersAsync<T>(T eventData) where T : class
    {
        var eventType = typeof(T);
        
        if (!_handlerTypes.TryGetValue(eventType, out var handlerTypes))
        {
            _logger.LogWarning("⚠️ No handlers found for event type: {EventType}", eventType.Name);
            return;
        }

        _logger.LogInformation(
            "📡 Auto-invoking {HandlerCount} handler(s) for event '{EventType}'",
            handlerTypes.Count,
            eventType.Name);

        var tasks = handlerTypes.Select(async handlerType =>
        {
            try
            {
                using var scope = _serviceProvider.CreateScope();
                var handler = scope.ServiceProvider.GetService(handlerType);
                
                if (handler == null)
                {
                    _logger.LogWarning("Handler {HandlerType} not found in service provider", handlerType.Name);
                    return;
                }

                // Find the Handle method using reflection
                var method = handlerType.GetMethod("HandlePaymentRequestedAsync");
                if (method != null)
                {
                    _logger.LogInformation("🔄 Invoking handler: {HandlerType}", handlerType.Name);
                    var task = (Task?)method.Invoke(handler, new object[] { eventData });
                    if (task != null)
                    {
                        await task;
                        _logger.LogInformation("✅ Handler {HandlerType} completed successfully", handlerType.Name);
                    }
                }
                else
                {
                    _logger.LogWarning("Method HandlePaymentRequestedAsync not found in {HandlerType}", handlerType.Name);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "❌ Error invoking handler {HandlerType}", handlerType.Name);
            }
        });

        await Task.WhenAll(tasks);
    }
}

