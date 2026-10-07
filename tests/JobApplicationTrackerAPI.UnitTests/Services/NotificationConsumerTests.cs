using AwesomeAssertions;
using JobApplicationTrackerAPI.Infrastructure.Services;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace JobApplicationTrackerAPI.UnitTests.Services;

/// <summary>
/// P1/M1k: the notification consumer's disabled path — with no Service Bus
/// configured (dev/test), it logs and returns instead of entering the
/// receive loop. ExecuteAsync is invoked directly: BackgroundService.StartAsync
/// launches it on a background task, which would race the assertion.
/// </summary>
public class NotificationConsumerTests
{
    private sealed class RecordingLogger<T> : ILogger<T>
    {
        public List<string> Messages { get; } = new();

        public IDisposable? BeginScope<TState>(TState state) where TState : notnull => null;

        public bool IsEnabled(LogLevel logLevel) => true;

        public void Log<TState>(
            LogLevel logLevel, EventId eventId, TState state,
            Exception? exception, Func<TState, Exception?, string> formatter)
            => Messages.Add(formatter(state, exception));
    }

    private sealed class ExposedConsumer : NotificationConsumer
    {
        public ExposedConsumer(ILogger<NotificationConsumer> logger, IConfiguration configuration)
            : base(logger, configuration) { }

        public Task RunAsync(CancellationToken cancellationToken) => ExecuteAsync(cancellationToken);
    }

    [Fact]
    public async Task ExecuteAsync_WhenNotConfigured_LogsDisabled_AndReturns()
    {
        var config = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>())
            .Build();
        var logger = new RecordingLogger<NotificationConsumer>();
        using var consumer = new ExposedConsumer(logger, config);

        using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(5));
        await consumer.RunAsync(cts.Token);

        logger.Messages.Should().ContainSingle(m => m.Contains("disabled"));
    }
}
