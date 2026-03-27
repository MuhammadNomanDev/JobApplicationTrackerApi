using JobApplicationTracker.Infrastructure.Data;
using JobApplicationTracker.Infrastructure.Persistence.Repositories;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace JobApplicationTracker.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(
        this IServiceCollection services, 
        IConfiguration configuration)
    {
        services.AddDbContext<AppDbContext>(options =>
            options.UseSqlServer(
                configuration.GetConnectionString("DefaultConnection"),
                b => b.MigrationsAssembly(typeof(AppDbContext).Assembly.FullName)));

        services.AddScoped<IUserRepository, UserRepository>();
        services.AddScoped<IJobApplicationRepository, JobApplicationRepository>();
        services.AddScoped<IDocumentRepository, DocumentRepository>();
        services.AddScoped<INoteRepository, NoteRepository>();

        return services;
    }
}