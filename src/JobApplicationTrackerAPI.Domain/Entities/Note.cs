namespace JobApplicationTrackerAPI.Domain.Entities;

public class Note : BaseEntity
{
    public string Content { get; private set; } = null!;

    // Foreign key
    public Guid JobApplicationId { get; private set; }

    // Navigation properties
    public JobApplication JobApplication { get; private set; } = null!;

    private Note() { } // For EF Core

    public Note(string content, Guid jobApplicationId)
    {
        Content = content;
        JobApplicationId = jobApplicationId;
    }

    public void UpdateContent(string content)
    {
        Content = content;
        SetAsUpdated();
    }
}
