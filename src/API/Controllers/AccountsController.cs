using Application.Accounts.Commands.CloseAccount;
using Application.Accounts.Commands.CreateAccount;
using Application.Accounts.Commands.FreezeAccount;
using Application.Accounts.Commands.UnfreezeAccount;
using Application.Accounts.DTOs;
using Application.Accounts.Queries.GetAccount;
using Application.Accounts.Queries.GetAccountBalance;
using Application.Transactions.Queries.GetAccountTransactions;
using MediatR;
using Microsoft.AspNetCore.Mvc;

namespace API.Controllers;

[ApiController]
[Route("api/[controller]")]
public class AccountsController : ControllerBase
{
    private readonly IMediator _mediator;

    public AccountsController(IMediator mediator)
    {
        _mediator = mediator;
    }

    /// <summary>
    /// Create a new account
    /// </summary>
    [HttpPost]
    [ProducesResponseType(typeof(AccountDto), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> Create([FromBody] CreateAccountCommand command, CancellationToken cancellationToken)
    {
        var result = await _mediator.Send(command, cancellationToken);
        return CreatedAtAction(nameof(GetById), new { id = result.Id }, result);
    }

    /// <summary>
    /// Get account details by ID
    /// </summary>
    [HttpGet("{id:guid}")]
    [ProducesResponseType(typeof(AccountDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetById(Guid id, CancellationToken cancellationToken)
    {
        var result = await _mediator.Send(new GetAccountQuery { AccountId = id }, cancellationToken);

        if (result == null)
            return NotFound(new { error = $"Account {id} not found" });

        return Ok(result);
    }

    /// <summary>
    /// Get account balance
    /// </summary>
    [HttpGet("{id:guid}/balance")]
    [ProducesResponseType(typeof(AccountBalanceDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetBalance(Guid id, CancellationToken cancellationToken)
    {
        var result = await _mediator.Send(new GetAccountBalanceQuery { AccountId = id }, cancellationToken);

        if (result == null)
            return NotFound(new { error = $"Account {id} not found" });

        return Ok(result);
    }

    /// <summary>
    /// Get account transaction history (paginated)
    /// </summary>
    [HttpGet("{id:guid}/transactions")]
    [ProducesResponseType(typeof(Application.Common.Models.PaginatedList<Application.Transactions.DTOs.TransactionDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetTransactions(
        Guid id,
        [FromQuery] int pageNumber = 1,
        [FromQuery] int pageSize = 10,
        CancellationToken cancellationToken = default)
    {
        var result = await _mediator.Send(new GetAccountTransactionsQuery
        {
            AccountId = id,
            PageNumber = pageNumber,
            PageSize = pageSize
        }, cancellationToken);

        return Ok(result);
    }

    /// <summary>
    /// Freeze an account
    /// </summary>
    [HttpPost("{id:guid}/freeze")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status422UnprocessableEntity)]
    public async Task<IActionResult> Freeze(Guid id, [FromBody] FreezeAccountRequest request, CancellationToken cancellationToken)
    {
        await _mediator.Send(new FreezeAccountCommand
        {
            AccountId = id,
            Reason = request.Reason
        }, cancellationToken);

        return NoContent();
    }

    /// <summary>
    /// Unfreeze an account
    /// </summary>
    [HttpPost("{id:guid}/unfreeze")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status422UnprocessableEntity)]
    public async Task<IActionResult> Unfreeze(Guid id, CancellationToken cancellationToken)
    {
        await _mediator.Send(new UnfreezeAccountCommand { AccountId = id }, cancellationToken);
        return NoContent();
    }

    /// <summary>
    /// Close an account
    /// </summary>
    [HttpPost("{id:guid}/close")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status422UnprocessableEntity)]
    public async Task<IActionResult> Close(Guid id, CancellationToken cancellationToken)
    {
        await _mediator.Send(new CloseAccountCommand { AccountId = id }, cancellationToken);
        return NoContent();
    }
}

public record FreezeAccountRequest
{
    public string Reason { get; init; } = null!;
}
