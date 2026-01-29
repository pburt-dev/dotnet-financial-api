namespace Application.Transactions.DTOs;

public record TransferResultDto
{
    public TransactionDto SourceTransaction { get; init; } = null!;
    public TransactionDto DestinationTransaction { get; init; } = null!;
}
