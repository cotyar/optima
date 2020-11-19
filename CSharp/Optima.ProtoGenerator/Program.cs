using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using LinNet;
using Optima.Domain.DatasetDefinition;
// using Prototypes;
using RazorEngine;
using RazorEngine.Configuration;
using RazorEngine.Templating;
using RazorEngine.Text;
// using FieldType = LinNet.FieldType;

namespace LinCalcServer
{
    class Program
    {
        static async Task Main(string[] args)
        {
            var template = await File.ReadAllTextAsync(@"Templates/proto.mustache");
            
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
            
            Console.WriteLine(await FileHelper.GenerateProto(template, dl));
            
            // FileHelper.CopyDirectory(@"C:\Work\UMG\Lin\CsGened", @"C:\Work\UMG\Lin\CsGened1").Wait();

            // var config = new TemplateServiceConfiguration
            // {
            //     EncodedStringFactory = new RawStringFactory()
            // };
            //
            // var service = RazorEngineService.Create(config);
            // Engine.Razor = service;
            //
            // var user = new User {Name = "John Smith", Uid = Guid.NewGuid().ToString()};
            //
            // var dl = new DatasetLineage
            // {
            //     Dataset = new DatasetInfo { Name = "TestDataset" },
            //     Fields = { new FieldDef { Name = "field1", Type = FieldType.String}, new FieldDef { Name = "field2", Type = FieldType.Int32 } } 
            // };
            // new Program().GenerateProto(dl);
        }

        
    }
}
