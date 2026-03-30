using JobApplicationTracker.Application.Interfaces.Repositories;
using JobApplicationTracker.Domain.Entities;
using JobApplicationTracker.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace JobApplicationTracker.Infrastructure.Persistence.Repositories;

public class DocumentRepository : IDocumentRepository
{
    private readonly AppDbContext _context;

    public DocumentRepository(AppDbContext context)
    {
        _context = context;
    }

    public async Task<Document?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return await _context.Documents
            .FirstOrDefaultAsync(d => d.Id == id, cancellationToken);
    }

    public async Task<IEnumerable<Document>> GetAllByJobApplicationIdAsync(Guid jobApplicationId, CancellationToken cancellationToken = default)
    {
        return await _context.Documents
            .Where(d => d.JobApplicationId == jobApplicationId)
            .ToListAsync(cancellationToken);
    }

    public async Task AddAsync(Document document, CancellationToken cancellationToken = default)
    {
        await _context.Documents.AddAsync(document, cancellationToken);
    }

    public async Task UpdateAsync(Document document, CancellationToken cancellationToken = default)
    {
        _context.Documents.Update(document);
        await Task.CompletedTask;
    }

    public async Task DeleteAsync(Document document, CancellationToken cancellationToken = default)
    {
        _context.Documents.Remove(document);
        await Task.CompletedTask;
    }
}