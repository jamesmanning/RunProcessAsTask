using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace RunProcessAsTask.Tests
{
    public static class ProcessExTests
    {
        [TestClass]
        public class RunAsyncTests
        {
            [TestMethod]
            public void SomeTestMethod()
            {
                // Arrange
                int expectedExitCode = 123;
                int expectedStandardOutputLineCount = 5;
                int expectedStandardErrorLineCount = 3;
                var pathToConsoleApp = typeof(DummyConsoleApp.Program).Assembly.Location;
                var arguments = String.Format("{0} {1} {2}", expectedExitCode, expectedStandardOutputLineCount, expectedStandardErrorLineCount);

                // Act
                var task = ProcessEx.RunAsync(pathToConsoleApp, arguments);

                // Assert
                Assert.IsNotNull(task);
                var results = task.Result;
                Assert.IsNotNull(results);
                Assert.IsNotNull(results.Process);
                Assert.IsTrue(results.Process.HasExited);
                Assert.IsNotNull(results.StandardOutput);
                Assert.IsNotNull(results.StandardError);

                Assert.AreEqual(expectedExitCode, results.Process.ExitCode);
                Assert.AreEqual(expectedStandardOutputLineCount, results.StandardOutput.Count());
                Assert.AreEqual(expectedStandardErrorLineCount, results.StandardError.Count());
            }
        }
    }
}
