using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Optima.Domain.DatasetDefinition;
using Stubble.Core.Builders;
using Stubble.Extensions.Loaders;

namespace Optima.ProtoGenerator
{
    public static class GeneratorHelper
    {
        public static async Task CopyDirectoryAsync(
            string sourceDirectory, 
            string destDirectory, 
            string[] ignorePatterns = null,
            IDictionary<string, string> substitutes = null,
            string[] templatePatterns = null,
            dynamic templateTags = null)
        {
            ignorePatterns ??= new string[0];
            substitutes ??= new Dictionary<string, string>();
            templatePatterns ??= new string[0];
            
            if (ignorePatterns.Any(pattern => Regex.IsMatch(sourceDirectory, pattern, RegexOptions.CultureInvariant | RegexOptions.IgnorePatternWhitespace | RegexOptions.IgnoreCase))) return;
            
            if (!Directory.Exists(destDirectory))
                Directory.CreateDirectory(destDirectory);

            foreach (var file in Directory.GetFiles(sourceDirectory))
            {
                if (ignorePatterns.Any(pattern => Regex.IsMatch(file, pattern, RegexOptions.CultureInvariant | RegexOptions.IgnorePatternWhitespace | RegexOptions.IgnoreCase))) continue;
                
                var name = Path.GetFileName(file);
                var dest = Path.Combine(destDirectory, name);
                
                string content;
                if (!substitutes.TryGetValue(name, out content))
                {
                    content = await File.ReadAllTextAsync(file);
                    if (templatePatterns.Any(pattern => Regex.IsMatch(name, pattern, RegexOptions.CultureInvariant | RegexOptions.IgnorePatternWhitespace | RegexOptions.IgnoreCase)))
                    {
                        content = await new StubbleBuilder().Build().RenderAsync(content, templateTags);
                    }
                }

                await File.WriteAllTextAsync(dest, content);
            }

            foreach (var folder in Directory.GetDirectories( sourceDirectory ))
            {
                var name = Path.GetFileName( folder );
                var dest = Path.Combine( destDirectory, name );
                await CopyDirectoryAsync( folder, dest, ignorePatterns, substitutes, templatePatterns, templateTags);
            }
        }
        
        public static async Task<string> GenerateProto(DatasetInfo dataset)
        {
            var model = ToModel(dataset);

            var template = await new EmbeddedResourceLoader(typeof(GeneratorHelper).Assembly).LoadAsync("proto"); 
            return await new StubbleBuilder().Build().RenderAsync(template, model);
        }

        private static dynamic ToModel(DatasetInfo dataset)
        {
            var fields = string.Join("\n    ", FieldDefsToStrings(dataset.PersistedTo.Fields));
            var usingTemplate = @"
using Calc = {{CsNamespace}}.Calc;
using Req = {{CsNamespace}}.{{RequestName}};
using ReqWithLineage = {{CsNamespace}}.{{RequestNameLin}};
using Resp = {{CsNamespace}}.{{ResponseName}};
using RespWithLineage = {{CsNamespace}}.{{ResponseNameLin}};
// ";
            
            return new
                {
                    Package = $"optimacalc.{dataset.Name.ToLowerInvariant()}",
                    RequestName = $"{dataset.Name}_Req",
                    RequestNameLin = $"{dataset.Name}_ReqWithLineage",
                    ResponseName = $"{dataset.Name}_Resp",
                    ResponseNameLin = $"{dataset.Name}_RespWithLineage",
                    RequestFields = fields,
                    ResponseFields = fields,
                    CsNamespace = "Optima.Calc",
                    Usings = new Func<string, Func<string, string>, object>((str, render) => render(usingTemplate))
                };
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
            await CopyDirectoryAsync(modelProbePath, 
                Path.Combine(generatedProbesDestination, prefix + (dataset.Id?.Uid ?? dataset.Name)), 
                new [] {"bin", "obj", @"\.idea", @"\.vs.*"},
                new Dictionary<string, string> { { "generated.proto", await GenerateProto(dataset) } },
                new [] {@".*\.cs"},
                ToModel(dataset));
        }
    }
}