using System;
using System.Threading.Tasks;
using Optima.DatasetLoader;
using Optima.Domain.DatasetDefinition;
using static Optima.ProtoGenerator.GeneratorHelper;

namespace Optima.TmpRunner
{
    class Program
    {
        static async Task Main(string[] args)
        {
            foreach (var pt in PostgresDatasetSchemaLoader.LoadSchema("pg")) 
                await PrintSchema(pt);

            Console.WriteLine("--- FILES ---");

            // await new KeyCloakConnect().AddUser("ccc"); 
            // Console.WriteLine(string.Join(", ", await new KeyCloakConnect().GetUsers()));
            await PrintSchema(FileSchemaLoader.InferCsvSchema(@"C:\Work\UMG\optima\FSharp\csv.csv"));
            await PrintSchema(FileSchemaLoader.InferJsonSchema(@"C:\Work\UMG\optima\FSharp\json.json"));
            
            //Console.WriteLine(string.Join(", ", FileSchemaLoader.ReadParquetSchema(@"C:\Work\UMG\hive\data\files\AvroPrimitiveInList.parquet")));
            await PrintSchema(FileSchemaLoader.ReadParquetSchema(@"C:\Work\UMG\Lin\LinNet\LinPlayground\db\parquet\test.parquet")); 
        }
        
        static async Task PrintSchema(PersistenceType pt)
        {
            var dl = await ToDatasetInfo(pt);

            //ProtoFileWriter.WriteMessageDescriptor();
            // Console.WriteLine(await ProtoGenerator.GeneratorHelper.GenerateProto(dl));

            await GenerateProbes(dl, @"C:\Work\UMG\Probs_Generated", modelProbePath: @"../Probes/DatasetProbe", prefix: "DatasetGen_");
        }
    }
}
