using System;
using System.Threading.Tasks;
using LinNet;
using Optima.Domain.DatasetDefinition;

// using Prototypes;
// using FieldType = LinNet.FieldType;

namespace Optima.ProtoGenerator
{
    class Program
    {
        static async Task Main(string[] args)
        {
            var user = new User {Name = "John Smith", Uid = Guid.NewGuid().ToString()};
            var fields = new []
                {
                    new FieldDef {Name = "field1", Type = FieldType.String},
                    new FieldDef {Name = "field2", Type = FieldType.Int32}, 
                    new FieldDef {Name = "field3", Type = FieldType.Boolean}
                };

            var di = new DatasetInfo
                {
                    Name = "TestDataset",
                    Schema = new DatasetSchema { ProtoFile = await GeneratorHelper.GenerateDatasetProto("TestDataset", fields) },
                    PersistedTo = new PersistenceType
                    {
                        Fields = { fields },
                        Memory = new PersistenceType.Types.MemoryDatasetInfo()
                    }
                };
            
            Console.WriteLine(di.Schema.ProtoFile);
        }
    }
}
