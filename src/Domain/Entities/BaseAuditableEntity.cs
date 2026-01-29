namespace Domain.Entities;

/// <summary>
/// Base class for entities that require audit tracking.
/// Provides created/modified timestamps and user tracking.
/// </summary>
public abstract class BaseAuditableEntity
{
    public DateTime CreatedAt { get; set; }
    public string? CreatedBy { get; set; }
    public DateTime? LastModifiedAt { get; set; }
    public string? LastModifiedBy { get; set; }
}
