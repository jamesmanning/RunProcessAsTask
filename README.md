RunProcessAsTask
================

Simple wrapper around [System.Diagnostics.Process](http://msdn.microsoft.com/en-us/library/system.diagnostics.process.aspx) to expose it as a [System.Threading.Tasks.Task](http://msdn.microsoft.com/en-us/library/system.threading.tasks.task.aspx)<[ProcessResults](https://github.com/jamesmanning/RunProcessAsTask/blob/master/src/RunProcessAsTask/ProcessResults.cs)>

Includes support for cancellation, timeout (via cancellation), and exposes the standard output, standard error, and exit code of the process.
