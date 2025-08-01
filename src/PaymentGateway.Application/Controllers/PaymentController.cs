using AutoMapper;

using Microsoft.AspNetCore.Mvc;

using PaymentGateway.Application.DTOs.Requests;
using PaymentGateway.Application.DTOs.Responses;
using PaymentGateway.Domain;
using PaymentGateway.Domain.Entities;
using PaymentGateway.Domain.Interfaces.Repositories;
using PaymentGateway.Domain.Interfaces.Services;

namespace PaymentGateway.Application.Controllers;

[Route("api/[controller]")]
[ApiController]
public class PaymentController : Controller
{
    private readonly IPaymentRepository _paymentsRepository;
    private readonly IPaymentUseCase _paymentUseCase;
    private readonly IMapper _mapper;

    public PaymentController(
        IPaymentRepository paymentsRepository,
        IPaymentUseCase paymentUseCase,
        IMapper mapper)
    {
        _paymentsRepository = paymentsRepository;
        _paymentUseCase = paymentUseCase;
        _mapper = mapper;
    }

    [HttpGet("{id:guid}")]
    [EndpointDescription("Retrieves a payment by its id")]
    [ProducesResponseType(typeof(GetPaymentByIdResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<GetPaymentByIdResponse?>> GetPayment(Guid id)
    {
        try
        {
            var payment = await _paymentsRepository.GetByIdAsync(id);
            if (payment == null)
                return NotFound($"Payment with identifier {id} was not found.");

            var mappedResponse = _mapper.Map<Payment, GetPaymentByIdResponse>(payment);
            
            return Ok(mappedResponse);
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
    [ProducesResponseType(typeof(ProcessPaymentResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status422UnprocessableEntity)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<ProcessPaymentResponse?>> PostPaymentAsync(ProcessPaymentRequest request)
    {
        try
        {
            var response = await _paymentUseCase.ProcessPaymentAsync(
                request.CardNumber, request.ExpiryMonth, request.ExpiryYear, request.Currency, request.Amount,
                request.Cvv);

            var mappedResponse = _mapper.Map<Payment, ProcessPaymentResponse>(response);

            return Ok(mappedResponse);
        }
        catch (ArgumentException e)
        {
            // Add Logs
            return Problem(
                detail: "Invalid request",
                statusCode: StatusCodes.Status400BadRequest,
                title: e.Message);
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