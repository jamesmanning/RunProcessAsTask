using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading.Tasks;

namespace RunProcessAsTask
{
    public static partial class ProcessEx
    {
        // TODO: add support for cancellation and timeout
        public static Task<ProcessResults> RunAsync(ProcessStartInfo processStartInfo)
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

            process.OutputDataReceived += (sender, args) =>
            {
                if (args.Data != null)
                {
                    standardOutput.Add(args.Data);
                }
            };

            process.ErrorDataReceived += (sender, args) =>
            {
                if (args.Data != null)
                {
                    standardError.Add(args.Data);
                }
            };

            process.Exited += (sender, args) => tcs.TrySetResult(new ProcessResults(process, standardOutput, standardError));

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
