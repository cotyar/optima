using System;
using Optima.DatasetLoader;

namespace Optima.TmpRunner
{
    class Program
    {
        static void Main(string[] args)
        {
            Console.WriteLine("Hello World!");
            PostgresDatasetSchemaLoader.LoadSchema(); 
        }
    }
}
