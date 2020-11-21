using System;
using System.IO;
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
            await PrintSchema(FileSchemaLoader.InferCsvSchema(@"C:\Work\UMG\optima\FSharp\csv.csv"));
            await PrintSchema(FileSchemaLoader.InferJsonSchema(@"C:\Work\UMG\optima\FSharp\json.json"));
            
            //Console.WriteLine(string.Join(", ", FileSchemaLoader.ReadParquetSchema(@"C:\Work\UMG\hive\data\files\AvroPrimitiveInList.parquet")));
            await PrintSchema(FileSchemaLoader.ReadParquetSchema(@"C:\Work\UMG\Lin\LinNet\LinPlayground\db\parquet\test.parquet")); 
        }
        
        static async Task PrintSchema(PersistenceType pt)
        {
            var name = pt.PersistenceCase switch
            {
                PersistenceType.PersistenceOneofCase.File => Path.GetFileNameWithoutExtension(pt.File.Path),
                PersistenceType.PersistenceOneofCase.Db => $"{pt.Db.DbProvider.Postgres.TableCatalog}_{pt.Db.DbProvider.Postgres.SchemaName}_{pt.Db.DbProvider.Postgres.TableName}",
                // PersistenceType.PersistenceOneofCase.None => ,
                // PersistenceType.PersistenceOneofCase.Hive => ,
                // PersistenceType.PersistenceOneofCase.Memory => ,
                _ => $"Mds{Guid.NewGuid():N}"
            };
            
            var dl = new DatasetInfo
            {
                Name = name,
                PersistedTo = pt
            };
            
            //ProtoFileWriter.WriteMessageDescriptor();
            Console.WriteLine(await ProtoGenerator.GeneratorHelper.GenerateProto(dl));

            await ProtoGenerator.GeneratorHelper.GenerateCalcProbe(dl, @"C:\Work\UMG\Probs_Generated");
        }
    }
}
