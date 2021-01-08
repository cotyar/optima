using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Dynamic;
using System.Linq;
using System.Reflection;
using Google.Protobuf;
using Google.Protobuf.Reflection;
using LinNet;
using Microsoft.VisualBasic;
using Optima.Domain.DatasetDefinition;
using PbFieldType = Google.Protobuf.Reflection.FieldType;
using grpc = global::Grpc.Core;
using wkt = Google.Protobuf.WellKnownTypes;
using static Google.Protobuf.WellKnownTypes.Field.Types.Kind;
using FieldType = Optima.Domain.DatasetDefinition.FieldType;
using Type = System.Type;
using FType = Google.Protobuf.Reflection.FieldDescriptorProto.Types.Type;
using FLabel = Google.Protobuf.Reflection.FieldDescriptorProto.Types.Label;

namespace DatasetProbeServer
{
    public static class ServiceReflectionHelper
    {
        public static (ImmutableArray<PbMessageDescriptor>, ImmutableArray<PbServiceDescriptor>) LoadPbDescriptors(Assembly assembly = null)
        {
            assembly ??= Assembly.GetAssembly(typeof(DatasetLineage));
            // ReSharper disable once PossibleNullReferenceException
            var messageTypes = assembly.DefinedTypes.Where(t => typeof(IMessage).IsAssignableFrom(t));
            var messageDescriptors = messageTypes.
                // ReSharper disable once PossibleNullReferenceException
                Select(t => (t, (MessageDescriptor)t.GetProperty("Descriptor").GetValue(null))).
                Select(t => new PbMessageDescriptor(t.t, t.Item2, ToWktType(t.Item2))).
                Where(t => !t.MessageDescriptor.FullName.StartsWith("lin.")).
                ToImmutableArray();
            var rpcTypes = assembly.DefinedTypes.Where(t => t.GetCustomAttribute<grpc::BindServiceMethodAttribute>() != null && !t.IsAbstract);
            var rpcDescriptors = rpcTypes.
                Select(t => (t, (ServiceDescriptor)t.BaseType.DeclaringType.GetProperty("Descriptor").GetValue(null))).
                Select(t => new PbServiceDescriptor(t.t, t.Item2, ToWktType(t.Item2))).
                ToImmutableArray();

            return (messageDescriptors, rpcDescriptors);
        }

        public class PbMessageDescriptor
        {
            public Type Type { get; }
            public MessageDescriptor MessageDescriptor { get; }
            public wkt.Type WktType { get; }

            public PbMessageDescriptor(Type type, MessageDescriptor messageDescriptor, wkt::Type wktType)
            {
                Type = type;
                MessageDescriptor = messageDescriptor;
                WktType = wktType;
            }
        }

        public class PbServiceDescriptor
        {
            public Type Type { get; }
            public ServiceDescriptor ServiceDescriptor { get; }
            public wkt.Api WktApi { get; }

            public PbServiceDescriptor(Type type, ServiceDescriptor serviceDescriptor, wkt::Api wktApi)
            {
                Type = type;
                ServiceDescriptor = serviceDescriptor;
                WktApi = wktApi;
            }
        }


        private static wkt.Type ToWktType(MessageDescriptor md) =>
            new wkt.Type
            {
                Name = md.Name,
                Fields = { md.Fields.InDeclarationOrder().Select(f => new wkt.Field
                {
                    Name = f.Name,
                    JsonName = f.Name,
                    Cardinality = f.IsRepeated ? wkt.Field.Types.Cardinality.Repeated : wkt.Field.Types.Cardinality.Optional,
                    // DefaultValue = f.
                    Kind = f.FieldType switch
                    {
                        PbFieldType.Double => TypeDouble,
                        PbFieldType.Float => TypeFloat,
                        PbFieldType.Int64 => TypeInt64,
                        PbFieldType.UInt64 => TypeUint64,
                        PbFieldType.Int32 => TypeInt32,
                        PbFieldType.Fixed64 => TypeFixed64,
                        PbFieldType.Fixed32 => TypeFixed32,
                        PbFieldType.Bool => TypeBool,
                        PbFieldType.String => TypeString,
                        PbFieldType.Group => TypeGroup,
                        PbFieldType.Message => TypeMessage,
                        PbFieldType.Bytes => TypeBytes,
                        PbFieldType.UInt32 => TypeUint32,
                        PbFieldType.SFixed32 => TypeSfixed32,
                        PbFieldType.SFixed64 => TypeSfixed64,
                        PbFieldType.SInt32 => TypeSint32,
                        PbFieldType.SInt64 => TypeSint64,
                        PbFieldType.Enum => TypeEnum,
                        _ => throw new ArgumentOutOfRangeException()
                    }, 
                    TypeUrl = f.FieldType switch
                    {
                        PbFieldType.Group => f.MessageType.FullName,
                        PbFieldType.Message => f.MessageType.FullName,
                        _ => ""
                    }, 
                    // TODO: Add missing
                })}
            };

        private static wkt.Api ToWktType(ServiceDescriptor sd) =>
            new wkt.Api
            {
                Name = sd.Name,
                // Version = sd.
                Methods =
                {
                    sd.Methods.Select(md => new wkt.Method
                    {
                        Name = md.Name,
                        RequestTypeUrl = md.InputType.FullName,
                        RequestStreaming = md.IsClientStreaming,
                        ResponseTypeUrl = md.OutputType.FullName,
                        ResponseStreaming = md.IsServerStreaming
                    })
                }
            };

        public static wkt.Field.Types.Kind ToKindType(this FieldDef fd) =>
            fd.Type switch
            {
                FieldType.String => TypeString,
                FieldType.Int8 => TypeInt32,
                FieldType.Int16 => TypeInt32,
                FieldType.Int32 => TypeInt32,
                FieldType.Int64 => TypeInt64,
                FieldType.Float32 => TypeFloat,
                FieldType.Float64 => TypeDouble,
                FieldType.Decimal => TypeDouble,
                FieldType.Boolean => TypeBool,
                _ => throw new ArgumentOutOfRangeException()
            };
        
        public static wkt.Field.Types.Kind ToKindType(this Type type) =>
            type.Name switch
            {
                "System.String" => TypeString,
                "System.SByte" => TypeInt32,
                "System.Byte" => TypeInt32,
                "System.Short" => TypeInt32,
                "System.Int32" => TypeInt32,
                "System.Int64" => TypeInt64,
                "System.Single" => TypeFloat,
                "System.Double" => TypeDouble,
                "System.Boolean" => TypeBool,
                _ => throw new ArgumentOutOfRangeException()
            };
        
        public static FType ToFieldType(this Type type) =>
            type.FullName switch
            {
                "System.String" => FType.String,
                "System.SByte" => FType.Int32,
                "System.Byte" => FType.Int32,
                "System.Short" => FType.Int32,
                "System.Int32" => FType.Int32,
                "System.Int64" => FType.Int64,
                "System.Single" => FType.Float,
                "System.Double" => FType.Double,
                "System.Boolean" => FType.Bool,
                _ => throw new ArgumentOutOfRangeException()
            };

        public static object GetProperty(this object target, string name) =>
            target is ExpandoObject
                ? (((IDictionary<string, object>)target).TryGetValue(name, out var val) ? val : null)
                : (target.GetType().GetProperty(name) != null ? Microsoft.VisualBasic.CompilerServices.Versioned.CallByName(target, name, CallType.Get) : null);
        public static bool IsPropertyExist(this object target, string name) =>
            target is ExpandoObject
                ? ((IDictionary<string, object>)target).ContainsKey(name)
                : target.GetType().GetProperty(name) != null;
    }
}
