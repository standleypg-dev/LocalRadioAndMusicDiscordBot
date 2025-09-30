namespace Application.Interfaces.Services;

public interface INetCordAudioPlayerService
{
    event Func<Task>? DisconnectedVoiceClientEvent;
    event Func<Task>? NotInVoiceChannelCallback;
    Task Play(Action<Func<Task>> disconnectAsync);
}