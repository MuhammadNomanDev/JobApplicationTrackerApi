using Azure.Messaging.ServiceBus;
using JobApplicationTracker.Application.Interfaces.Services;
using Microsoft.Extensions.Configuration;
using System.Text.Json;

namespace JobApplicationTracker.Infrastructure.Services;

public class ServiceBusMessagePublisher : IMessagePublisher
{
    private readonly ServiceBusSender _sender;
    private readonly bool _isConfigured;

    public ServiceBusMessagePublisher(IConfiguration configuration)
    {
        var connectionString = configuration["AzureServiceBus:ConnectionString"];
        var queueName = configuration["AzureServiceBus:QueueName"];

        if (string.IsNullOrEmpty(connectionString) || string.IsNullOrEmpty(queueName))
        {
            _isConfigured = false;
            _sender = null!;
            return;
        }

        _isConfigured = true;
        var client = new ServiceBusClient(connectionString);
        _sender = client.CreateSender(queueName);
    }

    public async Task PublishAsync<T>(T message, CancellationToken cancellationToken = default) where T : class
    {
        if (!_isConfigured)
        {
            // Gracefully skip if Service Bus is not configured (e.g., development)
            return;
        }

        var json = JsonSerializer.Serialize(message);
        var serviceBusMessage = new ServiceBusMessage(json)
        {
            ContentType = "application/json",
            MessageId = Guid.NewGuid().ToString(),
            Subject = typeof(T).Name
        };

        await _sender.SendMessageAsync(serviceBusMessage, cancellationToken);
    }

    public async ValueTask DisposeAsync()
    {
        if (_sender != null)
        {
            await _sender.DisposeAsync();
        }
        GC.SuppressFinalize(this);
    }
}
