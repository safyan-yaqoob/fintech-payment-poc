using FintechPaymentPOC.Domain.Entities;

namespace FintechPaymentPOC.Infrastructure.Data;

public static class DatabaseSeeder
{
    public static void Seed(PaymentDbContext context)
    {
        if (context.Accounts.Any())
        {
            return; // Database already seeded
        }

        var accounts = new List<Account>
        {
            new Account
            {
                Id = Guid.Parse("11111111-1111-1111-1111-111111111111"),
                Name = "Alice Johnson",
                Balance = 10000.00m,
                Currency = "USD",
                CreatedAt = DateTime.UtcNow.AddDays(-30)
            },
            new Account
            {
                Id = Guid.Parse("22222222-2222-2222-2222-222222222222"),
                Name = "Bob Smith",
                Balance = 5000.00m,
                Currency = "USD",
                CreatedAt = DateTime.UtcNow.AddDays(-25)
            },
            new Account
            {
                Id = Guid.Parse("33333333-3333-3333-3333-333333333333"),
                Name = "Charlie Brown",
                Balance = 7500.00m,
                Currency = "USD",
                CreatedAt = DateTime.UtcNow.AddDays(-20)
            }
        };

        context.Accounts.AddRange(accounts);
        context.SaveChanges();
    }
}

