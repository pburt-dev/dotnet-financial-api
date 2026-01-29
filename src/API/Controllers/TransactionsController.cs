using Application.Transactions.Commands.Deposit;
using Application.Transactions.Commands.Transfer;
using Application.Transactions.Commands.Withdraw;
using Application.Transactions.DTOs;
using Application.Transactions.Queries.GetTransaction;
using Application.Transactions.Queries.GetTransactionByReference;
using MediatR;
using Microsoft.AspNetCore.Mvc;

namespace API.Controllers;

[ApiController]
[Route("api/[controller]")]
public class TransactionsController : ControllerBase
{
    private readonly IMediator _mediator;

    public TransactionsController(IMediator mediator)
    {
        _mediator = mediator;
    }

    /// <summary>
    /// Deposit funds into an account
    /// </summary>
    [HttpPost("deposit")]
    [ProducesResponseType(typeof(TransactionDto), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status422UnprocessableEntity)]
    public async Task<IActionResult> Deposit([FromBody] DepositRequest request, CancellationToken cancellationToken)
    {
        var result = await _mediator.Send(new DepositCommand
        {
            AccountId = request.AccountId,
            Amount = request.Amount,
            CurrencyCode = request.CurrencyCode,
            IdempotencyKey = request.IdempotencyKey,
            Description = request.Description
        }, cancellationToken);

        return CreatedAtAction(nameof(GetById), new { id = result.Id }, result);
    }

    /// <summary>
    /// Withdraw funds from an account
    /// </summary>
    [HttpPost("withdraw")]
    [ProducesResponseType(typeof(TransactionDto), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status422UnprocessableEntity)]
    public async Task<IActionResult> Withdraw([FromBody] WithdrawRequest request, CancellationToken cancellationToken)
    {
        var result = await _mediator.Send(new WithdrawCommand
        {
            AccountId = request.AccountId,
            Amount = request.Amount,
            CurrencyCode = request.CurrencyCode,
            IdempotencyKey = request.IdempotencyKey,
            Description = request.Description
        }, cancellationToken);

        return CreatedAtAction(nameof(GetById), new { id = result.Id }, result);
    }

    /// <summary>
    /// Transfer funds between accounts
    /// </summary>
    [HttpPost("transfer")]
    [ProducesResponseType(typeof(TransferResultDto), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status422UnprocessableEntity)]
    public async Task<IActionResult> Transfer([FromBody] TransferRequest request, CancellationToken cancellationToken)
    {
        var result = await _mediator.Send(new TransferCommand
        {
            SourceAccountId = request.SourceAccountId,
            DestinationAccountId = request.DestinationAccountId,
            Amount = request.Amount,
            CurrencyCode = request.CurrencyCode,
            IdempotencyKey = request.IdempotencyKey
        }, cancellationToken);

        return CreatedAtAction(nameof(GetById), new { id = result.SourceTransaction.Id }, result);
    }

    /// <summary>
    /// Get transaction by ID
    /// </summary>
    [HttpGet("{id:guid}")]
    [ProducesResponseType(typeof(TransactionDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetById(Guid id, CancellationToken cancellationToken)
    {
        var result = await _mediator.Send(new GetTransactionQuery { TransactionId = id }, cancellationToken);

        if (result == null)
            return NotFound(new { error = $"Transaction {id} not found" });

        return Ok(result);
    }

    /// <summary>
    /// Get transaction by reference number
    /// </summary>
    [HttpGet("by-reference/{reference}")]
    [ProducesResponseType(typeof(TransactionDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetByReference(string reference, CancellationToken cancellationToken)
    {
        var result = await _mediator.Send(new GetTransactionByReferenceQuery { TransactionReference = reference }, cancellationToken);

        if (result == null)
            return NotFound(new { error = $"Transaction with reference '{reference}' not found" });

        return Ok(result);
    }
}

public record DepositRequest
{
    public Guid AccountId { get; init; }
    public decimal Amount { get; init; }
    public string CurrencyCode { get; init; } = "USD";
    public string IdempotencyKey { get; init; } = null!;
    public string? Description { get; init; }
}

public record WithdrawRequest
{
    public Guid AccountId { get; init; }
    public decimal Amount { get; init; }
    public string CurrencyCode { get; init; } = "USD";
    public string IdempotencyKey { get; init; } = null!;
    public string? Description { get; init; }
}

public record TransferRequest
{
    public Guid SourceAccountId { get; init; }
    public Guid DestinationAccountId { get; init; }
    public decimal Amount { get; init; }
    public string CurrencyCode { get; init; } = "USD";
    public string IdempotencyKey { get; init; } = null!;
}
