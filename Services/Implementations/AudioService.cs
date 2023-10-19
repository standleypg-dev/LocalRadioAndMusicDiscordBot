using System.Collections.Concurrent;
using System.Diagnostics;
using Discord;
using Discord.Audio;
using radio_discord_bot.Models;
using YoutubeExplode;
using YoutubeExplode.Videos.Streams;

namespace radio_discord_bot.Services;

public class AudioService : IAudioService
{
    private readonly YoutubeClient _youtubeClient;
    private IVoiceChannel _currentVoiceChannel;
    private IAudioClient _audioClient;
    private bool isPlaying = false;
    private bool isRadioPlaying = false;
    private List<Song> songs = new();


    public AudioService(YoutubeClient youtubeClient)
    {
        _youtubeClient = youtubeClient;
    }

    public async Task InitiateVoiceChannelAsync(IVoiceChannel voiceChannel, string audioUrl, bool isYt = false)
    {
        if (_currentVoiceChannel != null)
            await DestroyVoiceChannelAsync();
        try
        {
            isPlaying = true;
            isRadioPlaying = !isYt;
            dynamic outputUrl = isYt ? (await _youtubeClient.Videos.Streams.GetManifestAsync(audioUrl)).GetAudioOnlyStreams().GetWithHighestBitrate().Url : audioUrl;

            await ConnectToVoiceChannelAsync(voiceChannel, outputUrl);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error on InitiateVoiceChannelAsync: {ex.Message}");
            isPlaying = false;
            isRadioPlaying = false;
        }
    }

    private async Task ConnectToVoiceChannelAsync(IVoiceChannel voiceChannel, string audioUrl)
    {
        List<Task> tasks = new();
        var ffmpeg = CreateStream(audioUrl);
        
        var audioOutStream = ffmpeg.StandardOutput.BaseStream;
        _audioClient = await voiceChannel.ConnectAsync();
        var discordStream = _audioClient.CreatePCMStream(AudioApplication.Music);

        //increase the buffer size to prevent the song ending early
        var bufferedStream = new BufferedStream(discordStream, 16348);

        // Store the current voice channel
        _currentVoiceChannel = voiceChannel;

        tasks.Add(audioOutStream.CopyToAsync(bufferedStream));
        tasks.Add(bufferedStream.FlushAsync());

        await Task.WhenAll(tasks);

        if (songs.Count > 0)
            await NextSongAsync();

        await DestroyVoiceChannelAsync();
    }

    public async Task DestroyVoiceChannelAsync()
    {

        try
        {
            await _currentVoiceChannel.DisconnectAsync();
            _currentVoiceChannel = null;
            isPlaying = false;
            isRadioPlaying = false;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"error on DestroyVoiceChannelAsync: {ex}");
        }
    }

    private Process CreateStream(string audioUrl)
    {
        var ffmpeg = new ProcessStartInfo
        {
            FileName = "/usr/bin/ffmpeg",
            Arguments = $"-reconnect 1 -reconnect_streamed 1 -reconnect_delay_max 5 -i {audioUrl} -f s16le -ar 48000 -ac 2 -bufsize 120k pipe:1",
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false,
            CreateNoWindow = true,
        };
        return Process.Start(ffmpeg);
    }


    public async Task NextSongAsync()
    {
        RemoveFirstSong();
        if (songs.Count > 0)
        {
            var song = songs[0];
            await InitiateVoiceChannelAsync(song.VoiceChannel, song.Url, isYt: true);
        }
    }

    public async Task EmptyPlaylist()
    {
        await Task.CompletedTask;
        songs.Clear();
    }

    public void AddSong(Song song)
    {
        songs.Add(song);
    }

    public List<Song> GetSongs()
    {
        return songs;
    }

    public void RemoveFirstSong()
    {
        songs.RemoveAt(0);
    }

    public async Task OnPlaylistChanged()
    {
        var song = songs[0];
        if (songs.Count > 0)
        {
            if (!isPlaying || isRadioPlaying)
                await InitiateVoiceChannelAsync(song.VoiceChannel, song.Url, isYt: true);
        }
        else
        {
            await DestroyVoiceChannelAsync();
        }
    }
}
