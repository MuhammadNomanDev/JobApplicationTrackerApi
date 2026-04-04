using JobApplicationTracker.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace JobApplicationTracker.Application.Interfaces;

public interface IAppDbContext
{
    DbSet<User> Users { get; }
    DbSet<JobApplication> JobApplications { get; }
    DbSet<Document> Documents { get; }
    DbSet<Note> Notes { get; }
    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
}
