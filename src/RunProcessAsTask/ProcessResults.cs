using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;

namespace RunProcessAsTask
{
    public sealed class ProcessResults : IDisposable
    {
        public ProcessResults(Process process, string[] standardOutput, string[] standardError)
        {
            Process = process;
            ExitCode = process.ExitCode;
            RunTime = process.ExitTime - process.StartTime;
            StandardOutput = standardOutput;
            StandardError = standardError;
        }

        public Process Process { get; }
        public int ExitCode { get; }
        public TimeSpan RunTime { get; }
        public string[] StandardOutput { get; }
        public string[] StandardError { get; }
        public void Dispose() { Process.Dispose(); }
    }
}
