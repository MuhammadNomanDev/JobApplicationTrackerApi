using JobApplicationTracker.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace JobApplicationTracker.Infrastructure.Persistence.Configurations;

public class DocumentConfiguration : IEntityTypeConfiguration<Document>
{
    public void Configure(EntityTypeBuilder<Document> builder)
    {
        builder.ToTable("Documents");
        
        builder.HasKey(d => d.Id);
        
        builder.Property(d => d.FileName)
            .HasMaxLength(500)
            .IsRequired();
            
        builder.Property(d => d.FileUrl)
            .HasMaxLength(2000)
            .IsRequired();
            
        builder.Property(d => d.DocumentType)
            .HasConversion<string>()
            .IsRequired();
            
        builder.HasQueryFilter(d => !d.IsDeleted);
    }
}