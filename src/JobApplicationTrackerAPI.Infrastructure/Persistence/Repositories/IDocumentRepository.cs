using JobApplicationTracker.Domain.Entities;

namespace JobApplicationTracker.Infrastructure.Persistence.Repositories;

public interface IDocumentRepository
{
    Task<Document?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<IEnumerable<Document>> GetAllByJobApplicationIdAsync(Guid jobApplicationId, CancellationToken cancellationToken = default);
    Task AddAsync(Document document, CancellationToken cancellationToken = default);
    Task UpdateAsync(Document document, CancellationToken cancellationToken = default);
    Task DeleteAsync(Document document, CancellationToken cancellationToken = default);
}