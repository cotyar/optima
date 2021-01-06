using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using CalcProbeServer.Storage;
using Cocona;
using CsvHelper;
using Generated;
using Google.Protobuf;
using Google.Protobuf.Reflection;
using Google.Protobuf.WellKnownTypes;
using Grpc.Core;
using Grpc.Core.Utils;
using Grpc.Health.V1;
using Grpc.HealthCheck;
using Grpc.Reflection;
using Grpc.Reflection.V1Alpha;
using LinNet;
using Optima.Domain.DatasetDefinition;
using Enum = System.Enum;
using JsonSerializer = System.Text.Json.JsonSerializer;

#region Substituted usings
// {{#UsingsDs}}

// {{/UsingsDs}}
#endregion Substituted usings

namespace DatasetProbeServer
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
            csv.Configuration.MissingFieldFound = null;
            try
            {
                var records = request.PagingCase switch
                {
                    DatasetDataRequest.PagingOneofCase.All => csv.GetRecords<Row>().Select((r, i) => new RowWithLineage { RowNum = (ulong)i, Row = r }).ToArray(), // TODO: Remove ToArray-s?
                    DatasetDataRequest.PagingOneofCase.Page => csv.GetRecords<Row>().Skip((int) request.Page.StartIndex).Take((int) request.Page.PageSize).
                        Select((r, i) => new RowWithLineage { RowNum = (ulong)i + request.Page.StartIndex, Row = r }).ToArray(),
                    DatasetDataRequest.PagingOneofCase.None => throw new NotImplementedException(),
                    _ => throw new NotImplementedException()
                };
                
                await responseStream.WriteAllAsync(records);
            }
            catch (Exception e)
            {
                Console.Error.WriteLine(e); // TODO: Change to Logger
                throw new RpcException(new Status(StatusCode.Internal, "Data()", e));
            }
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
            var server = StartServer((int) port, sourceType, handler, "lin.generated.test1.DatasetSource");
        
            Console.WriteLine($"Server for Dataset {datasetId} is started on port {port}");
            // Console.WriteLine("Press any key to stop ...");
            // Console.ReadKey();
        
            // ShutdownFlag.SetResult(0);
            
            Task.WaitAll(server);
        }

        private static Task StartServer(int port, SupportedSources source, DatasetSource.DatasetSourceBase handler, string serviceName) => Task.Run(async () =>
        {
            var protoGetter = typeof(FileDescriptor).GetProperty("Proto", BindingFlags.NonPublic | BindingFlags.Instance);
            var fileProto = (FileDescriptorProto) protoGetter.GetValue(DatasetProbeReflection.Descriptor);
            
            // var txt = JsonFormatter.Default.Format(new FileDescriptorProto(fileProto));
            // var fileProto2 = JsonParser.Default.Parse<FileDescriptorProto>(txt);

            var newFileProto = fileProto.Clone(); // TODO: Clear Field and re-add field definitions 
            var bytes = newFileProto.ToByteString();

            IEnumerable<FileDescriptor> Deps(FileDescriptor descriptor)
            {
                foreach (var dependency in descriptor.Dependencies)
                {
                    foreach (var dep in Deps(dependency))
                    {
                        yield return dep;
                    }

                    yield return dependency;
                }
            }

            var deps = DatasetProbeReflection.Descriptor.Dependencies.SelectMany(Deps).Distinct().Concat(new [] {DatasetLineage.Descriptor.File}).
                Select(d => ((FileDescriptorProto) protoGetter.GetValue(d)).ToByteString()).ToArray();

            var newDatasetProbeReflectionDescriptor = FileDescriptor.BuildFromByteStrings(deps.Concat(new [] {bytes}).ToArray()).Last();
            var newDataSourceDescriptor = newDatasetProbeReflectionDescriptor.Services[0];
            
            var grpcTransport = new GrpcTransport<DatasetDataRequest, RowWithLineage>(serviceName, handler.Data);
            
            var serviceDescriptors = new [] {newDataSourceDescriptor, Health.Descriptor, ServerReflection.Descriptor}; // TODO: Replace DatasetSource.Descriptor

            var reflectionServiceImpl = new ReflectionServiceImpl(newDataSourceDescriptor, ServerReflection.Descriptor);
            var healthServiceImpl = new HealthServiceImpl();
            var server = new Server
            {
                Services =
                {
                    // DatasetSource.BindService(handler),
                    grpcTransport.ServerServiceDefinition,
                    Health.BindService(healthServiceImpl),
                    ServerReflection.BindService(reflectionServiceImpl)
                },
                Ports = {new ServerPort("localhost", port, ServerCredentials.Insecure)}
            };
            server.Start();
            
            // Mark all services as healthy.
            foreach (var serviceDescriptor in serviceDescriptors)
            {
                healthServiceImpl.SetStatus(serviceDescriptor.FullName, HealthCheckResponse.Types.ServingStatus.Serving);
            }
            // Mark overall server status as healthy.
            healthServiceImpl.SetStatus("", HealthCheckResponse.Types.ServingStatus.Serving);

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
            Enum.GetNames(typeof(SupportedSources)).Contains((value?.ToString() ?? "").ToLowerInvariant()) 
                ? ValidationResult.Success 
                : new ValidationResult($"The source type '{value}' is not supported.");
    }
}