using FintechPaymentPOC.Application.DTOs;
using FintechPaymentPOC.Application.Interfaces;
using Microsoft.AspNetCore.Mvc;

namespace FintechPaymentPOC.API.Controllers;

[ApiController]
[Route("api/[controller]")]
public class PaymentsController : ControllerBase
{
    private readonly IPaymentService _paymentService;
    private readonly ILogger<PaymentsController> _logger;

    public PaymentsController(IPaymentService paymentService, ILogger<PaymentsController> logger)
    {
        _paymentService = paymentService;
        _logger = logger;
    }

    [HttpPost]
    public async Task<ActionResult<PaymentResponseDto>> CreatePayment([FromBody] PaymentRequestDto request)
    {
        try
        {
            _logger.LogInformation("Received payment request: {Amount} {Currency}", request.Amount, request.Currency);
            
            var result = await _paymentService.CreatePaymentAsync(request);
            
            if (result.Status == "Failed")
            {
                return BadRequest(result);
            }

            return Ok(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating payment");
            return StatusCode(500, new PaymentResponseDto
            {
                Status = "Failed",
                Message = "An error occurred while processing the payment"
            });
        }
    }
}

