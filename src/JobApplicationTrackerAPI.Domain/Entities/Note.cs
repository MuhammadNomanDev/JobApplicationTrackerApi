namespace JobApplicationTracker.Domain.Entities;

public class Note : BaseEntity
{
    public string Content { get; private set; }
    
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