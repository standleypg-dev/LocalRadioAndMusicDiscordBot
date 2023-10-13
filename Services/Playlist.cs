using Discord;

namespace radio_discord_bot;

public class Song
{
    public string url { get; set; }
    public IVoiceChannel voiceChannel { get; set; }
}
public class PlaylistService
{
    public static List<Song> playlist = new List<Song>();
    public static int previousPlaylistLength = playlist.Count;
}
