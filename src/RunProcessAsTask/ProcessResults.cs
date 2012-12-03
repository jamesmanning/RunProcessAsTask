using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;

namespace RunProcessAsTask
{
    public class ProcessResults
    {
        private readonly Process _process;
        private readonly IEnumerable<string> _standardOutput;
        private readonly IEnumerable<string> _standardError;

        public ProcessResults(Process process, IEnumerable<string> standardOutput, IEnumerable<string> standardError)
        {
            _process = process;
            _standardOutput = standardOutput;
            _standardError = standardError;
        }

        public Process Process
        {
            get { return _process; }
        }

        public IEnumerable<string> StandardOutput
        {
            get { return _standardOutput; }
        }

        public IEnumerable<string> StandardError
        {
            get { return _standardError; }
        }
    }
}
