using JobApplicationTracker.Domain.Entities;
using JobApplicationTracker.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace JobApplicationTracker.Infrastructure.Persistence.Repositories;

public class JobApplicationRepository : IJobApplicationRepository
{
    private readonly AppDbContext _context;

    public JobApplicationRepository(AppDbContext context)
    {
        _context = context;
    }

    public async Task<JobApplication?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return await _context.JobApplications
            .Include(j => j.Notes)
            .Include(j => j.Documents)
            .FirstOrDefaultAsync(j => j.Id == id, cancellationToken);
    }

    public async Task<IEnumerable<JobApplication>> GetAllByUserIdAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        return await _context.JobApplications
            .Where(j => j.UserId == userId)
            .Include(j => j.Notes)
            .Include(j => j.Documents)
            .ToListAsync(cancellationToken);
    }

    public async Task AddAsync(JobApplication jobApplication, CancellationToken cancellationToken = default)
    {
        await _context.JobApplications.AddAsync(jobApplication, cancellationToken);
    }

    public async Task UpdateAsync(JobApplication jobApplication, CancellationToken cancellationToken = default)
    {
        _context.JobApplications.Update(jobApplication);
        await Task.CompletedTask;
    }

    public async Task DeleteAsync(JobApplication jobApplication, CancellationToken cancellationToken = default)
    {
        _context.JobApplications.Remove(jobApplication);
        await Task.CompletedTask;
    }
}