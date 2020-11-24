using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using LinNet;
using Optima.Domain.DatasetDefinition;
using Optima.ProtoGenerator;

// using Prototypes;
// using FieldType = LinNet.FieldType;

namespace LinCalcServer
{
    class Program
    {
        static async Task Main(string[] args)
        {
            var user = new User {Name = "John Smith", Uid = Guid.NewGuid().ToString()};
            
            var dl = new DatasetInfo
            {
                
                Name = "TestDataset",
                PersistedTo = new PersistenceType
                {
                    Fields = { new FieldDef { Name = "field1", Type = FieldType.String}, new FieldDef { Name = "field2", Type = FieldType.Int32 }, new FieldDef { Name = "field3", Type = FieldType.Boolean } },
                    Memory = new PersistenceType.Types.MemoryDatasetInfo()
                }
                
            };
            
            Console.WriteLine(await GeneratorHelper.GenerateProto(dl, "datasetProto"));
        }
    }
}
