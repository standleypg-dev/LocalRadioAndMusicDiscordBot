using Discord;
using Discord.Audio;
using radio_discord_bot;
using System.Collections.Concurrent;
using System.Diagnostics;
using Victoria.Player.Args;
using YoutubeExplode;
using YoutubeExplode.Videos.Streams;

public class AudioService : PlaylistService
{
    private readonly ConcurrentDictionary<ulong, IAudioClient> _connectedChannels;
    private readonly YoutubeClient _youtubeClient;

    public AudioService(YoutubeClient youtubeClient, PlaylistService playlistService)
    {
        _connectedChannels = new ConcurrentDictionary<ulong, IAudioClient>();
        _youtubeClient = youtubeClient;
    }

    public async Task InitiateVoiceChannelAsync(IVoiceChannel voiceChannel, string audioUrl)
    {

        try
        {
            var channel = await voiceChannel.ConnectAsync();
            var ffmpeg = CreateStream(audioUrl);
            var audioOutStream = ffmpeg.StandardOutput.BaseStream;
            var discordStream = channel.CreatePCMStream(AudioApplication.Music);
            await audioOutStream.CopyToAsync(discordStream);
            await discordStream.FlushAsync();
        }
        catch (Exception ex)
        {
            Console.WriteLine($"error on InitiateVoiceChannelAsync: {ex}");
            playlist.Clear();
        }

    }

    public async Task InitiateVoiceChannelAsyncYt(IVoiceChannel voiceChannel, string videoId)
    {
        try
        {
            var channel = await voiceChannel.ConnectAsync();
            var streamManifest = await _youtubeClient.Videos.Streams.GetManifestAsync(videoId);
            var audioStreamInfo = streamManifest.GetAudioOnlyStreams().GetWithHighestBitrate();
            var ffmpeg = CreateStream(audioStreamInfo.Url);
            var audioOutStream = ffmpeg.StandardOutput.BaseStream;
            var discordStream = channel.CreatePCMStream(AudioApplication.Music);
            await audioOutStream.CopyToAsync(discordStream);
            if (audioOutStream.CanRead)
            {
                await discordStream.FlushAsync();
                await DestroyVoiceChannelAsync(voiceChannel);
            }


            await NextSongAsync();
            await OnPlaylistChangedAsync();

        }
        catch (Exception ex)
        {
            Console.WriteLine($"error on InitiateVoiceChannelAsync: {ex}");
            playlist.Clear();
        }

    }

    public async Task DestroyVoiceChannelAsync(IVoiceChannel voiceChannel)
    {

        try
        {
            await voiceChannel.DisconnectAsync();
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

    public async Task OnPlaylistChangedAsync()
    {
        await Task.CompletedTask;
        int currentPlaylistLength = playlist.Count;

        if (currentPlaylistLength < previousPlaylistLength || currentPlaylistLength == 1)
        {
            var currentSong = playlist[0];
            await InitiateVoiceChannelAsyncYt(currentSong.VoiceChannel, currentSong.Url);
        }
        
        previousPlaylistLength = currentPlaylistLength;
    }

    public async Task NextSongAsync()
    {
        await Task.CompletedTask;
        if (playlist.Count > 0)
            playlist.RemoveAt(0);
    }

}
