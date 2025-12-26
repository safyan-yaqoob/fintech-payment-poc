using FintechPaymentPOC.Domain.Entities;

namespace FintechPaymentPOC.Application.Interfaces;

public interface IPaymentRepository
{
    Task<Account?> GetAccountByIdAsync(Guid accountId);
    Task<Account> CreateAccountAsync(Account account);
    Task<Transaction?> GetTransactionByIdAsync(Guid transactionId);
    Task<Transaction> CreateTransactionAsync(Transaction transaction);
    Task UpdateTransactionAsync(Transaction transaction);
    Task UpdateAccountBalanceAsync(Guid accountId, decimal newBalance);
    Task<List<Transaction>> GetTransactionsByAccountIdAsync(Guid accountId);
}

