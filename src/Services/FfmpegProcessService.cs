using System.Diagnostics;
using Microsoft.Extensions.Logging;
using radio_discord_bot.Services.Interfaces;

namespace radio_discord_bot.Services;

public class FfmpegProcessService(ILogger<FfmpegProcessService> logger) : IFfmpegProcessService, IDisposable
{
    private Process? _ffmpegProcess;
    private CancellationTokenSource _cancellationTokenSource;
    private bool _disposed = false;

    public async Task<Process> CreateStream(string audioUrl, CancellationToken cancellationToken)
    {
        await Task.CompletedTask;
        if (cancellationToken.IsCancellationRequested)
        {
            return await Task.FromCanceled<Process>(cancellationToken);
        }
        TerminateFFmpegProcess();
        _cancellationTokenSource = new CancellationTokenSource();
        try
        {
            _ffmpegProcess = new Process()
            {
                StartInfo = new ProcessStartInfo
                {
                    FileName = "/usr/bin/ffmpeg",
                    Arguments =
                        $"-reconnect 1 -reconnect_streamed 1 -v verbose -reconnect_delay_max 5 -i {audioUrl} -f s16le -ar 48000 -ac 2 -bufsize 120k pipe:1",
                    RedirectStandardOutput = true,
                    UseShellExecute = false,
                    CreateNoWindow = true,
                    RedirectStandardError = true,
                }
            };
            _ffmpegProcess.Start();

            // Capture error output
            _ffmpegProcess.ErrorDataReceived += (sender, e) =>
            {
                if (!string.IsNullOrEmpty(e.Data))
                {
                    logger.LogInformation($"FFmpeg Log: {e.Data}");
                    // Console.WriteLine($"FFmpeg Log: {e.Data}");
                    // Or log to a file
                }
            };
            _ffmpegProcess.BeginErrorReadLine();
            
            return _ffmpegProcess;
        }
        catch (Exception e)
        {
            logger.LogError($"Error creating FFmpeg process: {e.Message}");
            throw;
        }
    }

    private void TerminateFFmpegProcess()
    {
        if (_ffmpegProcess is { HasExited: false })
        {
            try
            {
                _ffmpegProcess.Kill();
                _ffmpegProcess.Dispose();
            }
            catch (Exception ex)
            {
                logger.LogError($"Error terminating FFmpeg: {ex.Message}");
            }
        }

        _cancellationTokenSource?.Cancel();
    }
    
    public void Dispose()
    {
        _ffmpegProcess?.Dispose();
        GC.SuppressFinalize(this);
    }

    protected virtual void Dispose(bool disposing)
    {
        if (_disposed) return;
        if (disposing)
        {
            // Dispose managed resources
            TerminateFFmpegProcess();
            _cancellationTokenSource?.Dispose();
        }

        // Dispose unmanaged resources if any
        _disposed = true;
    }
    // Finalizer (optional but good for unmanaged resources)
    ~FfmpegProcessService()
    {
        Dispose(false);
    }
}