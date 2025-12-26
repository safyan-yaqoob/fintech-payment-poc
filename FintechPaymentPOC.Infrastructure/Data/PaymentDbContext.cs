using FintechPaymentPOC.Application.Interfaces;
using FintechPaymentPOC.Domain.Entities;
using FintechPaymentPOC.Domain.Events;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.Extensions.Logging;

namespace FintechPaymentPOC.Infrastructure.Data;

public class PaymentDbContext : DbContext
{
    private readonly IDomainEventDispatcher? _domainEventDispatcher;

    public PaymentDbContext(DbContextOptions<PaymentDbContext> options) : base(options)
    {
    }

    public PaymentDbContext(
        DbContextOptions<PaymentDbContext> options,
        IDomainEventDispatcher domainEventDispatcher) : base(options)
    {
        _domainEventDispatcher = domainEventDispatcher;
    }

    public DbSet<Account> Accounts { get; set; }
    public DbSet<Transaction> Transactions { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.Entity<Account>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Name).IsRequired().HasMaxLength(200);
            entity.Property(e => e.Balance).HasPrecision(18, 2);
            entity.Property(e => e.Currency).IsRequired().HasMaxLength(3);
        });

        modelBuilder.Entity<Transaction>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Amount).HasPrecision(18, 2);
            entity.Property(e => e.Currency).IsRequired().HasMaxLength(3);
            // Pacs008Xml is stored as string (nvarchar(max) in SQL Server, text in in-memory)
            entity.Property(e => e.Pacs008Xml);
            entity.HasOne(e => e.SenderAccount)
                .WithMany(a => a.SentTransactions)
                .HasForeignKey(e => e.SenderAccountId)
                .OnDelete(DeleteBehavior.Restrict);
            entity.HasOne(e => e.ReceiverAccount)
                .WithMany(a => a.ReceivedTransactions)
                .HasForeignKey(e => e.ReceiverAccountId)
                .OnDelete(DeleteBehavior.Restrict);
        });
    }

    public override async Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        // TODO: Should be wrraped inside database transaction for real database.
        var domainEvents = GetDomainEvents();

        // Save changes to database (within transaction, not committed yet)
        var result = await base.SaveChangesAsync(cancellationToken);
        // Dispatch domain events BEFORE committing transaction
        // Critical handlers (like PaymentEventHandler) will run in the same transaction
        // If any handler fails, the entire transaction will be rolled back
            
        var dispatcher = _domainEventDispatcher ?? 
            this.GetService<IDomainEventDispatcher>();

        if (dispatcher != null && domainEvents.Any())
        {
            await dispatcher.DispatchAsync(domainEvents, cancellationToken);
        }
            
        // Clear domain events after successful commit
        ClearDomainEvents();

        return result;
    }

    public override int SaveChanges()
    {
        // Collect domain events from all entities before saving
        var domainEvents = GetDomainEvents();

        // Save changes to database
        var result = base.SaveChanges();

        // Dispatch domain events after successful save (synchronously)
        var dispatcher = _domainEventDispatcher ?? 
            this.GetService<IDomainEventDispatcher>();
        
        if (dispatcher != null && domainEvents.Any())
        {
            dispatcher.DispatchAsync(domainEvents).GetAwaiter().GetResult();
        }

        // Clear domain events after dispatching
        ClearDomainEvents();

        return result;
    }

    private List<IDomainEvent> GetDomainEvents()
    {
        var domainEvents = ChangeTracker.Entries<Entity>()
            .Select(x => x.Entity)
            .SelectMany(entity =>
            {
                var events = entity.DomainEvents.ToList();
                return events;
            })
            .ToList();

        return domainEvents;
    }

    private void ClearDomainEvents()
    {
        var entities = ChangeTracker.Entries<Entity>()
            .Select(x => x.Entity)
            .ToList();

        foreach (var entity in entities)
        {
            entity.ClearDomainEvents();
        }
    }
}

