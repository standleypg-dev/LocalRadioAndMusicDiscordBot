using System.Diagnostics;
using radio_discord_bot.Services.Interfaces;

namespace radio_discord_bot.Services;

public sealed class FfmpegProcessService(ILogger<FfmpegProcessService> logger) : IFfmpegProcessService, IDisposable
{
    private Process? _ffmpegProcess;
    private CancellationTokenRegistration _cancellationRegistration;
    private readonly SemaphoreSlim _processLock = new(1, 1);
    private bool _disposed;

    public async Task<Process> CreateStream(string audioUrl, CancellationToken cancellationToken)
    {
        await Task.CompletedTask;

        await TerminateFFmpegProcessAsync();

        await _processLock.WaitAsync(cancellationToken);
        try
        {
            _ffmpegProcess = new Process()
            {
                StartInfo = new ProcessStartInfo
                {
                    FileName = "/usr/bin/ffmpeg",
                    Arguments =
                        $"-reconnect 1 -reconnect_streamed 1 -reconnect_delay_max 5 -i \"{audioUrl}\" -f s16le -ar 48000 -ac 2 -bufsize 120k pipe:1",
                    RedirectStandardOutput = true,
                    UseShellExecute = false,
                    CreateNoWindow = true,
                    RedirectStandardError = true,
                }
            };

            // Register cancellation BEFORE starting the process
            _cancellationRegistration = cancellationToken.Register(() =>
            {
                logger.LogInformation("Cancellation requested for FFmpeg process");
                // Use async termination to avoid blocking
                _ = Task.Run(async () => await TerminateFFmpegProcessAsync());
            });

            _ffmpegProcess.Start();

            // Capture error output for debugging
            _ffmpegProcess.ErrorDataReceived += (_, e) =>
            {
                if (!string.IsNullOrEmpty(e.Data))
                {
                    // Only log important messages to avoid spam
                    if (e.Data.Contains("error", StringComparison.OrdinalIgnoreCase) ||
                        e.Data.Contains("warning", StringComparison.OrdinalIgnoreCase) ||
                        e.Data.Contains("failed", StringComparison.OrdinalIgnoreCase))
                    {
                        logger.LogWarning("FFmpeg: {Message}", e.Data);
                    }
                    else
                    {
                        logger.LogDebug("FFmpeg: {Message}", e.Data);
                    }
                }
            };
            _ffmpegProcess.BeginErrorReadLine();

            // Handle process exit
            _ffmpegProcess.EnableRaisingEvents = true;
            _ffmpegProcess.Exited += (_, _) =>
            {
                logger.LogInformation("FFmpeg process exited with code: {ExitCode}", _ffmpegProcess?.ExitCode);
            };

            logger.LogInformation("FFmpeg process started for URL: {AudioUrl}", audioUrl);
            return _ffmpegProcess;
        }
        catch (Exception e)
        {
            logger.LogError(e, "Error creating FFmpeg process for URL: {AudioUrl}", audioUrl);

            // Clean up on error
            await TerminateFFmpegProcessAsync();
            throw;
        }
        finally
        {
            _processLock.Release();
        }
    }

    private async Task TerminateFFmpegProcessAsync()
    {
        Process? processToKill = null;

        await _processLock.WaitAsync();
        try
        {
            if (_ffmpegProcess != null && !_ffmpegProcess.HasExited)
            {
                processToKill = _ffmpegProcess;
            }
        }
        finally
        {
            _processLock.Release();
        }

        if (processToKill != null)
        {
            try
            {
                logger.LogInformation("Terminating FFmpeg process...");

                // First try graceful shutdown
                try
                {
                    processToKill.StandardInput.Close();

                    // Wait a short time for graceful exit
                    if (!processToKill.WaitForExit(1000))
                    {
                        // Force kill if it doesn't exit gracefully
                        processToKill.Kill(entireProcessTree: true);

                        // Wait for kill to complete
                        if (!processToKill.WaitForExit(3000))
                        {
                            logger.LogWarning("FFmpeg process did not exit after kill command");
                        }
                    }
                }
                catch (InvalidOperationException)
                {
                    // Process already exited
                    logger.LogDebug("FFmpeg process already exited");
                }
                catch (Exception ex)
                {
                    logger.LogError(ex, "Error during FFmpeg process termination");
                }
            }
            finally
            {
                await _processLock.WaitAsync();
                try
                {
                    _cancellationRegistration.Dispose();
                    processToKill.Dispose();

                    if (_ffmpegProcess == processToKill)
                    {
                        _ffmpegProcess = null;
                    }
                }
                catch (Exception ex)
                {
                    logger.LogWarning(ex, "Error disposing FFmpeg process resources");
                }
                finally
                {
                    _processLock.Release();
                }
            }
        }

        // Small delay to ensure process cleanup completes
        await Task.Delay(100);
    }

    public void Dispose()
    {
        _ = Dispose(true);
        GC.SuppressFinalize(this);
    }

    private async Task Dispose(bool disposing)
    {
        if (_disposed) return;

        if (disposing)
        {
            await _processLock.WaitAsync();
            try
            {
                _cancellationRegistration.Dispose();

                if (_ffmpegProcess != null && !_ffmpegProcess.HasExited)
                {
                    try
                    {
                        _ffmpegProcess.Kill(entireProcessTree: true);
                        _ffmpegProcess.WaitForExit(3000);
                    }
                    catch (Exception ex)
                    {
                        logger.LogWarning(ex, "Error killing FFmpeg process during disposal");
                    }
                }

                _ffmpegProcess?.Dispose();
                _ffmpegProcess = null;
            }
            catch (Exception ex)
            {
                logger.LogWarning(ex, "Error during FFmpeg service disposal");
            }
            finally
            {
                _processLock.Release();
            }
        }

        _disposed = true;
    }

    ~FfmpegProcessService()
    {
        _ = Dispose(false);
    }
}