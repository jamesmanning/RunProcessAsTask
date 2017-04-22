using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;

namespace DummyConsoleApp
{
    public class Program
    {
        static void Main(string[] args)
        {
            var exitCodeToReturn = int.Parse(args[0]);
            var millisecondsToSleep = int.Parse(args[1]);
            var linesOfStandardOutput = int.Parse(args[2]);
            var linesOfStandardError = int.Parse(args[3]);

            Thread.Sleep(millisecondsToSleep);

            for (var i = 0; i < linesOfStandardOutput; i++)
                Console.WriteLine("Standard output line #{0}", i + 1);

            for (var i = 0; i < linesOfStandardError; i++)
                Console.Error.WriteLine("Standard error line #{0}", i + 1);

            Environment.Exit(exitCodeToReturn);
        }
    }
}
