using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;

namespace RunProcessAsTask
{
    public static partial class ProcessEx
    {
        public static Task<ProcessResults> RunAsync(ProcessStartInfo processStartInfo) 
            => RunAsync(processStartInfo, CancellationToken.None);

        public static async Task<ProcessResults> RunAsync(ProcessStartInfo processStartInfo, CancellationToken cancellationToken)
        {
            // force some settings in the start info so we can capture the output
            processStartInfo.UseShellExecute = false;
            processStartInfo.RedirectStandardOutput = true;
            processStartInfo.RedirectStandardError = true;

            var tcs = new TaskCompletionSource<ProcessResults>();

            var standardOutput = new List<string>();
            var standardError = new List<string>();

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
                tcs.TrySetResult(new ProcessResults(process, await processStartTime.Task, await standardOutputResults.Task, await standardErrorResults.Task));
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

                if (process.Start() == false)
                    tcs.TrySetException(new InvalidOperationException("Failed to start process"));
                processStartTime.SetResult(process.StartTime);

                process.BeginOutputReadLine();
                process.BeginErrorReadLine();

                return await tcs.Task;
            }
        }
    }
}
