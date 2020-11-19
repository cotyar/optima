using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using LinNet;
using RazorEngine;
using RazorEngine.Templating;
using Stubble.Core.Builders;

// using LinNet;

namespace LinCalcServer
{
    public static class FileHelper
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
        
        public static async Task<string> GenerateProto(string template, DatasetLineage dataset)
        {
            // var template = File.ReadAllText(@"Templates/proto.razor");

            var fields = string.Join("\n    ", FieldDefsToStrings(dataset.Fields));
 
            var model = new
                {
                    Package = "lin.generated.test1",
                    RequestName = dataset.Dataset.Name,
                    RequestNameLin = dataset.Dataset.Name + "WithLineage",
                    ResponseName = dataset.Dataset.Name + "Resp",
                    ResponseNameLin = dataset.Dataset.Name + "Resp" + "WithLineage",
                    RequestFields = fields,
                    ResponseFields = fields,
                    CsNamespace = "LinNet"
                };
                    
            var result = await (new StubbleBuilder().Build()).RenderAsync(template, model);

            // Console.WriteLine(result);
            // File.WriteAllText("GeneratedProtos/generated.proto", result);

            return result;
        }
        
        public static Task WriteFile(string fileName, string content) => 
            File.WriteAllTextAsync(fileName, content);

        private static string[] FieldDefsToStrings(IEnumerable<FieldDef> fields) => 
            fields.
                Select((f, i) => new
                {
                    f.Name, 
                    Type = f.Type switch
                    {
                        FieldType.String => "string",
                        FieldType.Int8 => "uint32",
                        FieldType.Int16 => "int32",
                        FieldType.Int32 => "int32",
                        FieldType.Int64 => "int64",
                        FieldType.Float32 => "float",
                        FieldType.Float64 => "double",
                        FieldType.Decimal => "float", 
                        FieldType.Boolean => "bool",
                        _ => "string"
                    },
                    Index = i + 1
                }).
                Select(f => $"{f.Type} {f.Name} = {f.Index};"). 
                ToArray();
    }
}