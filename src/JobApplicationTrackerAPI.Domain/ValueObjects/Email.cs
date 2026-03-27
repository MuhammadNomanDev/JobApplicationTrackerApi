namespace JobApplicationTracker.Domain.ValueObjects;

public record Email
{
    public string Value { get; }
    
    private Email(string value) => Value = value;

    public static Email Create(string email)
    {
        if (string.IsNullOrWhiteSpace(email))
            throw new ArgumentException("Email cannot be empty", nameof(email));

        if (!email.Contains('@'))
            throw new ArgumentException("Invalid email format", nameof(email));

        return new Email(email);
    }

    public static implicit operator string(Email email) => email.Value;
}