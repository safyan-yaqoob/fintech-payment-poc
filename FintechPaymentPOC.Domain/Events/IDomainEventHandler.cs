namespace FintechPaymentPOC.Domain.Events;

/// <summary>
/// Interface for domain event handlers
/// </summary>
/// <typeparam name="T">The type of domain event to handle</typeparam>
public interface IDomainEventHandler<in T> where T : IDomainEvent
{ 
    Task HandleAsync(T domainEvent, CancellationToken cancellationToken = default);
}

