using FintechPaymentPOC.Application.DTOs;
using FintechPaymentPOC.Application.Interfaces;
using FintechPaymentPOC.Application.Mappings;
using FintechPaymentPOC.Domain.Entities;
using Microsoft.AspNetCore.Mvc;

namespace FintechPaymentPOC.API.Controllers;

[ApiController]
[Route("api/[controller]")]
public class AccountsController : ControllerBase
{
    private readonly IPaymentRepository _repository;
    private readonly ILogger<AccountsController> _logger;

    public AccountsController(IPaymentRepository repository, ILogger<AccountsController> logger)
    {
        _repository = repository;
        _logger = logger;
    }

    [HttpPost]
    public async Task<ActionResult<AccountDto>> CreateAccount([FromBody] CreateAccountRequestDto request)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(request.Name))
            {
                return BadRequest("Account name is required");
            }

            if (request.InitialBalance < 0)
            {
                return BadRequest("Initial balance cannot be negative");
            }

            var account = new Account
            {
                Id = Guid.NewGuid(),
                Name = request.Name,
                Balance = request.InitialBalance,
                Currency = request.Currency,
                CreatedAt = DateTime.UtcNow
            };

            var createdAccount = await _repository.CreateAccountAsync(account);

            var accountDto = new AccountDto
            {
                Id = createdAccount.Id,
                Name = createdAccount.Name,
                Balance = createdAccount.Balance,
                Currency = createdAccount.Currency,
                CreatedAt = createdAccount.CreatedAt
            };

            _logger.LogInformation("Created account {AccountId} with name {AccountName}", account.Id, account.Name);

            return CreatedAtAction(nameof(GetAccount), new { id = account.Id }, accountDto);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating account");
            return StatusCode(500, "An error occurred while creating the account");
        }
    }

    [HttpGet("{id}")]
    public async Task<ActionResult<AccountDto>> GetAccount(Guid id)
    {
        try
        {
            var account = await _repository.GetAccountByIdAsync(id);
            
            if (account == null)
            {
                return NotFound($"Account with id {id} not found");
            }

            var accountDto = new AccountDto
            {
                Id = account.Id,
                Name = account.Name,
                Balance = account.Balance,
                Currency = account.Currency,
                CreatedAt = account.CreatedAt
            };

            return Ok(accountDto);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving account {AccountId}", id);
            return StatusCode(500, "An error occurred while retrieving the account");
        }
    }

    [HttpGet("{id}/transactions")]
    public async Task<ActionResult<List<TransactionDto>>> GetTransactions(Guid id)
    {
        try
        {
            var transactions = await _repository.GetTransactionsByAccountIdAsync(id);
            var transactionDtos = transactions.Select(TransactionMapper.ToDto).ToList();
            
            return Ok(transactionDtos);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving transactions for account {AccountId}", id);
            return StatusCode(500, "An error occurred while retrieving transactions");
        }
    }
}

