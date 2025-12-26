using FintechPaymentPOC.Application.Interfaces;
using FintechPaymentPOC.Domain.Events;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace FintechPaymentPOC.Infrastructure.Events;

/// <summary>
/// Dispatches domain events to their registered handlers
/// </summary>
public class DomainEventDispatcher : IDomainEventDispatcher
{
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<DomainEventDispatcher> _logger;

    public DomainEventDispatcher(
        IServiceProvider serviceProvider,
        ILogger<DomainEventDispatcher> logger)
    {
        _serviceProvider = serviceProvider;
        _logger = logger;
    }

    public async Task DispatchAsync(IEnumerable<IDomainEvent> domainEvents, CancellationToken cancellationToken = default)
    {
        var events = domainEvents.ToList();
        
        if (!events.Any())
        {
            return;
        }

        _logger.LogInformation("📤 Dispatching {EventCount} domain event(s)", events.Count);

        foreach (var domainEvent in events)
        {
            await DispatchEventAsync(domainEvent, cancellationToken);
        }

        _logger.LogInformation("✅ All domain events dispatched successfully");
    }

    private async Task DispatchEventAsync(IDomainEvent domainEvent, CancellationToken cancellationToken)
    {
        var eventType = domainEvent.GetType();
        _logger.LogInformation("🔄 Dispatching domain event: {EventType}", eventType.Name);

        // Get all handlers for this event type
        var handlerType = typeof(IDomainEventHandler<>).MakeGenericType(eventType);
        var handlers = _serviceProvider.GetServices(handlerType);

        if (!handlers.Any())
        {
            _logger.LogWarning("⚠️ No handlers found for domain event: {EventType}", eventType.Name);
            return;
        }

        _logger.LogInformation("📡 Found {HandlerCount} handler(s) for {EventType}", handlers.Count(), eventType.Name);

        // Invoke each handler
        var tasks = handlers.Select(async handler =>
        {
            try
            {
                var handleMethod = handlerType.GetMethod("HandleAsync");
                if (handleMethod != null)
                {
                    var task = (Task?)handleMethod.Invoke(handler, new object[] { domainEvent, cancellationToken });
                    if (task != null)
                    {
                        await task;
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "❌ Error handling domain event {EventType}", eventType.Name);
                throw;
            }
        });

        await Task.WhenAll(tasks);
        _logger.LogInformation("✅ Handlers completed for {EventType}", eventType.Name);
    }
}

