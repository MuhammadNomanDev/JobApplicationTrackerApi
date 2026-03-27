using JobApplicationTracker.Domain.Enums;

namespace JobApplicationTracker.Domain.Entities;

public class Document : BaseEntity
{
    public string FileName { get; private set; }
    public string FileUrl { get; private set; }
    public DocumentType DocumentType { get; private set; }
    
    // Foreign key
    public Guid JobApplicationId { get; private set; }
    
    // Navigation properties
    public JobApplication JobApplication { get; private set; } = null!;

    private Document() { } // For EF Core

    public Document(
        string fileName, 
        string fileUrl, 
        DocumentType documentType, 
        Guid jobApplicationId)
    {
        FileName = fileName;
        FileUrl = fileUrl;
        DocumentType = documentType;
        JobApplicationId = jobApplicationId;
    }

    public void UpdateDocument(string fileName, string fileUrl)
    {
        FileName = fileName;
        FileUrl = fileUrl;
        SetAsUpdated();
    }
}