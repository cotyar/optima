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
        
        public static (Task<int>, Action) RunAsync(string fileName, string args)
        {
            var tcs = new TaskCompletionSource<int>();

            var process = new Process
            {
                StartInfo = { FileName = fileName, Arguments = args },
                EnableRaisingEvents = true
            };

            process.Exited += (sender, args) =>
            {
                tcs.SetResult(process.ExitCode);
                process.Dispose();
            };

            process.Start();

            return (tcs.Task, () => process.CloseMainWindow());
        }
    }
}