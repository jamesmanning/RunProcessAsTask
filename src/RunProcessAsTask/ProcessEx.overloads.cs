using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;

namespace RunProcessAsTask
{
    // these overloads match the ones in Process.Start to make it a simpler transition for callers
    // see http://msdn.microsoft.com/en-us/library/system.diagnostics.process.start.aspx
    public partial class ProcessEx
    {
        public static Task<ProcessResults> RunAsync(string fileName)
            => RunAsync(new ProcessStartInfo(fileName));

        public static Task<ProcessResults> RunAsync(string fileName, string arguments)
            => RunAsync(new ProcessStartInfo(fileName, arguments));
    }
}
