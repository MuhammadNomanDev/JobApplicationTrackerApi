using Azure.Messaging.ServiceBus;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Configuration;
using System.Text.Json;
using JobApplicationTracker.Application.Events;

namespace JobApplicationTracker.Infrastructure.Services;

/// <summary>
/// Background service that consumes messages from Azure Service Bus queue.
/// This is a placeholder implementation - in production, this would process
/// notifications (send emails, webhooks, etc.).
/// </summary>
public class NotificationConsumer : BackgroundService
{
    private readonly ILogger<NotificationConsumer> _logger;
    private readonly IConfiguration _configuration;

    public NotificationConsumer(ILogger<NotificationConsumer> logger, IConfiguration configuration)
    {
        _logger = logger;
        _configuration = configuration;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        var connectionString = _configuration["AzureServiceBus:ConnectionString"];
        var queueName = _configuration["AzureServiceBus:QueueName"];

        if (string.IsNullOrEmpty(connectionString) || string.IsNullOrEmpty(queueName))
        {
            _logger.LogInformation("Azure Service Bus not configured. Notification consumer disabled.");
            return;
        }

        var client = new ServiceBusClient(connectionString);
        var receiver = client.CreateReceiver(queueName);

        _logger.LogInformation("Notification consumer started. Listening on queue: {QueueName}", queueName);

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                var message = await receiver.ReceiveMessageAsync(TimeSpan.FromSeconds(5), stoppingToken);

                if (message == null)
                    continue;

                await ProcessMessageAsync(message, stoppingToken);
                await receiver.CompleteMessageAsync(message, stoppingToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error processing message from Service Bus");
            }
        }
    }

    private Task ProcessMessageAsync(ServiceBusReceivedMessage message, CancellationToken cancellationToken)
    {
        var body = message.Body.ToString();
        _logger.LogInformation("Received message: {Subject} - {Body}", message.Subject, body);

        // TODO: Process based on message type
        // Example: if message.Subject == nameof(JobApplicationStatusChangedEvent)
        // - Send email notification
        // - Trigger webhook
        // - Update external systems

        return Task.CompletedTask;
    }
}
