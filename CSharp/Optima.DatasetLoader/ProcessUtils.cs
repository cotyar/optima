using System.Diagnostics;

namespace Optima.DatasetLoader
{
    public static class ProcessUtils
    {
        public static void Run(string filePath, string args)
        {
            var startInfo = new ProcessStartInfo { FileName = filePath, Arguments = args };
            var process = Process.Start(startInfo);
            process.WaitForExit(); // TODO: Add timeout and required exception handling
        }
    }
}