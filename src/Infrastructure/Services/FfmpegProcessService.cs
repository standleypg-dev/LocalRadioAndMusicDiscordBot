using System.Diagnostics;
using Application.Interfaces.Services;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace Infrastructure.Services;

public class FfmpegProcessService(ILogger<FfmpegProcessService> logger, IConfiguration configuration)
    : INativePlaceMusicProcessorService, IDisposable
{
    private Process? _ffmpegProcess;
    private bool _disposed;
    private readonly Lock _processLock = new();
    private readonly string _ffmpegPath = configuration["Ffmpeg:Path"] ?? "/usr/bin/ffmpeg";

    public async Task<Process> CreateStreamAsync(string audioUrl, CancellationToken cancellationToken)
    {
        // Make sure any previous process is gone before starting a new one.
        await StopCurrentProcessAsync();
        cancellationToken.ThrowIfCancellationRequested();

        var process = new Process
        {
            StartInfo = new ProcessStartInfo
            {
                FileName = _ffmpegPath,
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

        process.ErrorDataReceived += (_, e) =>
        {
            if (string.IsNullOrEmpty(e.Data)) return;

            var level = e.Data.Contains("error", StringComparison.OrdinalIgnoreCase) ||
                        e.Data.Contains("failed", StringComparison.OrdinalIgnoreCase)
                ? LogLevel.Error
                : e.Data.Contains("warning", StringComparison.OrdinalIgnoreCase)
                    ? LogLevel.Warning
                    : LogLevel.Debug;

            logger.Log(level, "FFmpeg: {Message}", e.Data);
        };

        try
        {
            if (!process.Start())
            {
                throw new InvalidOperationException("Failed to start ffmpeg process.");
            }

            process.BeginErrorReadLine();

            logger.LogInformation("FFmpeg process started for URL: {AudioUrl} (PID: {ProcessId})",
                audioUrl, process.Id);
        }
        catch
        {
            process.Dispose();
            throw;
        }

        lock (_processLock)
        {
            _ffmpegProcess = process;
        }

        return process;
    }

    public async Task StopCurrentProcessAsync()
    {
        Process? process;
        lock (_processLock)
        {
            process = _ffmpegProcess;
            _ffmpegProcess = null;
        }

        if (process is null) return;

        await TerminateProcessAsync(process);
    }

    private async Task TerminateProcessAsync(Process process)
    {
        try
        {
            try
            {
                if (process.HasExited)
                {
                    logger.LogDebug("FFmpeg process already exited (PID: {ProcessId})", process.Id);
                    return;
                }
            }
            catch (InvalidOperationException)
            {
                // Process was never started or already disposed
                return;
            }

            logger.LogInformation("Terminating FFmpeg process (PID: {ProcessId})...", process.Id);

            // Try graceful shutdown first
            try
            {
                if (!process.HasExited)
                {
                    process.StandardInput.WriteLine("q");
                    process.StandardInput.Close();
                }
            }
            catch (InvalidOperationException)
            {
                // Process might have exited between checks
            }

            if (await WaitForExitAsync(process, TimeSpan.FromMilliseconds(1500)))
            {
                logger.LogInformation("FFmpeg process terminated gracefully (PID: {ProcessId})", process.Id);
                return;
            }

            // Force kill if still running
            try
            {
                if (!process.HasExited)
                {
                    process.Kill(entireProcessTree: true);
                    await WaitForExitAsync(process, TimeSpan.FromMilliseconds(3000));
                    logger.LogInformation("FFmpeg process killed (PID: {ProcessId})", process.Id);
                }
            }
            catch (InvalidOperationException)
            {
                logger.LogDebug("Process exited before kill command");
            }
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "Error terminating FFmpeg process");
        }
        finally
        {
            try
            {
                process.Dispose();
            }
            catch (Exception ex)
            {
                logger.LogDebug(ex, "Error disposing FFmpeg process");
            }
        }
    }

    private static async Task<bool> WaitForExitAsync(Process process, TimeSpan timeout)
    {
        using var cts = new CancellationTokenSource(timeout);
        try
        {
            await process.WaitForExitAsync(cts.Token);
            return true;
        }
        catch (OperationCanceledException)
        {
            return false;
        }
    }

    public void Dispose()
    {
        if (_disposed) return;

        Process? process;
        lock (_processLock)
        {
            if (_disposed) return;
            _disposed = true;
            process = _ffmpegProcess;
            _ffmpegProcess = null;
        }

        if (process is null) return;

        // Best-effort synchronous kill during host shutdown.
        try
        {
            if (!process.HasExited)
            {
                process.Kill(entireProcessTree: true);
            }
        }
        catch (Exception ex)
        {
            logger.LogDebug(ex, "Error killing FFmpeg process during dispose");
        }
        finally
        {
            try
            {
                process.Dispose();
            }
            catch
            {
                // ignored
            }
        }
    }
}
