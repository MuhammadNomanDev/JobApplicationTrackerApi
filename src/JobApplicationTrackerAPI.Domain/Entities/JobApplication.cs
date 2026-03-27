using JobApplicationTracker.Domain.Enums;

namespace JobApplicationTracker.Domain.Entities;

public class JobApplication : BaseEntity
{
    public string CompanyName { get; private set; }
    public string PositionTitle { get; private set; }
    public string? JobUrl { get; private set; }
    public decimal? Salary { get; private set; }
    public JobStatus Status { get; private set; } = JobStatus.Draft;
    public DateTime? AppliedDate { get; private set; }
    
    // Foreign key
    public Guid UserId { get; private set; }
    
    // Navigation properties
    public User User { get; private set; } = null!;
    private readonly List<Note> _notes = new();
    public IReadOnlyCollection<Note> Notes => _notes.AsReadOnly();
    private readonly List<Document> _documents = new();
    public IReadOnlyCollection<Document> Documents => _documents.AsReadOnly();

    private JobApplication() { } // For EF Core

    public JobApplication(
        string companyName, 
        string positionTitle, 
        Guid userId)
    {
        CompanyName = companyName;
        PositionTitle = positionTitle;
        UserId = userId;
    }

    public void UpdateDetails(
        string companyName, 
        string positionTitle, 
        string? jobUrl, 
        decimal? salary)
    {
        CompanyName = companyName;
        PositionTitle = positionTitle;
        JobUrl = jobUrl;
        Salary = salary;
        SetAsUpdated();
    }

    public void UpdateStatus(JobStatus status)
    {
        Status = status;
        
        if (status == JobStatus.Applied && AppliedDate == null)
            AppliedDate = DateTime.UtcNow;
            
        SetAsUpdated();
    }
}