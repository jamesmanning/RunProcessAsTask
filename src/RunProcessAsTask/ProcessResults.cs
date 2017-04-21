using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;

namespace RunProcessAsTask
{
        public class ProcessResults : IDisposable
        {
                private readonly Process _process;
                private readonly string[] _standardOutput;
                private readonly string[] _standardError;
                private readonly int _exitCode;
                private readonly TimeSpan _runTime;

                public ProcessResults(Process process, string[] standardOutput, string[] standardError)
                {
                        _process = process;
                        _exitCode = process.ExitCode;
                        _runTime = process.ExitTime - process.StartTime;
                        _standardOutput = standardOutput;
                        _standardError = standardError;
                }

                public Process Process
                {
                        get { return _process; }
                }

                public int ExitCode
                {
                        get { return _exitCode; }
                }

                public TimeSpan RunTime
                {
                        get { return _runTime; }
                }

                public string[] StandardOutput
                {
                        get { return _standardOutput; }
                }

                public string[] StandardError
                {
                        get { return _standardError; }
                }

                public void Dispose()
                {
                        _process.Dispose();
                }
        }
}
