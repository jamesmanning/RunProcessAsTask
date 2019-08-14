using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;

namespace RunProcessAsTask
{
    public static partial class ProcessEx
    {
        public static Task<ProcessResults> RunAsync(ProcessStartInfo processStartInfo, List<string> standardOutput, List<string> standardError, CancellationToken cancellationToken)
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
            process.OutputDataReceived += (sender, args) => {
                if (args.Data != null)
                    standardOutput.Add(args.Data);
                else
                    standardOutputResults.SetResult(standardOutput.ToArray());
            };

            var standardErrorResults = new TaskCompletionSource<string[]>();
            process.ErrorDataReceived += (sender, args) => {
                if (args.Data != null)
                    standardError.Add(args.Data);
                else
                    standardErrorResults.SetResult(standardError.ToArray());
            };

            var processStartTime = new TaskCompletionSource<DateTime>();

            process.Exited += async (sender, args) => {
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
            };

            using (cancellationToken.Register(
                () => {
                    tcs.TrySetCanceled();
                    try {
                        if (!process.HasExited)
                            process.Kill();
                    } catch (InvalidOperationException) { }
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

                return tcs.Task;
            }
        }
    }
}
