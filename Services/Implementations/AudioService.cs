using System.Diagnostics;
using Discord;
using Discord.Audio;
using Discord.WebSocket;
using radio_discord_bot.Models;
using radio_discord_bot.Services.Interfaces;
using radio_discord_bot.Store;
using radio_discord_bot.Utils;
using YoutubeExplode;
using YoutubeExplode.Videos.Streams;

namespace radio_discord_bot.Services.Implementations;

public class AudioService(YoutubeClient youtubeClient, GlobalStore globalStore) : IAudioService
{
    private IVoiceChannel? _currentVoiceChannel;
    private bool _isPlaying;
    private bool _isRadioPlaying;
    private Process _ffmpegProcess;
    
    public async Task InitiateVoiceChannelAsync(IVoiceChannel? voiceChannel, string audioUrl, bool isYt = false)
    {
        try
        {
            _isPlaying = true;
            _isRadioPlaying = !isYt;
            dynamic outputUrl = isYt
                ? (await youtubeClient.Videos.Streams.GetManifestAsync(audioUrl)).GetAudioOnlyStreams()
                .GetWithHighestBitrate().Url
                : audioUrl;

            await ConnectToVoiceChannelAsync(voiceChannel, outputUrl);
        }
        catch (Exception ex)
        {
            await ReplyToChannel.FollowupAsync(globalStore.Get<SocketMessageComponent>()!,
                $"Error on InitiateVoiceChannelAsync: {ex.Message}");
        
            _isPlaying = false;
            _isRadioPlaying = false;
        }
    }

    private async Task ConnectToVoiceChannelAsync(IVoiceChannel? voiceChannel, string audioUrl)
    {
        List<Task> tasks = new();

        IAudioClient audioClient = await voiceChannel.ConnectAsync();
        //increase the buffer size to prevent the song ending early1

        using var ffmpeg = CreateStream(audioUrl);
        await using var audioOutStream = ffmpeg.StandardOutput.BaseStream;
        await using var discord = audioClient.CreatePCMStream(AudioApplication.Music);
        await using var bufferedStream = new BufferedStream(discord, 16348);
        try
        {
            // Store the current voice channel
            SetBotCurrentVoiceChannel(voiceChannel);

            tasks.Add(audioOutStream.CopyToAsync(bufferedStream));
            tasks.Add(bufferedStream.FlushAsync());

            await Task.WhenAll(tasks);

            if (globalStore.Get<List<Song>>()?.Count > 0)
                await NextSongAsync();
            else
                await DestroyVoiceChannelAsync();
        }
        catch (Exception ex)
        {
            await ReplyToChannel.FollowupAsync(globalStore.Get<SocketMessageComponent>()!,
                $"Error on ConnectToVoiceChannelAsync: {ex.Message}");
        }
        finally
        {
            await discord.FlushAsync();
            await bufferedStream.FlushAsync();
        }
    }

    public async Task DestroyVoiceChannelAsync()
    {
        try
        {
            if (_currentVoiceChannel != null)
            {
                await _currentVoiceChannel.DisconnectAsync();
                TerminateStream();
            }

            _currentVoiceChannel = null;
            _isPlaying = false;
            _isRadioPlaying = false;
        }
        catch (Exception ex)
        {
            await ReplyToChannel.FollowupAsync(globalStore.Get<SocketMessageComponent>()!,
                $"Error on DestroyVoiceChannelAsync: {ex.Message}");
        }
    }

    private static Process CreateStream(string audioUrl)
    {
        var ffmpeg = new ProcessStartInfo
        {
            FileName = "/usr/bin/ffmpeg",
            Arguments =
                $"-reconnect 1 -reconnect_streamed 1 -v verbose -reconnect_delay_max 5 -i {audioUrl} -f s16le -ar 48000 -ac 2 -bufsize 120k pipe:1",
            RedirectStandardOutput = true,
            UseShellExecute = false,
            CreateNoWindow = true,
            RedirectStandardError = true,
        };
        var process = Process.Start(ffmpeg)!;

        // Capture error output
        process.ErrorDataReceived += (sender, e) =>
        {
            if (!string.IsNullOrEmpty(e.Data))
            {
                Console.WriteLine($"FFmpeg Log: {e.Data}");
                // Or log to a file
            }
        };
        process.BeginErrorReadLine();

        return process;
    }

    private void TerminateStream()
    {
        _ffmpegProcess?.Kill();
        _ffmpegProcess?.Dispose();
    }


    public async Task NextSongAsync()
    {
        RemoveFirstSong();
        // Terminate the previous stream before playing the next song
        TerminateStream();
        if (globalStore.Get<List<Song>>()?.Count > 0)
        {
            var song = globalStore.Get<List<Song>>()?[0];
            if (song != null)
                await InitiateVoiceChannelAsync(song.VoiceChannel, song.Url, isYt: true);
        }
        else
        {
            await DestroyVoiceChannelAsync();
        }
    }

    public async Task EmptyPlaylist()
    {
        await Task.CompletedTask;
        globalStore.Get<List<Song>>()?.Clear();
    }

    public async Task AddSong(Song song)
    {
        if (globalStore.Get<List<Song>>() is null)
            globalStore.Set(new List<Song>());
        
        globalStore.Get<List<Song>>()?.Add(song);
        
        var component = globalStore.Get<SocketMessageComponent>()!;
        var videoTitle = await GetYoutubeTitle(component.Data.CustomId);
        
        await ReplyToChannel.FollowupAsync(component,
            $"Added {videoTitle} to queue. Total songs in a queue is {GetSongs().Count}");
    }

    public List<Song> GetSongs()
    {
        return globalStore.Get<List<Song>>() ?? new List<Song>();
    }

    public void RemoveFirstSong()
    {
        globalStore.Get<List<Song>>()?.RemoveAt(0);
    }

    public async Task OnPlaylistChanged()
    {
        var songs = globalStore.Get<List<Song>>();
        if (songs is not null)
        {
            var song = songs[0];
            if (!_isPlaying || _isRadioPlaying)
                await InitiateVoiceChannelAsync(song.VoiceChannel, song.Url, isYt: true);
        }
        else
        {
            await DestroyVoiceChannelAsync();
        }
    }

    private void SetBotCurrentVoiceChannel(IVoiceChannel? voiceChannel)
    {
        _currentVoiceChannel = voiceChannel;
    }

    public IVoiceChannel? GetBotCurrentVoiceChannel()
    {
        return _currentVoiceChannel;
    }

    public async Task<string> GetYoutubeTitle(string url)
    {
        var video = await youtubeClient.Videos.GetAsync(url);

        return $"{video.Title}";
    }
}