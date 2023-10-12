using Discord;
using Discord.Audio;
using System.Collections.Concurrent;
using System.Diagnostics;
using YoutubeExplode;
using YoutubeExplode.Videos.Streams;

public class AudioService
{
    private readonly ConcurrentDictionary<ulong, IAudioClient> _connectedChannels;
    private readonly YoutubeClient _youtubeClient;

    public AudioService()
    {
        _youtubeClient = new YoutubeClient();
        _connectedChannels = new ConcurrentDictionary<ulong, IAudioClient>();
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
            await discordStream.FlushAsync();
        }
        catch (Exception ex)
        {
            Console.WriteLine($"error on InitiateVoiceChannelAsync: {ex}");
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

}
