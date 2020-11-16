using System;
using System.IO;
using System.Globalization;
using System.Linq;
using System.Threading.Tasks;
using System.Xml.Linq;
using System.Xml.Schema;
using FSharp.Data;
using Optima.ColumnInferrer;

// using FSharp.Data.Runtime.CsvInference;

namespace Optima.DatasetLoader
{
    public static class CsvSchemaLoader
    {
        public static string[] InferCsvSchema() => Inferrers.inferCsvColumns().Select(v => $"{v.Item1} - {v.Item2}").ToArray();
        
        public static string[] InferJsonSchema() => Inferrers.inferJsonColumns().Select(v => $"{v.Item1} - {v.Item2}").ToArray();
    }
}