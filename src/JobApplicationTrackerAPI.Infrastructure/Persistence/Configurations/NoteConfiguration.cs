using JobApplicationTrackerAPI.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace JobApplicationTrackerAPI.Infrastructure.Persistence.Configurations;

public class NoteConfiguration : IEntityTypeConfiguration<Note>
{
    public void Configure(EntityTypeBuilder<Note> builder)
    {
        builder.ToTable("Notes");
        
        builder.HasKey(n => n.Id);
        
        builder.Property(n => n.Content)
            .HasMaxLength(2000)
            .IsRequired();
            
        builder.HasQueryFilter(n => !n.IsDeleted);
    }
}