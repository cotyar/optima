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

        private static async Task<string> GenerateProto(string name, IEnumerable<FieldDef> fieldDefs, string templateName)
        {
            var model = ToModel(name, fieldDefs);

            var template = await new EmbeddedResourceLoader(typeof(GeneratorHelper).Assembly).LoadAsync(templateName); 
            return await new StubbleBuilder().Build().RenderAsync(template, model);
        }

        private static dynamic ToModel(string name, IEnumerable<FieldDef> fieldDefs)
        {
            var fields = string.Join("\n    ", FieldDefsToStrings(fieldDefs));
            var usingTemplate = @"
using Calc = {{CsNamespace}}.Calc;
using Req = {{CsNamespace}}.{{RequestName}};
using ReqWithLineage = {{CsNamespace}}.{{RequestNameLin}};
using Resp = {{CsNamespace}}.{{ResponseName}};
using RespWithLineage = {{CsNamespace}}.{{ResponseNameLin}};
// ";
            var usingDsTemplate = @"
using Row = {{CsDsNamespace}}.{{RowName}}.Row;
using RowWithLineage = {{CsDsNamespace}}.{{RowName}}.RowWithLineage;
using DatasetSource = {{CsDsNamespace}}.{{RowName}}.DatasetSource;
using DatasetSink = {{CsDsNamespace}}.{{RowName}}.DatasetSink;
// ";
            
            return new
                {
                    Package = $"optimacalc.{name.ToLowerInvariant()}",
                    RequestName = $"{name}_Req",
                    RequestNameLin = $"{name}_ReqWithLineage",
                    ResponseName = $"{name}_Resp",
                    ResponseNameLin = $"{name}_RespWithLineage",
                    RequestFields = fields,
                    ResponseFields = fields,
                    CsNamespace = "Optima.Calc",
                    RowName = $"{name.Replace(" ", "")}",
                    CsDsNamespace = "Optima.Dataset",
                    Usings = new Func<string, Func<string, string>, object>((str, render) => render(usingTemplate)),
                    UsingsDs = new Func<string, Func<string, string>, object>((str, render) => render(usingDsTemplate))
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

        public static Task<string> GenerateDatasetProto(string name, IEnumerable<FieldDef> fieldDefs) => GenerateProto(name, fieldDefs, "datasetProbe");

        public static async Task GenerateProbes(DatasetInfo dataset, string generatedProbesDestination = @"../Probes", string modelProbePath = @"../Probes/CalcProbe", string prefix = "CalcGen_")
        {
            await CopyDirectoryAsync(modelProbePath, 
                Path.Combine(generatedProbesDestination, prefix + (dataset.Id?.Uid ?? dataset.Name)), 
                new [] {@".*\\bin\\{0,1}.*", @".*\\obj\\{0,1}.*", @"\.idea", @"\.vs.*"},
                new Dictionary<string, string>
                {
                    { "calcProbe.proto", await GenerateProto(dataset.Name, dataset.PersistedTo.Fields, "calcProbe") },
                    { "datasetProbe.proto", await GenerateProto(dataset.Name, dataset.PersistedTo.Fields, "datasetProbe") }
                },
                new [] {@".*\.cs"},
                ToModel(dataset.Name, dataset.PersistedTo.Fields));
        }
        
        public static async Task<DatasetInfo> ToDatasetInfo(PersistenceType pt)
        {
            var name = pt.PersistenceCase switch
            {
                PersistenceType.PersistenceOneofCase.File => Path.GetFileNameWithoutExtension(pt.File.Path),
                PersistenceType.PersistenceOneofCase.Db =>
                    $"{pt.Db.DbProvider.Postgres.TableCatalog}_{pt.Db.DbProvider.Postgres.SchemaName}_{pt.Db.DbProvider.Postgres.TableName}",
                // PersistenceType.PersistenceOneofCase.None => ,
                // PersistenceType.PersistenceOneofCase.Hive => ,
                // PersistenceType.PersistenceOneofCase.Memory => ,
                _ => $"Mds{Guid.NewGuid():N}"
            };

            return new DatasetInfo
            {
                Id = new DatasetId { Uid = Guid.NewGuid().ToString("N") },
                Name = name,
                Schema = new DatasetSchema { ProtoFile = await GenerateDatasetProto(name, pt.Fields) },
                PersistedTo = pt
            };
        }
    }
}