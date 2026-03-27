using JobApplicationTracker.Domain.Entities;

namespace JobApplicationTracker.Infrastructure.Persistence.Repositories;

public interface IJobApplicationRepository
{
    Task<JobApplication?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<IEnumerable<JobApplication>> GetAllByUserIdAsync(Guid userId, CancellationToken cancellationToken = default);
    Task AddAsync(JobApplication jobApplication, CancellationToken cancellationToken = default);
    Task UpdateAsync(JobApplication jobApplication, CancellationToken cancellationToken = default);
    Task DeleteAsync(JobApplication jobApplication, CancellationToken cancellationToken = default);
}