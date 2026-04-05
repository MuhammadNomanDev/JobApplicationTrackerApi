namespace JobApplicationTracker.Application.Interfaces.Services;

public interface IMessagePublisher : IAsyncDisposable
{
    Task PublishAsync<T>(T message, CancellationToken cancellationToken = default) where T : class;
}
