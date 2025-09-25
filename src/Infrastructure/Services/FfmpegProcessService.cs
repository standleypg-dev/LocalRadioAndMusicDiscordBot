using System.Diagnostics;
using Application.Interfaces.Services;
using Microsoft.Extensions.Logging;

namespace Infrastructure.Services;

public sealed class FfmpegProcessService(ILogger<FfmpegProcessService> logger)
    : INativePlaceMusicProcessorService, IDisposable
{
    private readonly SemaphoreSlim _processLock = new(1, 1);

    private Process? _ffmpegProcess;
    private CancellationTokenRegistration _cancellationRegistration;
    private bool _disposed;

    public async Task<Process> CreateStreamAsync(string audioUrl, CancellationToken cancellationToken)
    {
        // Ensure any previous process is gone before starting a new one
        await TerminateFFmpegProcessAsync().ConfigureAwait(false);

        await _processLock.WaitAsync(cancellationToken).ConfigureAwait(false);
        try
        {
            // Safety: kill any leftover registration before creating a new one
            await _cancellationRegistration.DisposeAsync();

            _ffmpegProcess = new Process
            {
                StartInfo = new ProcessStartInfo
                {
                    FileName = "/usr/bin/ffmpeg",
                    Arguments = $"-reconnect 1 -reconnect_streamed 1 -reconnect_delay_max 5 -i \"{audioUrl}\" -f s16le -ar 48000 -ac 2 -bufsize 120k pipe:1",
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    RedirectStandardInput = true, // needed to send 'q'
                    UseShellExecute = false,
                    CreateNoWindow = true,
                },
                EnableRaisingEvents = true
            };

            // Register exit handler before Start to avoid missing an immediate exit race
            _ffmpegProcess.Exited += (_, _) =>
            {
                try
                {
                    var code = _ffmpegProcess?.ExitCode;
                    logger.LogInformation("FFmpeg process exited with code: {ExitCode}", code);
                }
                catch (Exception ex)
                {
                    logger.LogDebug(ex, "Error reading FFmpeg exit code");
                }
            };

            // Cancellation handler (never block here)
            _cancellationRegistration = cancellationToken.Register(() =>
            {
                logger.LogInformation("Cancellation requested for FFmpeg process");
                _ = Task.Run(TerminateFFmpegProcessAsync, cancellationToken);
            });

            // Start process
            if (!_ffmpegProcess.Start())
                throw new InvalidOperationException("Failed to start ffmpeg process.");

            // Wire stderr logging
            _ffmpegProcess.ErrorDataReceived += (_, e) =>
            {
                if (string.IsNullOrEmpty(e.Data)) return;

                var msg = e.Data;
                if (msg.Contains("error", StringComparison.OrdinalIgnoreCase) ||
                    msg.Contains("warning", StringComparison.OrdinalIgnoreCase) ||
                    msg.Contains("failed", StringComparison.OrdinalIgnoreCase))
                {
                    logger.LogWarning("FFmpeg: {Message}", msg);
                }
                else
                {
                    logger.LogDebug("FFmpeg: {Message}", msg);
                }
            };
            _ffmpegProcess.BeginErrorReadLine();

            logger.LogInformation("FFmpeg process started for URL: {AudioUrl}", audioUrl);
            return _ffmpegProcess;
        }
        catch
        {
            // If start failed, make sure we clean up
            await TerminateFFmpegProcessAsync().ConfigureAwait(false);
            throw;
        }
        finally
        {
            _processLock.Release();
        }
    }

    private async Task TerminateFFmpegProcessAsync()
    {
        Process? p = null;

        // Snapshot the current process under lock (don't hold the lock while waiting on the process)
        await _processLock.WaitAsync().ConfigureAwait(false);
        try
        {
            if (_ffmpegProcess is { HasExited: false } current)
            {
                p = current;
            }
        }
        finally
        {
            _processLock.Release();
        }

        if (p is null)
        {
            // Nothing to do; small delay to avoid hot loops when called repeatedly
            await Task.Delay(50).ConfigureAwait(false);
            return;
        }

        logger.LogInformation("Terminating FFmpeg process...");

        try
        {
            // 1) Try graceful shutdown by sending 'q' on stdin
            try
            {
                if (p.StartInfo.RedirectStandardInput && !p.HasExited)
                {
                    // Some ffmpeg builds exit on 'q' (then newline for safety)
                    await p.StandardInput.WriteLineAsync("q").ConfigureAwait(false);
                    await p.StandardInput.FlushAsync().ConfigureAwait(false);
                    p.StandardInput.Close();
                }
            }
            catch (Exception ex)
            {
                logger.LogDebug(ex, "Failed to send 'q' to ffmpeg stdin; will fall back to Kill if needed");
            }

            // 2) Give it a short window to exit gracefully
            var waitTask = p.WaitForExitAsync();
            _ = await Task.WhenAny(waitTask, Task.Delay(1500)).ConfigureAwait(false) == waitTask;

            // 3) If still alive, force kill (entire tree)
            if (!p.HasExited)
            {
                try
                {
                    p.Kill(entireProcessTree: true);
                }
                catch (Exception ex)
                {
                    logger.LogWarning(ex, "Error sending Kill to ffmpeg");
                }

                // Wait a bit for kill to complete
                try
                {
                    var killWait = p.WaitForExitAsync();
                    await Task.WhenAny(killWait, Task.Delay(3000)).ConfigureAwait(false);
                }
                catch { /* ignore */ }
            }
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error during FFmpeg process termination");
        }
        finally
        {
            // Cleanup & null out shared fields under lock
            await _processLock.WaitAsync().ConfigureAwait(false);
            try
            {
                _cancellationRegistration.Dispose();

                try { p.Dispose(); } catch { /* ignore */ }

                if (_ffmpegProcess == p)
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

        await Task.Delay(50).ConfigureAwait(false);
    }

    public void Dispose()
    {
        _ = DisposeAsyncCore(true);
        GC.SuppressFinalize(this);
    }

    private async Task DisposeAsyncCore(bool disposing)
    {
        if (_disposed) return;

        if (disposing)
        {
            await _processLock.WaitAsync().ConfigureAwait(false);
            try
            {
                _cancellationRegistration.Dispose();

                if (_ffmpegProcess is { HasExited: false })
                {
                    try
                    {
                        _ffmpegProcess.Kill(entireProcessTree: true);
                        var wait = _ffmpegProcess.WaitForExitAsync();
                        await Task.WhenAny(wait, Task.Delay(3000)).ConfigureAwait(false);
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
        _ = DisposeAsyncCore(false);
    }
}
