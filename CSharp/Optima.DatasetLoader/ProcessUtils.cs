using System;
using System.Diagnostics;
using System.Threading.Tasks;

namespace Optima.DatasetLoader
{
    public static class ProcessUtils
    {
        public static void Run(string fileName, string args)
        {
            var process = new Process
            {
                StartInfo = { FileName = fileName, Arguments = args },
                EnableRaisingEvents = true
            };
            process.WaitForExit(); // TODO: Add timeout and required exception handling
        }
        
        public static (Task<int>, Func<int, Task>) RunAsync(string folder, string fileName, string args)
        {
            var tcs = new TaskCompletionSource<int>();

            var process = new Process
            {
                StartInfo =
                {
                    WorkingDirectory = folder,
                    FileName = fileName, 
                    Arguments = args
                },
                EnableRaisingEvents = true
            };

            process.Exited += (sender, args) =>
            {
                tcs.SetResult(process.ExitCode);
                process.Dispose();
            };

            process.Start();

            return (tcs.Task, timeout => Task.Run(() =>
            {
                process.CloseMainWindow();
                process.WaitForExit(timeout);
                if (!process.HasExited) process.Kill(true);
            }));
        }
    }
}