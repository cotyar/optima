using System;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using Google.Protobuf;
using Optima.DatasetLoader;
using Optima.Domain.DatasetDefinition;
using Optima.Security;

namespace Optima.TmpRunner
{
    class Program
    {
        static async Task Main(string[] args)
        {
            foreach (var pt in PostgresDatasetSchemaLoader.LoadSchema("pg")) PrintSchema(pt);

            Console.WriteLine("--- FILES ---");

            // await new KeyCloakConnect().AddUser("ccc"); 
            // Console.WriteLine(string.Join(", ", await new KeyCloakConnect().GetUsers()));
            PrintSchema(FileSchemaLoader.InferCsvSchema(@"C:\Work\UMG\optima\FSharp\csv.csv"));
            PrintSchema(FileSchemaLoader.InferJsonSchema(@"C:\Work\UMG\optima\FSharp\json.json"));
            
            //Console.WriteLine(string.Join(", ", FileSchemaLoader.ReadParquetSchema(@"C:\Work\UMG\hive\data\files\AvroPrimitiveInList.parquet")));
            PrintSchema(FileSchemaLoader.ReadParquetSchema(@"C:\Work\UMG\Lin\LinNet\LinPlayground\db\parquet\test.parquet")); 
        }
        
        static void PrintSchema(PersistenceType pt) => 
            // Console.WriteLine(JsonFormatter.Default.Format(pt));
            ProtoFileWriter.WriteMessageDescriptor() pt.DescriptorProto.
    }
}
