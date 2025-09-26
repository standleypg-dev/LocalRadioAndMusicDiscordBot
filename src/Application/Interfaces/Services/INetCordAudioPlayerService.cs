using Domain.Common;

namespace Application.Interfaces.Services;

public interface INetCordAudioPlayerService
{
    Task Play<T>(T ctx, Func<Task> notInVoiceChannelCallback, Action<Func<Task>> disconnectAsync, TokenContainer tokens);
}