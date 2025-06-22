namespace Application.Interfaces.Services;

public interface IAudioPlayerService<in TSongDtoSocketVoiceChannel, out TSocketVoiceChannel>
{
    Task InitiateVoiceChannelAsync(TSongDtoSocketVoiceChannel songDto, bool isYt = false);
    Task NextSongAsync();
    Task DestroyVoiceChannelAsync();
    Task OnPlaylistChanged();
    TSocketVoiceChannel? GetBotCurrentVoiceChannel();
}