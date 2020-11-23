using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.ComponentModel.DataAnnotations;
using System.Globalization;
using System.IO;
using System.Threading.Tasks;
using System.Linq;
using System.Text.Json;
using System.Threading;
using CalcProbeServer.Storage;
using Cocona;
using CsvHelper;
using Generated;
using Grpc.Core;
using Google.Protobuf;
using Google.Protobuf.WellKnownTypes;
using Grpc.Core.Utils;
using Grpc.Reflection;
using Grpc.Reflection.V1Alpha;
using LinNet;
using Optima.Domain.DatasetDefinition;
using Enum = System.Enum;

#region Substituted usings
// {{#Usings}}

// {{/Usings}}
#endregion Substituted usings

namespace CalcProbeServer
{
    public class CsvDatasetSource : DatasetSource.DatasetSourceBase
    {
        private readonly string _filePath;
        private readonly string _delimiter;
        private readonly bool _hasHeader;

        public CsvDatasetSource(string filePath, string delimiter = ",", bool hasHeader = true)
        {
            _filePath = filePath;
            _delimiter = delimiter;
            _hasHeader = hasHeader;
        }
        
        public override async Task Data(DatasetDataRequest request, IServerStreamWriter<RowWithLineage> responseStream, ServerCallContext context)
        {
            using var reader = new StreamReader(_filePath);
            using var csv = new CsvReader(reader, CultureInfo.InvariantCulture);
            
            csv.Configuration.HasHeaderRecord = _hasHeader;
            csv.Configuration.Delimiter = _delimiter;
            var records = request.PagingCase switch
            {
                DatasetDataRequest.PagingOneofCase.All => csv.GetRecords<Row>().Select((r, i) => new RowWithLineage { RowNum = (ulong)i, Row = r }).ToArray(),
                DatasetDataRequest.PagingOneofCase.Page => csv.GetRecords<Row>().Skip((int) request.Page.StartIndex).Take((int) request.Page.PageSize).
                    Select((r, i) => new RowWithLineage { RowNum = (ulong)i + request.Page.StartIndex, Row = r }).ToArray(),
                DatasetDataRequest.PagingOneofCase.None => throw new NotImplementedException(),
                _ => throw new NotImplementedException()
            };
            
            // foreach (var row in records)
            // {
            //     await responseStream.WriteAsync(row);
            // }

            await responseStream.WriteAllAsync(records);
        }
    }
    
    public class JsonDatasetSource : DatasetSource.DatasetSourceBase
    {
        private readonly string _filePath;

        public JsonDatasetSource(string filePath)
        {
            _filePath = filePath;
        }
        
        public override async Task Data(DatasetDataRequest request, IServerStreamWriter<RowWithLineage> responseStream, ServerCallContext context)
        {
            await using var reader = File.OpenRead(_filePath); 
            
            var records = request.PagingCase switch 
            {
                DatasetDataRequest.PagingOneofCase.All => (await JsonSerializer.DeserializeAsync<Row[]>(reader)).Select((r, i) => new RowWithLineage { RowNum = (ulong)i, Row = r }),
                DatasetDataRequest.PagingOneofCase.Page => (await JsonSerializer.DeserializeAsync<Row[]>(reader)).Skip((int) request.Page.StartIndex).Take((int) request.Page.PageSize).
                    Select((r, i) => new RowWithLineage { RowNum = (ulong)i + request.Page.StartIndex, Row = r }),
                DatasetDataRequest.PagingOneofCase.None => throw new NotImplementedException(),
                _ => throw new NotImplementedException()
            };
            
            foreach (var row in records)
            {
                await responseStream.WriteAsync(row);
            }
        }
    }
    
    public class RocksDatasetSource : DatasetSource.DatasetSourceBase
    {
        private readonly string _folderPath;
        private readonly DatasetId _datasetId;

        public RocksDatasetSource(string folderPath, DatasetId datasetId)
        {
            _folderPath = folderPath;
            _datasetId = datasetId;
        }
        
        public override async Task Data(DatasetDataRequest request, IServerStreamWriter<RowWithLineage> responseStream, ServerCallContext context)
        {
            var reader = new RocksDbWrapper(_folderPath); // TODO: Make wrapped RocksDb long-leaving!!!
            
            var records = request.PagingCase switch
            {
                DatasetDataRequest.PagingOneofCase.All => reader.ReadAll(_datasetId.Uid).Select(r => RowWithLineage.Parser.ParseFrom(r.Item2)),
                DatasetDataRequest.PagingOneofCase.Page => reader.ReadPage(_datasetId.Uid, (long)request.Page.StartIndex, request.Page.PageSize).Select(r => RowWithLineage.Parser.ParseFrom(r.Item2)),
                DatasetDataRequest.PagingOneofCase.None => throw new NotImplementedException(),
                _ => throw new NotImplementedException()
            };
            
            foreach (var row in records)
            {
                await responseStream.WriteAsync(row);
            }
        }
    }
    
    public class CsvDatasetSink : DatasetSink.DatasetSinkBase
    {
        private readonly string _filePath;
        private readonly string _delimiter;
        private readonly bool _hasHeader;

        public CsvDatasetSink(string filePath, string delimiter = ",", bool hasHeader = true)
        {
            _filePath = filePath;
            _delimiter = delimiter;
            _hasHeader = hasHeader;
        }

        public override async Task<Empty> Data(IAsyncStreamReader<RowWithLineage> requestStream, ServerCallContext context)
        {
            await using var writer = new StreamWriter(_filePath);
            await using var csv = new CsvWriter(writer, CultureInfo.InvariantCulture);
            csv.Configuration.HasHeaderRecord = _hasHeader;
            csv.Configuration.Delimiter = _delimiter;
            
            csv.WriteHeader<Row>();
            await csv.NextRecordAsync();
            while (await requestStream.MoveNext())
            {
                csv.WriteRecord(requestStream.Current.Row);
                await csv.NextRecordAsync();
            }

            await writer.FlushAsync();

            return new Empty();
        }
    }
    
    public class JsonDatasetSink : DatasetSink.DatasetSinkBase
    {
        private readonly string _filePath;

        public JsonDatasetSink(string filePath)
        {
            _filePath = filePath;
        }

        public override async Task<Empty> Data(IAsyncStreamReader<RowWithLineage> requestStream, ServerCallContext context)
        {
            await using var writer = File.Create(_filePath);
            await JsonSerializer.SerializeAsync<IList<Row>>(writer, (await requestStream.ToListAsync()).Select(r => r.Row).ToList()); // TODO: Find a better streaming way
            await writer.FlushAsync();
            return new Empty();
        }
    }
    
    public class RocksDatasetSink : DatasetSink.DatasetSinkBase
    {
        private readonly string _folderPath;
        private readonly DatasetId _datasetId;

        public RocksDatasetSink(string folderPath, DatasetId datasetId)
        {
            _folderPath = folderPath;
            _datasetId = datasetId;
        }

        public override async Task<Empty> Data(IAsyncStreamReader<RowWithLineage> requestStream, ServerCallContext context)
        {
            var writer = new RocksDbWrapper(_folderPath); // TODO: Make wrapped RocksDb long-leaving!!!
            
            writer.WriteAll((await requestStream.ToListAsync()).Select((r, i) =>
            {
                r.RowNum = (ulong) i;
                return r.ToByteArray();
            }), _datasetId.Uid);

            return new Empty();
        }
    }

    public class Program
    {
        private const int PortBase = 5000;
        private static readonly TaskCompletionSource<int> ShutdownFlag = new TaskCompletionSource<int>();

        public static void Main(string[] args) => CoconaApp.Run<Program>(args);

        public void Run([Argument][Required(ErrorMessage = "Dataset Id is required.")]string datasetId, [Argument]string path, [Argument][SourceSupported]string source, [Argument]uint port = PortBase)
        {
            if (port == 0)
            {
                port = PortBase;
            }

            var sourceType = Enum.Parse<SupportedSources>(source.ToLowerInvariant());
            DatasetSource.DatasetSourceBase handler = sourceType switch
            {
                SupportedSources.csv => new CsvDatasetSource(path),
                SupportedSources.json => new JsonDatasetSource(path),
                SupportedSources.rocks => new RocksDatasetSource(path, new DatasetId { Uid = datasetId }),
                _ => throw new NotImplementedException()
            };
            var server = StartServer(PortBase, sourceType, handler);
        
            Console.WriteLine($"Server for Dataset {datasetId} is started on port {port}");
            Console.WriteLine("Press any key to stop ...");
            Console.ReadKey();
        
            ShutdownFlag.SetResult(0);
            
            Task.WaitAll(server);
        }

        private static Task StartServer(int port, SupportedSources source, DatasetSource.DatasetSourceBase handler) => Task.Run(async () =>
        {
            var reflectionServiceImpl = new ReflectionServiceImpl(DatasetSource.Descriptor, ServerReflection.Descriptor);
            var server = new Server
            {
                Services =
                {
                    DatasetSource.BindService(handler),
                    ServerReflection.BindService(reflectionServiceImpl)
                },
                Ports = {new ServerPort("localhost", port, ServerCredentials.Insecure)}
            };
            server.Start();

            Console.WriteLine("Server listening on port " + port);

            await ShutdownFlag.Task;
            await server.ShutdownAsync();
        });
    }

    enum SupportedSources
    {
        // ReSharper disable InconsistentNaming
        csv,
        json,
        rocks,
        // Parquet,
        // Postgres
        // ReSharper restore InconsistentNaming
    }

    internal class SourceSupportedAttribute : ValidationAttribute
    {
        protected override ValidationResult IsValid(object value, ValidationContext validationContext) => 
            System.Enum.GetNames(typeof(SupportedSources)).Contains((value?.ToString() ?? "").ToLowerInvariant()) 
                ? ValidationResult.Success 
                : new ValidationResult($"The source type '{value}' is not supported.");
    }
}