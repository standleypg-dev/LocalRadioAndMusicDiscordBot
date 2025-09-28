using System.Diagnostics;
using Application.Interfaces.Services;
using Microsoft.Extensions.Logging;

namespace Infrastructure.Services;

public class FfmpegProcessService(ILogger<FfmpegProcessService> logger, IMusicQueueService queue)
    : INativePlaceMusicProcessorService, IDisposable
{
    private Process? _ffmpegProcess;
    private bool _disposed;
    public event Func<Task>? OnExitProcess;
    public event Func<Task>? OnProcessStart;
    
    public async Task<Process> CreateStreamAsync(string audioUrl, CancellationToken cancellationToken)
    {
        DisposeCurrentProcess();

        var process = new Process
        {
            StartInfo = new ProcessStartInfo
            {
                FileName = "/usr/bin/ffmpeg",
                Arguments =
                    $"-reconnect 1 -reconnect_streamed 1 -reconnect_delay_max 5 -i \"{audioUrl}\" -f s16le -ar 48000 -ac 2 -bufsize 120k pipe:1",
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                RedirectStandardInput = true,
                UseShellExecute = false,
                CreateNoWindow = true,
            },
            EnableRaisingEvents = true
        };

        // Set up logging
        process.ErrorDataReceived += (_, e) =>
        {
            if (string.IsNullOrEmpty(e.Data)) return;

            var level = e.Data.Contains("error", StringComparison.OrdinalIgnoreCase) ||
                        e.Data.Contains("warning", StringComparison.OrdinalIgnoreCase) ||
                        e.Data.Contains("failed", StringComparison.OrdinalIgnoreCase)
                ? LogLevel.Warning
                : LogLevel.Debug;

            logger.Log(level, "FFmpeg: {Message}", e.Data);
        };

        process.Exited += async (sender, _) =>
        {
            try
            {
                var exitCode = ((Process)sender!).ExitCode;
                logger.LogInformation("FFmpeg process exited with code: {ExitCode}", exitCode);

                await (OnExitProcess?.Invoke() ?? Task.CompletedTask);
            }
            catch (Exception ex)
            {
                logger.LogDebug(ex, "Error reading FFmpeg exit code");
            }
        };
        
        // Handle cancellation
        cancellationToken.Register(() =>
        {
            logger.LogInformation("Cancellation requested for FFmpeg process");
            DisposeCurrentProcess();
        });

        // Start the process
        if (!process.Start())
        {
            process.Dispose();
            throw new InvalidOperationException("Failed to start ffmpeg process.");
        }

        process.BeginErrorReadLine();
        _ffmpegProcess = process;

        logger.LogInformation("FFmpeg process started for URL: {AudioUrl}", audioUrl);
        await (OnProcessStart?.Invoke() ?? Task.CompletedTask);
        return process;
    }

    private void DisposeCurrentProcess()
    {
        if (_ffmpegProcess == null) return;

        var process = _ffmpegProcess;
        _ffmpegProcess = null;

        if (process.HasExited)
        {
            process.Dispose();
            return;
        }

        logger.LogInformation("Terminating FFmpeg process...");

        try
        {
            // Try graceful shutdown first
            if (!process.HasExited)
            {
                process.StandardInput?.WriteLine("q");
                process.StandardInput?.Close();
            }

            // Wait briefly for graceful exit
            if (!process.WaitForExit(1500))
            {
                // Force kill if still running
                process.Kill(entireProcessTree: true);
                process.WaitForExit(3000);
            }
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "Error terminating FFmpeg process");
        }
        finally
        {
            process.Dispose();
        }
    }

    public void Dispose()
    {
        if (_disposed) return;
        _disposed = true;

        DisposeCurrentProcess();
    }
}