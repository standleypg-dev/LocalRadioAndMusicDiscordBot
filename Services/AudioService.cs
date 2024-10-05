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

namespace radio_discord_bot.Services;

public class AudioService(YoutubeClient youtubeClient, GlobalStore globalStore) : IAudioService
{
    private readonly GlobalStore _globalStore = globalStore ?? throw new ArgumentNullException(nameof(globalStore));
    private IVoiceChannel? _currentVoiceChannel;
    private bool _isPlaying;
    private bool _isRadioPlaying;
    private Process? _ffmpegProcess;
    private IAudioClient? _audioClient;
    private CancellationTokenSource? _cancellationTokenSource;

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
        catch (OperationCanceledException ex)
        {
            await ReplyToChannel.FollowupAsync(_globalStore.Get<SocketMessageComponent>()!,
                $"Error on InitiateVoiceChannelAsync: {ex.Message}");

            _isPlaying = false;
            _isRadioPlaying = false;
        }
    }

    private async Task ConnectToVoiceChannelAsync(IVoiceChannel? voiceChannel, string audioUrl)
    {
        if (_cancellationTokenSource is not null && !_cancellationTokenSource.IsCancellationRequested)
        {
            await _cancellationTokenSource.CancelAsync();
            _cancellationTokenSource = new CancellationTokenSource();
        }

        var cancellationToken = _cancellationTokenSource.Token;

        List<Task> tasks = new();
        if (voiceChannel is not null)
            _audioClient = await voiceChannel.ConnectAsync();

        if (_audioClient is not null)
        {
            CreateStream(audioUrl);
            await using var audioOutStream = _ffmpegProcess!.StandardOutput.BaseStream;
            await using var discord = _audioClient.CreatePCMStream(AudioApplication.Music);
            await using var bufferedStream = new BufferedStream(discord, 2048);
            try
            {
                // Store the current voice channel
                SetBotCurrentVoiceChannel(voiceChannel);

                tasks.Add(audioOutStream.CopyToAsync(bufferedStream, cancellationToken));
                tasks.Add(bufferedStream.FlushAsync(cancellationToken));

                await Task.WhenAll(tasks);

                if (_globalStore.Get<List<Song>>()?.Count > 0)
                    await NextSongAsync();
                else
                    await DestroyVoiceChannelAsync();
            }
            catch (Exception ex)
            {
                await ReplyToChannel.FollowupAsync(_globalStore.Get<SocketMessageComponent>()!,
                    $"Error on ConnectToVoiceChannelAsync: {ex.Message}");
            }
            finally
            {
                await discord.FlushAsync(cancellationToken);
                await bufferedStream.FlushAsync(cancellationToken);
            }
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
            _audioClient = null;
        }
        catch (Exception ex)
        {
            await ReplyToChannel.FollowupAsync(_globalStore.Get<SocketMessageComponent>()!,
                $"Error on DestroyVoiceChannelAsync: {ex.Message}");
        }
    }

    private void CreateStream(string audioUrl)
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
        _ffmpegProcess = Process.Start(ffmpeg)!;

        // Capture error output
        _ffmpegProcess.ErrorDataReceived += (sender, e) =>
        {
            if (!string.IsNullOrEmpty(e.Data))
            {
                Console.WriteLine($"FFmpeg Log: {e.Data}");
                // Or log to a file
            }
        };
        _ffmpegProcess.BeginErrorReadLine();
    }

    private void TerminateStream()
    {
        _ffmpegProcess?.Kill();
        _ffmpegProcess?.Dispose();
        _ffmpegProcess = null;
    }


    public async Task NextSongAsync()
    {
        RemoveFirstSong();
        // Terminate the previous stream before playing the next song
        TerminateStream();
        if (_cancellationTokenSource is not null && !_cancellationTokenSource.IsCancellationRequested)
        {
            await _cancellationTokenSource.CancelAsync();
        }

        if (_globalStore.Get<List<Song>>()?.Count > 0)
        {
            var song = _globalStore.Get<List<Song>>()?[0];
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
        _globalStore.Get<List<Song>>()?.Clear();
    }

    public async Task AddSong(Song song)
    {
        if (_globalStore.Get<List<Song>>() is null)
            _globalStore.Set(new List<Song>());

        _globalStore.Get<List<Song>>()?.Add(song);

        var component = _globalStore.Get<SocketMessageComponent>()!;
        var videoTitle = await GetYoutubeTitle(component.Data.CustomId);

        await ReplyToChannel.FollowupAsync(component,
            $"Added {videoTitle} to queue. Total songs in a queue is {GetSongs().Count}");
    }

    public List<Song> GetSongs()
    {
        return _globalStore.Get<List<Song>>() ?? new List<Song>();
    }

    public void RemoveFirstSong()
    {
        _globalStore.Get<List<Song>>()?.RemoveAt(0);
    }

    public async Task OnPlaylistChanged()
    {
        var songs = _globalStore.Get<List<Song>>();
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