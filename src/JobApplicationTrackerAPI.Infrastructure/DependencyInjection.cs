using JobApplicationTracker.Application.Features.Auth.Commands;
using JobApplicationTracker.Application.Interfaces;
using JobApplicationTracker.Application.Interfaces.Repositories;
using JobApplicationTracker.Application.Interfaces.Services;
using JobApplicationTracker.Domain.Interfaces;
using JobApplicationTracker.Infrastructure.Data;
using JobApplicationTracker.Infrastructure.Persistence;
using JobApplicationTracker.Infrastructure.Persistence.Repositories;
using JobApplicationTracker.Infrastructure.Services;
using MediatR;
using FluentValidation;
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

        services.AddScoped<IAppDbContext>(sp => sp.GetRequiredService<AppDbContext>());

        services.AddScoped<IUnitOfWork, UnitOfWork>();

        services.AddScoped<IUserRepository, UserRepository>();
        services.AddScoped<IJobApplicationRepository, JobApplicationRepository>();
        services.AddScoped<IDocumentRepository, DocumentRepository>();
        services.AddScoped<INoteRepository, NoteRepository>();

        services.AddScoped<IPasswordHasher, PasswordHasher>();

        return services;
    }

    public static IServiceCollection AddApplication(this IServiceCollection services)
    {
        services.AddMediatR(config => config.RegisterServicesFromAssembly(typeof(RegisterCommand).Assembly));
        services.AddValidatorsFromAssembly(typeof(RegisterCommandValidator).Assembly);
        services.AddScoped<IJwtTokenGenerator, JwtTokenGenerator>();

        return services;
    }
}