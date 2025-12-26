using FintechPaymentPOC.Domain.Events;

namespace FintechPaymentPOC.Application.Interfaces;

/// <summary>
/// Dispatches domain events to their registered handlers
/// </summary>
public interface IDomainEventDispatcher
{
    Task DispatchAsync(IEnumerable<IDomainEvent> domainEvents, CancellationToken cancellationToken = default);
}

