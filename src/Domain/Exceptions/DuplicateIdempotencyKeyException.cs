namespace Domain.Exceptions;

public class DuplicateIdempotencyKeyException : DomainException
{
    public string IdempotencyKey { get; }

    public DuplicateIdempotencyKeyException(string idempotencyKey)
        : base($"A transaction with idempotency key '{idempotencyKey}' has already been processed.")
    {
        IdempotencyKey = idempotencyKey;
    }
}
