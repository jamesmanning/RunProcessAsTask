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
        {
            return RunAsync(processStartInfo, CancellationToken.None);
        }

        public static Task<ProcessResults> RunAsync(ProcessStartInfo processStartInfo, CancellationToken cancellationToken)
        {
            // force some settings in the start info so we can capture the output
            processStartInfo.UseShellExecute = false;
            processStartInfo.RedirectStandardOutput = true;
            processStartInfo.RedirectStandardError = true;

            var tcs = new TaskCompletionSource<ProcessResults>();

            var standardOutput = new List<string>();
            var standardError = new List<string>();

            var process = new Process
            {
                StartInfo = processStartInfo,
                EnableRaisingEvents = true
            };

            var outputDataClosed = new ManualResetEventSlim();
            process.OutputDataReceived += (sender, args) =>
            {
                if (args.Data != null)
                {
                    standardOutput.Add(args.Data);
                }
                else
                {
                    outputDataClosed.Set();
                }
            };

            var errorDataClosed = new ManualResetEventSlim();
            process.ErrorDataReceived += (sender, args) =>
            {
                if (args.Data != null)
                {
                    standardError.Add(args.Data);
                }
                else
                {
                    errorDataClosed.Set();
                }
            };

            process.Exited += (sender, args) =>
            {
                // Since the Exited event can happen asynchronously to the output and error events, 
                // we wait on both of output and error being signaled as closed by our handlers to 
                // ensure we don't lose any of the data being written to them
                outputDataClosed.Wait(cancellationToken);
                errorDataClosed.Wait(cancellationToken);

                // now that we know both output and error have closed, we can safely proceed with serializing the arrays
                tcs.TrySetResult(new ProcessResults(process, standardOutput.ToArray(), standardError.ToArray()));
            };

            cancellationToken.Register(() =>
            {
                tcs.TrySetCanceled();
                process.Kill();
            });

            cancellationToken.ThrowIfCancellationRequested();

            if (process.Start() == false)
            {
                tcs.TrySetException(new InvalidOperationException("Failed to start process"));
            }

            process.BeginOutputReadLine();
            process.BeginErrorReadLine();

            return tcs.Task;
        }
    }
}
