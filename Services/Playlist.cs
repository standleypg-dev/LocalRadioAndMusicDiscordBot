using Discord;

namespace radio_discord_bot;

public class Song
{
    public string Url { get; set; }
    public IVoiceChannel VoiceChannel { get; set; }
}
public class PlaylistService
{
    public static List<Song> playlist = new List<Song>();
    public static int previousPlaylistLength = playlist.Count;
}
