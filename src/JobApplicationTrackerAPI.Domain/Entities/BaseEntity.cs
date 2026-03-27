namespace JobApplicationTracker.Domain.Entities;

public abstract class BaseEntity
{
    public Guid Id { get; private set; } = Guid.NewGuid();
    public DateTime CreatedAt { get; private set; } = DateTime.UtcNow;
    public DateTime? UpdatedAt { get; private set; }
    public bool IsDeleted { get; private set; }

    protected void SetAsUpdated() => UpdatedAt = DateTime.UtcNow;
    protected void Delete() => IsDeleted = true;
    protected void Restore() => IsDeleted = false;
}