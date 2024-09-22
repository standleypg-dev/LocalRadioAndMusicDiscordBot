using System.Diagnostics;
using Discord;
using Discord.Audio;
using radio_discord_bot.Models;
using radio_discord_bot.Services.Interfaces;
using YoutubeExplode;
using YoutubeExplode.Videos.Streams;

namespace radio_discord_bot.Services.Implementations;

public class AudioService(YoutubeClient youtubeClient)
    : IAudioService
{
    private IVoiceChannel? _currentVoiceChannel;
    private bool _isPlaying;
    private bool _isRadioPlaying;
    private List<Song> _songs = [];
    private Process _ffmpegProcess;

    public async Task InitiateVoiceChannelAsync(IVoiceChannel? voiceChannel, string audioUrl, bool isYt = false)
    {
        try
        {
            _isPlaying = true;
            _isRadioPlaying = !isYt;
            dynamic outputUrl = isYt ? (await youtubeClient.Videos.Streams.GetManifestAsync(audioUrl)).GetAudioOnlyStreams().GetWithHighestBitrate().Url : audioUrl;

            await ConnectToVoiceChannelAsync(voiceChannel, outputUrl);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error on InitiateVoiceChannelAsync: {ex.Message}");
            _isPlaying = false;
            _isRadioPlaying = false;
        }
    }

    private async Task ConnectToVoiceChannelAsync(IVoiceChannel? voiceChannel, string audioUrl)
    {
        List<Task> tasks = new();
        IAudioClient _audioClient = await voiceChannel.ConnectAsync();
        //increase the buffer size to prevent the song ending early

        using var ffmpeg = CreateStream(audioUrl);
        using var audioOutStream = ffmpeg.StandardOutput.BaseStream;
        using var discord = _audioClient.CreatePCMStream(AudioApplication.Music);
        using var bufferedStream = new BufferedStream(discord, 16348);
        try
        {
            // Store the current voice channel
            SetBotCurrentVoiceChannel(voiceChannel);

            tasks.Add(audioOutStream.CopyToAsync(bufferedStream));
            tasks.Add(bufferedStream.FlushAsync());

            await Task.WhenAll(tasks);

        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error on ConnectToVoiceChannelAsync: {ex.Message}");
        }
        finally
        {
            if (_songs.Count > 0)
                await NextSongAsync();
            else
                await DestroyVoiceChannelAsync();

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
            Console.WriteLine($"error on DestroyVoiceChannelAsync: {ex}");
        }
    }

    private static Process CreateStream(string audioUrl)
    {
        var ffmpeg = new ProcessStartInfo
        {
            FileName = "/usr/bin/ffmpeg",
            Arguments = $"-reconnect 1 -reconnect_streamed 1 -v verbose -reconnect_delay_max 5 -i {audioUrl} -f s16le -ar 48000 -ac 2 -bufsize 120k pipe:1",
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
        if (_songs.Count > 0)
        {
            var song = _songs[0];
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
        _songs.Clear();
    }

    public void AddSong(Song song)
    {
        _songs.Add(song);
    }

    public List<Song> GetSongs()
    {
        return _songs;
    }

    public void RemoveFirstSong()
    {
        _songs.RemoveAt(0);
    }

    public async Task OnPlaylistChanged()
    {
        var song = _songs[0];
        if (_songs.Count > 0)
        {
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
