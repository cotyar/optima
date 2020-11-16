using System;
using System.Linq;
using System.Threading.Tasks;
using Optima.DatasetLoader;
using Optima.Security;

namespace Optima.TmpRunner
{
    class Program
    {
        static async Task Main(string[] args)
        {
            Console.WriteLine("Hello World!");
            PostgresDatasetSchemaLoader.LoadSchema();

            // await new KeyCloakConnect().AddUser("ccc"); 
            Console.WriteLine(string.Join(", ", await new KeyCloakConnect().GetUsers()));
            Console.WriteLine(string.Join(", ", CsvSchemaLoader.InferCsvSchema()));
            Console.WriteLine(string.Join(", ", CsvSchemaLoader.InferJsonSchema()));
        }
    }
}
