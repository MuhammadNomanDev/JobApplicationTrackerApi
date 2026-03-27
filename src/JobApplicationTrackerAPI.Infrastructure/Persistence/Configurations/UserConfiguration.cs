using JobApplicationTracker.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace JobApplicationTracker.Infrastructure.Persistence.Configurations;

public class UserConfiguration : IEntityTypeConfiguration<User>
{
    public void Configure(EntityTypeBuilder<User> builder)
    {
        builder.ToTable("Users");
        
        builder.HasKey(u => u.Id);
        
        builder.Property(u => u.FirstName)
            .HasMaxLength(100)
            .IsRequired();
            
        builder.Property(u => u.LastName)
            .HasMaxLength(100)
            .IsRequired();
            
        builder.OwnsOne(u => u.Email, e => 
        {
            e.Property(x => x.Value)
                .HasColumnName("Email")
                .HasMaxLength(255)
                .IsRequired();
        });
        
        builder.Property(u => u.PasswordHash)
            .IsRequired();
            
        builder.Property(u => u.RefreshToken)
            .HasMaxLength(500);
            
        builder.Property(u => u.RefreshTokenExpiryTime);
        
        builder.HasMany(u => u.JobApplications)
            .WithOne(j => j.User)
            .HasForeignKey(j => j.UserId)
            .OnDelete(DeleteBehavior.Cascade);
            
        builder.HasQueryFilter(u => !u.IsDeleted);
    }
}