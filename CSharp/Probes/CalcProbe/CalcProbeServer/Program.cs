using System;
using System.Collections.Immutable;
using System.Threading.Tasks;
using System.Linq;
using System.Threading;
using CalcProbeServer.Storage;
using Grpc.Core;
using Google.Protobuf;

using LinNet;
using TestDataset = LinNet.TestDataset;
using TestDatasetWithLineage = LinNet.TestDatasetWithLineage;
using TestDatasetResp = LinNet.TestDatasetResp;
using TestDatasetRespWithLineage = LinNet.TestDatasetRespWithLineage;

namespace CalcProbeServer
{
    public class GeneratedCalcBase : Calc.CalcBase
    {
        private readonly ImmutableDictionary<string, ImmutableArray<FieldDef>> _fieldMapping;
        public CalculatorId CalculatorId { get; }

        public GeneratedCalcBase(CalculatorId calculatorId, ImmutableDictionary<string, ImmutableArray<FieldDef>> fieldMapping)
        {
            _fieldMapping = fieldMapping;
            CalculatorId = calculatorId;
        }
        
        public override Task Run(IAsyncStreamReader<TestDataset> requestStream, IServerStreamWriter<TestDatasetResp> responseStream, ServerCallContext context)
        {
            return base.Run(requestStream, responseStream, context);
        }

        public override async Task RunWithLineage(IAsyncStreamReader<TestDatasetWithLineage> requestStream, IServerStreamWriter<TestDatasetRespWithLineage> responseStream, ServerCallContext context)
        {
            var rowIndex = 0;
            var reader = new Reader(requestStream);
            var writer = new Writer(responseStream, resp => 
                new TestDatasetRespWithLineage
                {
                    RowResponse = new TestDatasetRespWithLineage.Types.RowResponse
                    {
                        Row = { resp }, 
                        RowLineage =
                        {
                            new RowLineage
                            {
                                Lineage = { _fieldMapping.Keys.Select(p => (p, new RowLineage.Types.RowFieldLineage{ RowId = rowIndex++, CalculatorId = CalculatorId, Parents = { _fieldMapping[p] }})).
                                    ToDictionary(kv => kv.p, kv => kv.Item2) }
                            }
                        } // TODO: Populate Parents
                    }
                });
            
            await Run(reader, writer, context);

            if (reader.ParentLineage != null)
            {
                await responseStream.WriteAsync(new TestDatasetRespWithLineage {DatasetLineage = reader.ParentLineage}); // TODO: Add CalcLineage and decorate
            }
        }
        
        // public static ImmutableArray<string> FieldNames = typeof(TestDatasetResp).GetProperties().Select(p => p.Name).ToImmutableArray();

        public override Task<TestDataset> Echo(TestDataset request, ServerCallContext context) => Task.FromResult(request);

        class Reader : IAsyncStreamReader<TestDataset>
        {
            private readonly IAsyncStreamReader<TestDatasetWithLineage> _parentReader;

            public Reader(IAsyncStreamReader<TestDatasetWithLineage> parentReader)
            {
                _parentReader = parentReader;
            }
            
            public async Task<bool> MoveNext(CancellationToken cancellationToken)
            {
                var ret = await _parentReader.MoveNext();

                while (ret && _parentReader.Current.CaseCase == TestDatasetWithLineage.CaseOneofCase.DatasetLineage) // Skipping Lineage information while proxying
                {
                    ParentLineage = _parentReader.Current.DatasetLineage;
                    ret = await _parentReader.MoveNext();
                }

                return ret;
            }

            public TestDataset Current => _parentReader.Current?.Request;
            
            public DatasetLineage ParentLineage { get; private set; }
        }

        class Writer : IServerStreamWriter<TestDatasetResp>
        {
            private readonly IServerStreamWriter<TestDatasetRespWithLineage> _parentWriter;
            private readonly Func<TestDatasetResp, TestDatasetRespWithLineage> _rowLineageApplier;

            public Writer(IServerStreamWriter<TestDatasetRespWithLineage> parentWriter, Func<TestDatasetResp, TestDatasetRespWithLineage> rowLineageApplier)
            {
                _parentWriter = parentWriter;
                _rowLineageApplier = rowLineageApplier;
            }
            
            public async Task WriteAsync(TestDatasetResp message) => await _parentWriter.WriteAsync(_rowLineageApplier(message));

            public WriteOptions WriteOptions
            {
                get => _parentWriter.WriteOptions;
                set => _parentWriter.WriteOptions = value;
            }
        }
    }

    public class GeneratedCalcProxy : GeneratedCalcBase
    {
        private readonly Calc.CalcClient _calcClient;
        private readonly RocksDbWrapper _rocks;
        private readonly CallOptions _options;

        public GeneratedCalcProxy(CalculatorId calculatorId, ImmutableDictionary<string, ImmutableArray<FieldDef>> fieldMapping, Calc.CalcClient calcClient, CallOptions options = default, string rocksRootFolder = null) : base(calculatorId, fieldMapping)
        {
            _calcClient = calcClient;
            _options = options;
            _rocks = rocksRootFolder != null ? new RocksDbWrapper(rocksRootFolder) : null;
        }

        public override async Task Run(IAsyncStreamReader<TestDataset> requestStream, IServerStreamWriter<TestDatasetResp> responseStream, ServerCallContext context)
        {
            var proxyRun = _calcClient.Run(_options);
            var rocksChannel = System.Threading.Channels.Channel.CreateUnbounded<TestDatasetResp>();
            var runUid = Guid.NewGuid().ToString("N");

            var readTask = Task.Run(async () =>
                {
                    while (await requestStream.MoveNext()) 
                        await proxyRun.RequestStream.WriteAsync(requestStream.Current);

                    await proxyRun.RequestStream.CompleteAsync();
                });
            
            var writeTask = Task.Run(async () =>
            {
                while (await proxyRun.ResponseStream.MoveNext())
                {
                    if (_rocks != null)
                    {
                        await rocksChannel.Writer.WriteAsync(proxyRun.ResponseStream.Current);
                    }
                    await responseStream.WriteAsync(proxyRun.ResponseStream.Current);
                }
            });
            
            var dbTask = _rocks == null 
                ? Task.CompletedTask 
                : Task.Run(() => _rocks.Write(rocksChannel.Reader.ReadAllAsync().ToEnumerable().Select(r => r.ToByteArray()), runUid));

            await readTask;
            await writeTask;
            await dbTask;
        }
        
        public override Task<TestDataset> Echo(TestDataset request, ServerCallContext context) => _calcClient.EchoAsync(request, _options).ResponseAsync;
    }
    
    public class ExampleCalcImpl : Calc.CalcBase
    {
        public override async Task Run(IAsyncStreamReader<TestDataset> requestStream, IServerStreamWriter<TestDatasetResp> responseStream, ServerCallContext context)
        {
            while (await requestStream.MoveNext()) 
                await responseStream.WriteAsync(new TestDatasetResp { Field1 = $"C# Calc F1: {requestStream.Current.Field1}", Field2 = 1_000_000 + requestStream.Current.Field2 });
        }

        public override Task<TestDataset> Echo(TestDataset request, ServerCallContext context) => Task.FromResult(request);
    }

    class Program
    {
        const int PortBase = 5050;
        static TaskCompletionSource<int> shutdownFlag = new TaskCompletionSource<int>();
        
        static void Main(string[] args)
        {
            var fieldMapping = new [] { ("Field1", new[] {new FieldDef { Name = "Field2" }}.ToImmutableArray()), ("Field2", new[] {new FieldDef { Name = "Field2" }}.ToImmutableArray() ) }.ToImmutableDictionary(kv => kv.Item1, kv => kv.Item2);

            var server = StartServer(PortBase + 1, new ExampleCalcImpl());
            var proxySharp = StartServer(PortBase + 2, 
                new GeneratedCalcProxy(new CalculatorId { Uid = Guid.NewGuid().ToString("N")}, 
                    fieldMapping,
                    new Calc.CalcClient(new Channel("localhost", PortBase + 1, ChannelCredentials.Insecure))));
            var proxyPython = StartServer(PortBase + 3, new GeneratedCalcProxy(new CalculatorId { Uid = Guid.NewGuid().ToString("N")}, 
                fieldMapping,
                new Calc.CalcClient(new Channel("localhost", PortBase + 5, ChannelCredentials.Insecure))));

            Console.WriteLine("Server and proxies started");
            Console.WriteLine("Press any key to stop ...");
            Console.ReadKey();

            shutdownFlag.SetResult(0);
            
            Task.WaitAll(server, proxySharp, proxyPython);
        }

        static Task StartServer(int port, Calc.CalcBase handler) => Task.Run(async () =>
        {
            var server = new Server
            {
                Services = {Calc.BindService(handler)},
                Ports = {new ServerPort("localhost", port, ServerCredentials.Insecure)}
            };
            server.Start();

            Console.WriteLine("ExampleCalc server listening on port " + port);

            await shutdownFlag.Task;
            await server.ShutdownAsync();
        });
    }
}