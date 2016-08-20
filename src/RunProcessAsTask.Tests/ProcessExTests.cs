using System;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Xunit;

namespace RunProcessAsTask.Tests
{
    public static class ProcessExTests
    {
        public class RunAsyncTests
        {
            [Fact]
            public void WhenProcessRunsNormally_ReturnsExpectedResults()
            {
                // Arrange
                const int expectedExitCode = 123;
                const int millisecondsToSleep = 5 * 1000; // set a minimum run time so we can validate it as part of the output
                const int expectedStandardOutputLineCount = 5;
                const int expectedStandardErrorLineCount = 3;
                var pathToConsoleApp = typeof(DummyConsoleApp.Program).Assembly.Location;
                var arguments = String.Join(" ", expectedExitCode, millisecondsToSleep, expectedStandardOutputLineCount, expectedStandardErrorLineCount);

                // Act
                var task = ProcessEx.RunAsync(pathToConsoleApp, arguments);

                // Assert
                Assert.NotNull(task);
                var results = task.Result;
                Assert.NotNull(results);
                Assert.Equal(TaskStatus.RanToCompletion, task.Status);
                Assert.NotNull(results.Process);
                Assert.True(results.Process.HasExited);
                Assert.NotNull(results.StandardOutput);
                Assert.NotNull(results.StandardError);

                Assert.Equal(expectedExitCode, results.ExitCode);
                Assert.Equal(expectedExitCode, results.Process.ExitCode);
                Assert.True(results.RunTime.TotalMilliseconds >= millisecondsToSleep);
                Assert.Equal(expectedStandardOutputLineCount, results.StandardOutput.Length);
                Assert.Equal(expectedStandardErrorLineCount, results.StandardError.Length);

                var expectedStandardOutput = new[]
                {
                    "Standard output line #1",
                    "Standard output line #2",
                    "Standard output line #3",
                    "Standard output line #4",
                    "Standard output line #5",
                };
                var expectedStandardError = new[]
                {
                    "Standard error line #1",
                    "Standard error line #2",
                    "Standard error line #3",
                };
                Assert.Equal(expectedStandardOutput, results.StandardOutput);
                Assert.Equal(expectedStandardError, results.StandardError);
            }

            [Fact(Skip = "disable while getting code coverage working for faster testing")]
            public void RunLotsOfOutputForFiveMinutes()
            {
                // when this problem manifested with the older code, it would normally 
                // trigger in this test within 5 to 10 seconds, so if it can run for 
                // 5 minutes and not cause the output-truncation issue, we are probably fine
                var fiveMinutes = TimeSpan.FromMinutes(5);
                for (var stopwatch = Stopwatch.StartNew(); stopwatch.Elapsed < fiveMinutes; )
                {
                    Parallel.ForEach(Enumerable.Range(1, 100), index => WhenProcessReturnsLotsOfOutput_AllOutputCapturedCorrectly());
                }
            }

            private readonly Random _random = new Random();

            [Fact]
            public void WhenProcessReturnsLotsOfOutput_AllOutputCapturedCorrectly()
            {
                // Arrange
                const int expectedExitCode = 123;
                const int millisecondsToSleep = 0; // We want the process to exit right after printing the lines, so no wait time
                int expectedStandardOutputLineCount = _random.Next(1000, 100 * 1000);
                int expectedStandardErrorLineCount = _random.Next(1000, 100 * 1000);
                var pathToConsoleApp = typeof(DummyConsoleApp.Program).Assembly.Location;
                var arguments = String.Join(" ", expectedExitCode, millisecondsToSleep, expectedStandardOutputLineCount, expectedStandardErrorLineCount);
                // force no window since there's no value in showing it during a test run
                var processStartInfo = new ProcessStartInfo(pathToConsoleApp, arguments)
                {
                    CreateNoWindow = true,
                    WindowStyle = ProcessWindowStyle.Hidden,
                };
                // Act
                var task = ProcessEx.RunAsync(processStartInfo);

                // Assert
                Assert.NotNull(task);
                var results = task.Result;
                Assert.NotNull(results);
                Assert.Equal(TaskStatus.RanToCompletion, task.Status);
                Assert.NotNull(results.Process);
                Assert.True(results.Process.HasExited);
                Assert.NotNull(results.StandardOutput);
                Assert.NotNull(results.StandardError);

                Assert.Equal(expectedExitCode, results.ExitCode);
                Assert.Equal(expectedExitCode, results.Process.ExitCode);
                Assert.True(results.RunTime.TotalMilliseconds >= millisecondsToSleep);
                Assert.Equal(expectedStandardOutputLineCount, results.StandardOutput.Length);
                Assert.Equal(expectedStandardErrorLineCount, results.StandardError.Length);

                var expectedStandardOutput = Enumerable.Range(1, expectedStandardOutputLineCount)
                    .Select(x => "Standard output line #" + x)
                    .ToArray();
                var expectedStandardError = Enumerable.Range(1, expectedStandardErrorLineCount)
                    .Select(x => "Standard error line #" + x)
                    .ToArray();
                Assert.Equal(expectedStandardOutput, results.StandardOutput);
                Assert.Equal(expectedStandardError, results.StandardError);
            }

            [Fact]
            public void WhenProcessTimesOut_TaskIsCanceled()
            {
                // Arrange
                const int expectedExitCode = 123;
                const int millisecondsForTimeout = 3 * 1000;
                const int millisecondsToSleep = 5 * 1000;
                const int expectedStandardOutputLineCount = 5;
                const int expectedStandardErrorLineCount = 3;

                // Act
                var pathToConsoleApp = typeof(DummyConsoleApp.Program).Assembly.Location;
                var arguments = String.Join(" ", expectedExitCode, millisecondsToSleep, expectedStandardOutputLineCount, expectedStandardErrorLineCount);
                var startInfo = new ProcessStartInfo(pathToConsoleApp, arguments);
                var cancellationToken = new CancellationTokenSource(millisecondsForTimeout).Token;
                var task = ProcessEx.RunAsync(startInfo, cancellationToken);
                Assert.NotNull(task);

                // Assert
                var aggregateException = Assert.Throws<AggregateException>(() => task.Result);
                Assert.Equal(1, aggregateException.InnerExceptions.Count);
                var innerException = aggregateException.InnerExceptions.Single();
                var canceledException = Assert.IsType<TaskCanceledException>(innerException);
                Assert.NotNull(canceledException);
                Assert.True(cancellationToken.IsCancellationRequested);
                Assert.Equal(TaskStatus.Canceled, task.Status);
            }
        }
    }
}
