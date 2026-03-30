using JobApplicationTracker.Domain.Entities;

namespace JobApplicationTracker.Application.Interfaces.Repositories;

public interface INoteRepository
{
    Task<Note?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<IEnumerable<Note>> GetAllByJobApplicationIdAsync(Guid jobApplicationId, CancellationToken cancellationToken = default);
    Task AddAsync(Note note, CancellationToken cancellationToken = default);
    Task UpdateAsync(Note note, CancellationToken cancellationToken = default);
    Task DeleteAsync(Note note, CancellationToken cancellationToken = default);
}