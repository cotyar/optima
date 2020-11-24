using System;
using System.Collections.Generic;
using System.IO;
using System.Globalization;
using System.Linq;
using System.Threading.Tasks;
using System.Xml.Linq;
using System.Xml.Schema;
using FSharp.Data;
using Google.Protobuf.Reflection;
using Optima.ColumnInferrer;
using Parquet;

// using FSharp.Data.Runtime.CsvInference;
using Optima.Domain.DatasetDefinition;
using static Optima.Domain.DatasetDefinition.PersistenceType.Types.FileDatasetInfo.Types;

namespace Optima.DatasetLoader
{
    public static class FileSchemaLoader
    {
        public static PersistenceType InferBinarySchema(string fileName) => 
            ToPersistenceType(fileName, FileFormat.Binary, new [] { new FieldDef { Name = "data", NativeFieldType = "Binary" }  });
        
        public static PersistenceType InferTextSchema(string fileName) => 
            ToPersistenceType(fileName, FileFormat.Text, new [] { new FieldDef { Name = "data", NativeFieldType = "Text" }  });

        public static PersistenceType InferCsvSchema(string fileName)
        {
            var columns =  Inferrers.inferCsvColumns(fileName).Select(v => new FieldDef
            {
                Name = v.Item1.Replace(" ", ""),
                NativeFieldType = v.Item2
            }).ToArray();
            
            return ToPersistenceType(fileName, FileFormat.Csv, columns);
        }

        public static PersistenceType InferJsonSchema(string fileName)
        {
            var columns = Inferrers.inferJsonColumns(fileName).Select(v => new FieldDef
            {
                Name = v.Item1,
                NativeFieldType = v.Item2
            }).ToArray();
            
            return ToPersistenceType(fileName, FileFormat.Json, columns);
        }

        public static PersistenceType ReadParquetSchema(string fileName)
        {
            using var fileStream = File.OpenRead(fileName);
            using var parquetReader = new ParquetReader(fileStream);
            var dataFields = parquetReader.Schema.GetDataFields();

            var columns = dataFields.Select((df, i) => new FieldDef
            {
                Name = df.Name,
                NativeFieldType = df.ClrType.FullName, // TODO: Change to df.DataType
                OrdinalPosition = i + 1
            }).ToArray();

            return ToPersistenceType(fileName, FileFormat.Parquet, columns);
        }

        private static PersistenceType ToPersistenceType(string fileName, FileFormat fileFormat, IList<FieldDef> columns) =>
            new PersistenceType
            {
                DescriptorProto = new DescriptorProto
                {
                    Name = Path.GetFileNameWithoutExtension(fileName),
                    Field = { columns.Select(PersistenceTypeHelper.ColumnToProto) }
                },
                Fields = { columns },
                File = new PersistenceType.Types.FileDatasetInfo
                {
                    Path = fileName,
                    FileFormat = fileFormat,
                    IsDirectory = false,
                    IsAppendable = false
                }
            };
    }
}