namespace Application.Interfaces.Services;

public delegate void OnSongAdded(string title);

public interface IQueueService<TSongVoiceChannel>
{
    event OnSongAdded? SongAdded;
    Task AddSongAsync(TSongVoiceChannel song, bool followup = true);
    Task SkipSongAsync();
    Task ClearQueueAsync();
    Task<List<TSongVoiceChannel>> GetQueueAsync();
}