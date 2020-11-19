using System.IO;
using System.Threading.Tasks;

namespace Optima.DatasetLoader
{
    public class FileHelper
    {
        public static async Task CopyDirectory(string sourceDirectory, string destDirectory)
        {
            foreach (var dirName in Directory.GetDirectories(sourceDirectory, "*", new EnumerationOptions {RecurseSubdirectories = true}))
            {
                var relativeName = Path.GetRelativePath(sourceDirectory, dirName);
                Directory.CreateDirectory(Path.Combine(destDirectory, relativeName));
            }

            foreach (var filename in Directory.GetFiles(sourceDirectory, "*", new EnumerationOptions {RecurseSubdirectories = true}))
            {
                var relativeName = Path.GetRelativePath(sourceDirectory, filename);
                await using var sourceStream = File.Open(filename, FileMode.Create);
                await using var destinationStream = File.Create(Path.Combine(destDirectory, relativeName));
                await sourceStream.CopyToAsync(destinationStream);
            }
        }

        public static string[] ListFilesInDirectory(string directory) =>
            Directory.GetFiles(directory, "*", new EnumerationOptions {RecurseSubdirectories = true});
    }
}