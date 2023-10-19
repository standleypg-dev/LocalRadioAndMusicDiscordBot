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

        // Store the current voice channel
        _currentVoiceChannel = voiceChannel;

        tasks.Add(audioOutStream.CopyToAsync(discordStream));
        tasks.Add(discordStream.FlushAsync());

        await Task.WhenAll(tasks);

        await Task.Delay(TimeSpan.FromSeconds(5));

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
            Arguments = $"-i {audioUrl} -f s16le -ar 48000 -ac 2 pipe:1",
            RedirectStandardOutput = true,
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
