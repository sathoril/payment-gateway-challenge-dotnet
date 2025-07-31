using Microsoft.AspNetCore.Mvc;

using PaymentGateway.Api.Interfaces;
using PaymentGateway.Api.Models.Requests;
using PaymentGateway.Api.Models.Responses;
using PaymentGateway.Api.Services;

namespace PaymentGateway.Api.Controllers;

[Route("api/[controller]")]
[ApiController]
public class PaymentsController : Controller
{
    private readonly PaymentsRepository _paymentsRepository;
    private readonly IPaymentService _paymentService;

    public PaymentsController(
        PaymentsRepository paymentsRepository,
        IPaymentService paymentService)
    {
        _paymentsRepository = paymentsRepository;
        _paymentService = paymentService;
    }

    [HttpGet("{id:guid}")]
    [EndpointDescription("Retrieves a payment by its id")]
    [ProducesResponseType(typeof(PostAcquiringBankResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public ActionResult<PostAcquiringBankResponse?> GetPayment(Guid id)
    {
        try
        {
            var payment = _paymentsRepository.Get(id);
            if (payment == null)
                return NotFound($"Payment with identifier {id} was not found.");

            return Ok(payment);
        }
        catch
        {
            // Add Logs
            return Problem(
                detail: "An unexpected error occurred while fetching requested payment.",
                statusCode: StatusCodes.Status500InternalServerError,
                title: "Internal server error",
                type: "https://tools.ietf.org/html/rfc7231#section-6.6.1");

        }
    }

    [HttpPost]
    [ProducesResponseType(typeof(PostAcquiringBankResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status422UnprocessableEntity)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<PostAcquiringBankResponse?>> PostPaymentAsync(PostPaymentRequest request)
    {
        try
        {
            var isValid = request.IsValid();
            if (!isValid)
                return Problem(
                    detail: "Some field(s) on the request are not in correct format",
                    statusCode: StatusCodes.Status400BadRequest,
                    title: "Request sent is invalid.",
                    type: "Error");

            var response = await _paymentService.ProcessPaymentAsync(request);
        
            return Ok(response);
        }
        catch (Exception e)
        {
            // Add Logs
            return Problem(
                detail: "An unexpected error occurred while processing the payment.",
                statusCode: StatusCodes.Status500InternalServerError,
                title: "Internal server error",
                type: "https://tools.ietf.org/html/rfc7231#section-6.6.1");
        }
    }
}