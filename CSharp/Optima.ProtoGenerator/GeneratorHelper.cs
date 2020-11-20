using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Optima.Domain.DatasetDefinition;
using Stubble.Core.Builders;

namespace Optima.ProtoGenerator
{
    public static class FileHelper
    {
        public static async Task CopyDirectory(string sourceDirectory, string destDirectory, string[] ignorePatterns = null)
        {
            ignorePatterns ??= new string[0];
            
            foreach (var dirName in Directory.GetDirectories(sourceDirectory, "*", new EnumerationOptions {RecurseSubdirectories = true}))
            {
                if (ignorePatterns.Any(pattern => Regex.IsMatch(dirName, pattern, RegexOptions.CultureInvariant | RegexOptions.IgnorePatternWhitespace | RegexOptions.IgnoreCase))) continue;
                
                var relativeName = Path.GetRelativePath(sourceDirectory, dirName);
                Directory.CreateDirectory(Path.Combine(destDirectory, relativeName));
            }

            foreach (var fileName in Directory.GetFiles(sourceDirectory, "*", new EnumerationOptions {RecurseSubdirectories = true}))
            {
                if (ignorePatterns.Any(pattern => Regex.IsMatch(fileName, pattern, RegexOptions.CultureInvariant | RegexOptions.IgnorePatternWhitespace | RegexOptions.IgnoreCase))) continue;
                
                Console.WriteLine($"fn - '{fileName}'");
                Console.WriteLine($"dd - '{destDirectory}'");
                var relativeName = Path.GetRelativePath(sourceDirectory, fileName);
                Console.WriteLine($"rn - '{relativeName}'");
                try
                { 
                    await using var sourceStream = File.Open(fileName, FileMode.Open, FileAccess.Read);
                    var destFileName = Path.Combine(destDirectory, relativeName);
                    Console.WriteLine($"df - '{destFileName}'");
                    await using var destinationStream = File.Create(destFileName);

                    await sourceStream.CopyToAsync(destinationStream);
                    
                    await sourceStream.FlushAsync(); 
                    await destinationStream.FlushAsync();
                    destinationStream.Close();
                    sourceStream.Close();
                }
                catch (Exception e)
                {
                    Console.WriteLine(e);
                    Console.WriteLine(fileName);
                    Console.WriteLine(destDirectory);
                    throw;
                }
            }
        }
        
        public static async Task<string> GenerateProto(string template, DatasetInfo dataset)
        {
            // var template = File.ReadAllText(@"Templates/proto.razor");

            var fields = string.Join("\n    ", FieldDefsToStrings(dataset.PersistedTo.Fields));
 
            var model = new
                {
                    Package = $"optimacalc.{dataset.Name.ToLowerInvariant()}",
                    RequestName = $"{dataset.Name}_Req",
                    RequestNameLin = $"{dataset.Name}_ReqWithLineage",
                    ResponseName = $"{dataset.Name}_Resp",
                    ResponseNameLin = $"{dataset.Name}_RespWithLineage",
                    RequestFields = fields,
                    ResponseFields = fields,
                    CsNamespace = "Optima.Calc"
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

        public static async Task GenerateCalcProbe(DatasetInfo dataset, string generatedProbesDestination = @"../Probes", string modelProbePath = @"../Probes/CalcProbe", string prefix = "Generated_")
        {
            // var template = await File.ReadAllTextAsync(@"Templates/proto.mustache");
            await CopyDirectory(modelProbePath, Path.Combine(generatedProbesDestination, prefix + (dataset.Id?.Uid ?? dataset.Name)), new string [] {"bin", "obj", @"\.idea"});
        }
    }
}