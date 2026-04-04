using JobApplicationTracker.Application.Interfaces;
using JobApplicationTracker.Domain.Entities;
using JobApplicationTracker.Infrastructure.Persistence.Configurations;
using Microsoft.EntityFrameworkCore;

namespace JobApplicationTracker.Infrastructure.Data;

public class AppDbContext : DbContext, IAppDbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options)
    {
    }

    public DbSet<User> Users => Set<User>();
    public DbSet<JobApplication> JobApplications => Set<JobApplication>();
    public DbSet<Document> Documents => Set<Document>();
    public DbSet<Note> Notes => Set<Note>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyConfiguration(new UserConfiguration());
        modelBuilder.ApplyConfiguration(new JobApplicationConfiguration());
        modelBuilder.ApplyConfiguration(new DocumentConfiguration());
        modelBuilder.ApplyConfiguration(new NoteConfiguration());

        base.OnModelCreating(modelBuilder);
    }
}