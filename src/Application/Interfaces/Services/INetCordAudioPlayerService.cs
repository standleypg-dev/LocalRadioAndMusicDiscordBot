namespace Application.Interfaces.Services;

public interface INetCordAudioPlayerService
{
    Task Play<T>(T ctx, Func<Task> notInVoiceChannelCallback, CancellationToken cancellationToken);
}