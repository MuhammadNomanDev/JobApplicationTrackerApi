using JobApplicationTrackerAPI.Application.Interfaces;
using JobApplicationTrackerAPI.Application.Interfaces.Services;
using JobApplicationTrackerAPI.Domain.Interfaces;
using JobApplicationTrackerAPI.Infrastructure.Data;
using JobApplicationTrackerAPI.Infrastructure.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace JobApplicationTrackerAPI.Infrastructure;

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

        services.AddScoped<IPasswordHasher, PasswordHasher>();
        services.AddScoped<IJwtTokenGenerator, JwtTokenGenerator>();
        services.AddScoped<IBlobStorageService, BlobStorageService>();
        services.AddScoped<IMessagePublisher, ServiceBusMessagePublisher>();
        services.AddHostedService<NotificationConsumer>();

        // HybridCache: in-memory L1 always; Redis L2 when a connection string
        // is configured (AddStackExchangeRedisCache registers the
        // IDistributedCache that HybridCache picks up as its L2).
        // Without Redis the app still caches locally — strictly better than
        // the old RedisCacheService, which silently no-op'd.
        services.AddHybridCache();

        var redisConnectionString = configuration["Redis:ConnectionString"];
        if (!string.IsNullOrWhiteSpace(redisConnectionString))
        {
            services.AddStackExchangeRedisCache(options =>
                options.Configuration = redisConnectionString);
        }

        services.AddSingleton<ICacheService, HybridCacheService>();

        return services;
    }
}
