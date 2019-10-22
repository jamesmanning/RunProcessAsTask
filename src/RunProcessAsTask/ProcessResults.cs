﻿using System;
using System.Diagnostics;

namespace RunProcessAsTask
{
    public sealed class ProcessResults : IDisposable
    {
        /// <summary>
        /// Contains information about terminated process.
        /// </summary>
        public ProcessResults(Process process, DateTime processStartTime, string[] standardOutput, string[] standardError)
        {
            Process = process;
            ExitCode = process.ExitCode;
            RunTime = process.ExitTime - processStartTime;
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
