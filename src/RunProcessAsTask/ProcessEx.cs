using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;

namespace RunProcessAsTask
{
    public static partial class ProcessEx
    {
        private static readonly TimeSpan _processExitGraceTime = TimeSpan.FromSeconds(30);
        
        /// <summary>
        /// Runs asynchronous process.
        /// </summary>
        /// <param name="processStartInfo">The <see cref="T:System.Diagnostics.ProcessStartInfo" /> that contains the information that is used to start the process, including the file name and any command-line arguments.</param>
        /// <param name="standardOutput">List that lines written to standard output by the process will be added to</param>
        /// <param name="standardError">List that lines written to standard error by the process will be added to</param>
        /// <param name="cancellationToken">The token to monitor for cancellation requests.</param>
        public static async Task<ProcessResults> RunAsync(ProcessStartInfo processStartInfo, List<string> standardOutput, List<string> standardError, CancellationToken cancellationToken)
        {
            // force some settings in the start info so we can capture the output
            processStartInfo.UseShellExecute = false;
            processStartInfo.RedirectStandardOutput = true;
            processStartInfo.RedirectStandardError = true;

            var tcs = new TaskCompletionSource<ProcessResults>();

            var process = new Process {
                StartInfo = processStartInfo,
                EnableRaisingEvents = true
            };

            var standardOutputResults = new TaskCompletionSource<string[]>();

            void OutputDataReceived(object sender, DataReceivedEventArgs args)
            {
                if (args.Data != null)
                    standardOutput.Add(args.Data);
                else
                    standardOutputResults.SetResult(standardOutput.ToArray());
            }

            process.OutputDataReceived += OutputDataReceived;

            var standardErrorResults = new TaskCompletionSource<string[]>();

            void ErrorDataReceived(object sender, DataReceivedEventArgs args)
            {
                if (args.Data != null)
                    standardError.Add(args.Data);
                else
                    standardErrorResults.SetResult(standardError.ToArray());
            }

            process.ErrorDataReceived += ErrorDataReceived;

            var processStartTime = new TaskCompletionSource<DateTime>();

            async void OnExited(object sender, EventArgs args)
            {
                // Since the Exited event can happen asynchronously to the output and error events, 
                // we await the task results for stdout/stderr to ensure they both closed.  We must await
                // the stdout/stderr tasks instead of just accessing the Result property due to behavior on MacOS.  
                // For more details, see the PR at https://github.com/jamesmanning/RunProcessAsTask/pull/16/
                tcs.TrySetResult(
                    new ProcessResults(
                        process, 
                        await processStartTime.Task.ConfigureAwait(false), 
                        await standardOutputResults.Task.ConfigureAwait(false), 
                        await standardErrorResults.Task.ConfigureAwait(false)
                    )
                );
            }

            process.Exited += OnExited;

            using (cancellationToken.Register(
                async () => {
                    try
                    {
                        if (!process.HasExited)
                        {
                            process.OutputDataReceived -= OutputDataReceived;
                            process.ErrorDataReceived -= ErrorDataReceived;
                            process.Exited -= OnExited;
                            process.Kill();
                            await Task.Delay(TimeSpan.FromSeconds(1));
                            if (!process.HasExited)
                            {
                                if (!process.WaitForExit(_processExitGraceTime.Milliseconds))
                                {
                                    throw new TimeoutException($"Timed out after {_processExitGraceTime.TotalSeconds:N2} seconds waiting for cancelled process to exit: {process}");
                                }
                            }
                        }
                        tcs.TrySetCanceled();
                    }
                    catch (InvalidOperationException)
                    {
                    }
                    catch (Exception exception)
                    {
                        tcs.SetException(new Exception($"Failed to kill process '{process.StartInfo.FileName}' ({process.Id}) upon cancellation", exception));
                    }
                })) {
                cancellationToken.ThrowIfCancellationRequested();

                var startTime = DateTime.Now;
                if (process.Start() == false)
                {
                    tcs.TrySetException(new InvalidOperationException("Failed to start process"));
                }
                else
                {
                    try
                    {
                        startTime = process.StartTime;
                    }
                    catch (Exception)
                    {
                        // best effort to try and get a more accurate start time, but if we fail to access StartTime
                        // (for instance, process has already existed), we still have a valid value to use.
                    }
                    processStartTime.SetResult(startTime);

                    process.BeginOutputReadLine();
                    process.BeginErrorReadLine();
                }

                return await tcs.Task.ConfigureAwait(false);
            }
        }
    }
}
