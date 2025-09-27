namespace Application.Interfaces.Services;

public interface INetCordAudioPlayerService
{
    event Func<Task>? DisconnectedVoiceClientEvent;
    event Func<Task>? NotInVoiceChannelCallback;
    Task Play<T>(T ctx, Action<Func<Task>> disconnectAsync);
}