using JobApplicationTracker.Domain.Interfaces;
using BC = BCrypt.Net.BCrypt;

namespace JobApplicationTracker.Infrastructure.Services;

public class PasswordHasher : IPasswordHasher
{
    public string HashPassword(string password)
    {
        return BC.HashPassword(password, workFactor: 12);
    }

    public bool VerifyPassword(string hashedPassword, string providedPassword)
    {
        return BC.Verify(providedPassword, hashedPassword);
    }
}