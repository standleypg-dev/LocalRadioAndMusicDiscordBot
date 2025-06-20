namespace Application.Interfaces.Services;

public interface IAudioPlayerService<T>
{
    Task InitiateVoiceChannelAsync(T? voiceChannel, string audioUrl, bool isYt = false);
    Task NextSongAsync();
    Task DestroyVoiceChannelAsync();
    Task OnPlaylistChanged();
    T? GetBotCurrentVoiceChannel();
}