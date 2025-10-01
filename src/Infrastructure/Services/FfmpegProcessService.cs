using System.Diagnostics;
using Application.Interfaces.Services;
using Microsoft.Extensions.Logging;

namespace Infrastructure.Services;

public class FfmpegProcessService(ILogger<FfmpegProcessService> logger)
    : INativePlaceMusicProcessorService, IDisposable
{
    private Process? _ffmpegProcess;
    private bool _disposed;
    private readonly Lock _processLock = new();
    
    public event Func<Task>? OnPlaySongCompleted;
    public event Func<Task>? OnProcessStart;
    public event Func<Task>? OnForbiddenUrlRequest;

    public async Task<Process> CreateStreamAsync(string audioUrl, CancellationToken cancellationToken)
    {
        Process process;
        
        lock (_processLock)
        {
            DisposeCurrentProcessUnsafe();

            process = new Process
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

            _ffmpegProcess = process;
        }

        // Set up logging
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

        process.Exited += async (sender, _) =>
        {
            Process? exitedProcess = sender as Process;
            if (exitedProcess == null) return;

            try
            {
                var exitCode = exitedProcess.ExitCode;
        
                if (exitCode == 0)
                {
                    // Handle normal completion
                    logger.LogInformation("FFmpeg stream completed successfully (PID: {ProcessId})", exitedProcess.Id);
                    await (OnPlaySongCompleted?.Invoke() ?? Task.CompletedTask);
                }
                else
                {
                    // Handle error - maybe retry or skip to next track
                    logger.LogError("FFmpeg process exited with error code: {ExitCode} (PID: {ProcessId})", 
                        exitCode, exitedProcess.Id);
                    await Task.Delay(500); // Brief delay to ensure logs are flushed
                    await (OnForbiddenUrlRequest?.Invoke() ?? Task.CompletedTask);
                }
            }
            catch (InvalidOperationException ex)
            {
                // Process might have been disposed already
                logger.LogDebug(ex, "Could not read exit code - process may have been disposed");
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Error in FFmpeg Exited event handler");
            }
        };

        // Handle cancellation
        cancellationToken.Register(() =>
        {
            logger.LogInformation("Cancellation requested for FFmpeg process");
            DisposeCurrentProcess();
        });

        // Start the process
        try
        {
            if (!process.Start())
            {
                lock (_processLock)
                {
                    if (_ffmpegProcess == process)
                    {
                        _ffmpegProcess = null;
                    }
                }
                process.Dispose();
                throw new InvalidOperationException("Failed to start ffmpeg process.");
            }

            process.BeginErrorReadLine();
            
            logger.LogInformation("FFmpeg process started for URL: {AudioUrl} (PID: {ProcessId})", 
                audioUrl, process.Id);
            
            await (OnProcessStart?.Invoke() ?? Task.CompletedTask);
            
            return process;
        }
        catch
        {
            lock (_processLock)
            {
                if (_ffmpegProcess == process)
                {
                    _ffmpegProcess = null;
                }
            }
            process.Dispose();
            throw;
        }
    }

    private void DisposeCurrentProcess()
    {
        lock (_processLock)
        {
            DisposeCurrentProcessUnsafe();
        }
    }

    private void DisposeCurrentProcessUnsafe()
    {
        if (_ffmpegProcess == null) return;

        var process = _ffmpegProcess;
        _ffmpegProcess = null;

        // Check if already exited before trying to terminate
        try
        {
            if (process.HasExited)
            {
                logger.LogDebug("FFmpeg process already exited (PID: {ProcessId})", process.Id);
                process.Dispose();
                return;
            }
        }
        catch (InvalidOperationException)
        {
            // Process was never started or already disposed
            process.Dispose();
            return;
        }

        logger.LogInformation("Terminating FFmpeg process (PID: {ProcessId})...", process.Id);

        try
        {
            // Try graceful shutdown first
            try
            {
                if (!process.HasExited)
                {
                    process.StandardInput?.WriteLine("q");
                    process.StandardInput?.Close();
                }
            }
            catch (InvalidOperationException)
            {
                // Process might have exited between checks
            }

            // Wait briefly for graceful exit
            if (!process.WaitForExit(1500))
            {
                // Force kill if still running
                try
                {
                    if (!process.HasExited)
                    {
                        process.Kill(entireProcessTree: true);
                        process.WaitForExit(3000);
                        logger.LogInformation("FFmpeg process killed (PID: {ProcessId})", process.Id);
                    }
                }
                catch (InvalidOperationException)
                {
                    // Process exited between check and kill
                    logger.LogDebug("Process exited before kill command");
                }
            }
            else
            {
                logger.LogInformation("FFmpeg process terminated gracefully (PID: {ProcessId})", process.Id);
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

    public void Dispose()
    {
        if (_disposed) return;
        
        lock (_processLock)
        {
            if (_disposed) return;
            _disposed = true;
            DisposeCurrentProcessUnsafe();
        }
    }
}