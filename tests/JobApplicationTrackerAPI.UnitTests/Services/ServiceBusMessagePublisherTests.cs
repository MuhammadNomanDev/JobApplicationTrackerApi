using System.Text.Json;
using AwesomeAssertions;
using Azure.Messaging.ServiceBus;
using JobApplicationTrackerAPI.Infrastructure.Services;
using Microsoft.Extensions.Configuration;
using Moq;

namespace JobApplicationTrackerAPI.UnitTests.Services;

/// <summary>
/// P1/M1i: Service Bus publisher tests. The unconfigured path (graceful skip)
/// uses the real constructor; the configured path injects a mocked sender via
/// the test-seam constructor.
/// </summary>
public class ServiceBusMessagePublisherTests
{
    private sealed record StatusChanged(Guid JobApplicationId, string NewStatus);

    private static IConfiguration EmptyConfig()
        => new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>())
            .Build();

    [Fact]
    public async Task PublishAsync_WhenNotConfigured_ReturnsGracefully()
    {
        await using var publisher = new ServiceBusMessagePublisher(EmptyConfig());

        var act = () => publisher.PublishAsync(new StatusChanged(Guid.NewGuid(), "Applied"));

        await act.Should().NotThrowAsync();
    }

    [Fact]
    public async Task DisposeAsync_WhenNotConfigured_DoesNotThrow()
    {
        await using var publisher = new ServiceBusMessagePublisher(EmptyConfig());

        var act = () => publisher.DisposeAsync().AsTask();

        await act.Should().NotThrowAsync();
    }

    [Fact]
    public async Task PublishAsync_WhenConfigured_SendsJsonMessage()
    {
        var mockSender = new Mock<ServiceBusSender>();
        ServiceBusMessage? sent = null;
        mockSender
            .Setup(s => s.SendMessageAsync(It.IsAny<ServiceBusMessage>(), It.IsAny<CancellationToken>()))
            .Callback<ServiceBusMessage, CancellationToken>((m, _) => sent = m)
            .Returns(Task.CompletedTask);

        await using var publisher = new ServiceBusMessagePublisher(mockSender.Object);
        var @event = new StatusChanged(Guid.NewGuid(), "Interviewing");

        await publisher.PublishAsync(@event);

        mockSender.Verify(s => s.SendMessageAsync(
            It.IsAny<ServiceBusMessage>(), It.IsAny<CancellationToken>()), Times.Once);
        sent.Should().NotBeNull();
        sent!.ContentType.Should().Be("application/json");
        sent.Subject.Should().Be(nameof(StatusChanged));
        sent.MessageId.Should().NotBeNullOrEmpty();
        JsonSerializer.Deserialize<StatusChanged>(sent.Body.ToString())
            .Should().BeEquivalentTo(@event);
    }

    [Fact]
    public async Task DisposeAsync_WhenConfigured_DisposesSender()
    {
        var mockSender = new Mock<ServiceBusSender>();
        await using (var publisher = new ServiceBusMessagePublisher(mockSender.Object))
        {
        }

        mockSender.Verify(s => s.DisposeAsync(), Times.Once);
    }
}
