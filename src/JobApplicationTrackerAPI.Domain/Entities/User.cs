using JobApplicationTracker.Domain.ValueObjects;

namespace JobApplicationTracker.Domain.Entities;

public class User : BaseEntity
{
    public string FirstName { get; private set; }
    public string LastName { get; private set; }
    public Email Email { get; private set; }
    public string PasswordHash { get; private set; }
    public string RefreshToken { get; private set; }
    public DateTime RefreshTokenExpiryTime { get; private set; }
    
    // Navigation properties
    private readonly List<JobApplication> _jobApplications = new();
    public IReadOnlyCollection<JobApplication> JobApplications => _jobApplications.AsReadOnly();

    private User() { } // For EF Core

    public User(string firstName, string lastName, Email email, string passwordHash)
    {
        FirstName = firstName;
        LastName = lastName;
        Email = email;
        PasswordHash = passwordHash;
    }

    public void UpdateName(string firstName, string lastName)
    {
        FirstName = firstName;
        LastName = lastName;
        SetAsUpdated();
    }

    public void UpdatePassword(string passwordHash)
    {
        PasswordHash = passwordHash;
        SetAsUpdated();
    }

    public void SetRefreshToken(string token, DateTime expiryTime)
    {
        RefreshToken = token;
        RefreshTokenExpiryTime = expiryTime;
        SetAsUpdated();
    }
}