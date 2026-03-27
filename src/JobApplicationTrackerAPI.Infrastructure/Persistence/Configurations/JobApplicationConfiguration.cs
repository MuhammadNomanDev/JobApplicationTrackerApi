using JobApplicationTracker.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace JobApplicationTracker.Infrastructure.Persistence.Configurations;

public class JobApplicationConfiguration : IEntityTypeConfiguration<JobApplication>
{
    public void Configure(EntityTypeBuilder<JobApplication> builder)
    {
        builder.ToTable("JobApplications");
        
        builder.HasKey(j => j.Id);
        
        builder.Property(j => j.CompanyName)
            .HasMaxLength(200)
            .IsRequired();
            
        builder.Property(j => j.PositionTitle)
            .HasMaxLength(200)
            .IsRequired();
            
        builder.Property(j => j.JobUrl)
            .HasMaxLength(500);
            
        builder.Property(j => j.Salary)
            .HasColumnType("decimal(18,2)");
            
        builder.Property(j => j.Status)
            .HasConversion<string>()
            .IsRequired();
            
        builder.Property(j => j.AppliedDate);
        
        builder.HasMany(j => j.Notes)
            .WithOne(n => n.JobApplication)
            .HasForeignKey(n => n.JobApplicationId)
            .OnDelete(DeleteBehavior.Cascade);
            
        builder.HasMany(j => j.Documents)
            .WithOne(d => d.JobApplication)
            .HasForeignKey(d => d.JobApplicationId)
            .OnDelete(DeleteBehavior.Cascade);
            
        builder.HasQueryFilter(j => !j.IsDeleted);
    }
}