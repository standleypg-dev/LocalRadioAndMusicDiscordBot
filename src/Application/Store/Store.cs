using System.Threading.Channels;

namespace Application.Store;

public class Enqueuer<T>(Channel<T> channel)
{
    public async Task EnqueueAsync(T item, CancellationToken cancellationToken = default)
    {
        await channel.Writer.WriteAsync(item, cancellationToken);
    }
}