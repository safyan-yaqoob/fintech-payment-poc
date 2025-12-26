using FintechPaymentPOC.Application.Interfaces;
using FintechPaymentPOC.Domain.Entities;
using FintechPaymentPOC.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace FintechPaymentPOC.Infrastructure.Repositories;

public class PaymentRepository : IPaymentRepository
{
    private readonly PaymentDbContext _context;

    public PaymentRepository(PaymentDbContext context)
    {
        _context = context;
    }

    public async Task<Account?> GetAccountByIdAsync(Guid accountId)
    {
        return await _context.Accounts.FindAsync(accountId);
    }

    public async Task<Account> CreateAccountAsync(Account account)
    {
        _context.Accounts.Add(account);
        await _context.SaveChangesAsync();
        return account;
    }

    public async Task<Transaction?> GetTransactionByIdAsync(Guid transactionId)
    {
        return await _context.Transactions
            .Include(t => t.SenderAccount)
            .Include(t => t.ReceiverAccount)
            .FirstOrDefaultAsync(t => t.Id == transactionId);
    }

    public async Task<Transaction> CreateTransactionAsync(Transaction transaction)
    {
        _context.Transactions.Add(transaction);
        await _context.SaveChangesAsync();
        return transaction;
    }

    public async Task UpdateTransactionAsync(Transaction transaction)
    {
        _context.Transactions.Update(transaction);
        await _context.SaveChangesAsync();
    }

    public async Task UpdateAccountBalanceAsync(Guid accountId, decimal newBalance)
    {
        var account = await _context.Accounts.FindAsync(accountId);
        if (account != null)
        {
            account.Balance = newBalance;
            _context.Accounts.Update(account);
            await _context.SaveChangesAsync();
        }
    }

    public async Task<List<Transaction>> GetTransactionsByAccountIdAsync(Guid accountId)
    {
        return await _context.Transactions
            .Include(t => t.SenderAccount)
            .Include(t => t.ReceiverAccount)
            .Where(t => t.SenderAccountId == accountId || t.ReceiverAccountId == accountId)
            .OrderByDescending(t => t.Timestamp)
            .ToListAsync();
    }
}

