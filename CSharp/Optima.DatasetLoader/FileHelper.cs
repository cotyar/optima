using System.IO;
using System.Threading.Tasks;

namespace Optima.DatasetLoader
{
    public class FileHelper
    {
        public static string[] ListFilesInDirectory(string directory) =>
            Directory.GetFiles(directory, "*", new EnumerationOptions {RecurseSubdirectories = true});
    }
}