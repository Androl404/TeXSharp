using System;
using System.Diagnostics;
using System.Text;
using System.Threading.Tasks;

/// <summary>
/// Provides asynchronous execution functionality for shell commands.
/// </summary>
public static class ProcessAsyncHelper
{
    /// <summary>
    /// Executes a shell command asynchronously with a specified timeout.
    /// Captures both standard output and standard error.
    /// </summary>
    /// <param name="command">The command or executable to run.</param>
    /// <param name="arguments">Arguments passed to the command.</param>
    /// <param name="timeout">Timeout in milliseconds to wait for command completion.</param>
    /// <returns>
    /// A task that resolves to a <see cref="ProcessResult"/> indicating
    /// success, exit code, and any output or error messages.
    /// </returns>
    public static async Task<ProcessResult> ExecuteShellCommand(string command, string arguments, int timeout)
    {
        var result = new ProcessResult();

        using (var process = new Process())
        {
            // Set up process start info
            process.StartInfo.FileName = command;
            process.StartInfo.Arguments = arguments;
            process.StartInfo.UseShellExecute = false;
            process.StartInfo.RedirectStandardInput = true;
            process.StartInfo.RedirectStandardOutput = true;
            process.StartInfo.RedirectStandardError = true;
            process.StartInfo.CreateNoWindow = true;

            var outputBuilder = new StringBuilder();
            var outputCloseEvent = new TaskCompletionSource<bool>();

            // Handle standard output stream
            process.OutputDataReceived += (s, e) =>
            {
                if (e.Data == null)
                {
                    // Output stream closed
                    outputCloseEvent.SetResult(true);
                }
                else
                {
                    outputBuilder.AppendLine(e.Data);
                }
            };

            var errorBuilder = new StringBuilder();
            var errorCloseEvent = new TaskCompletionSource<bool>();

            // Handle standard error stream
            process.ErrorDataReceived += (s, e) =>
            {
                if (e.Data == null)
                {
                    // Error stream closed
                    errorCloseEvent.SetResult(true);
                }
                else
                {
                    errorBuilder.AppendLine(e.Data);
                }
            };

            bool isStarted;

            try
            {
                // Attempt to start the process
                isStarted = process.Start();
            }
            catch (Exception error)
            {
                // Process start failed (e.g. executable not found)
                result.Completed = true;
                result.ExitCode = -1;
                result.Output = error.Message;
                isStarted = false;
            }

            if (isStarted)
            {
                // Begin reading output and error asynchronously
                process.BeginOutputReadLine();
                process.BeginErrorReadLine();

                // Task that waits for the process to exit with timeout
                var waitForExit = WaitForExitAsync(process, timeout);

                // Combine process exit task and output/error stream completion tasks
                var processTask = Task.WhenAll(waitForExit, outputCloseEvent.Task, errorCloseEvent.Task);

                // Wait for process task to finish or timeout
                if (await Task.WhenAny(Task.Delay(timeout), processTask) == processTask && waitForExit.Result)
                {
                    result.Completed = true;
                    result.ExitCode = process.ExitCode;
                    result.Output = $"{outputBuilder}{errorBuilder}";

                    // If error occurred, append output and error messages
                    if (process.ExitCode != 0)
                    {
                        result.Output = $"{outputBuilder}{errorBuilder}";
                    }
                }
                else
                {
                    try
                    {
                        // Kill the process if it's hanging
                        process.Kill();
                    }
                    catch
                    {
                        // Ignored: Process may have already exited or cannot be killed
                    }
                }
            }
        }

        return result;
    }

    /// <summary>
    /// Waits asynchronously for the process to exit within the specified timeout.
    /// </summary>
    /// <param name="process">The process to wait for.</param>
    /// <param name="timeout">Timeout in milliseconds.</param>
    /// <returns>A task that resolves to true if the process exited, otherwise false.</returns>
    private static Task<bool> WaitForExitAsync(Process process, int timeout) { return Task.Run(() => process.WaitForExit(timeout)); }

    /// <summary>
    /// Represents the result of a process execution.
    /// </summary>
    public struct ProcessResult
    {
        /// <value>
        /// Indicates whether the process completed (including after being killed).
        /// </value>
        public bool Completed;

        /// <value>
        /// Exit code of the process, if completed. It might be null.
        /// </value>
        public int? ExitCode;

        /// <value>
        /// Combined standard output and error output.
        /// </value>
        public string Output;
    }
}
