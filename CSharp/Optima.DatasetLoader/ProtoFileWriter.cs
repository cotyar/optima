using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Google.Protobuf.Reflection;

namespace Optima.DatasetLoader
{
    public class ProtoFileWriter
    {
        public static bool IsWellKnownType(FileDescriptor descriptor) =>
            descriptor.Name.StartsWith("google/protobuf/")
            && descriptor.Package.Equals("google.protobuf");

        public static async Task WriteFileDescriptor(FileDescriptor descriptor, TextWriter writer)
        {
            // Syntax
            await writer.WriteLineAsync("syntax = \"proto3\";");

            // Dependencies
            foreach (var dependency in descriptor.Dependencies)
            {
                await writer.WriteLineAsync($"import \"{dependency.Name}\";");
            }

            // Package
            await writer.WriteLineAsync($"package {descriptor.Package};");

            // Empty line
            await writer.WriteLineAsync();

            // Messages
            foreach (var message in descriptor.MessageTypes)
            {
                await WriteMessageDescriptor(message, writer);
                await writer.WriteLineAsync();
            }

            // Messages
            foreach (var service in descriptor.Services)
            {
                await WriteServiceDescriptor(service, writer);
                await writer.WriteLineAsync();
            }
        }

        public static async Task WriteServiceDescriptor(ServiceDescriptor service, TextWriter writer, string indentation = NoIndent)
        {
            await writer.WriteLineAsync($"service {service.Name} {{");
            foreach (var method in service.Methods)
            {
                await WriteMethodDescription(method, writer, indentation + Indent);
            }
            await writer.WriteLineAsync($"{indentation}}}");
        }

        public static async Task WriteMethodDescription(MethodDescriptor method, TextWriter writer, string indentation = NoIndent)
        {
            await writer.WriteAsync($"{indentation} rpc {method.Name}(");

            if (method.IsClientStreaming)
            {
                await writer.WriteAsync("stream ");
            }
            await writer.WriteAsync($"{method.InputType.Name}) returns (");
            if (method.IsServerStreaming)
            {
                await writer.WriteAsync("stream ");
            }
            await writer.WriteLineAsync($"{method.OutputType.Name});");
        }

        public static async Task WriteMessageDescriptor(MessageDescriptor message, TextWriter writer, string indentation = NoIndent)
        {
            await writer.WriteAsync(indentation);
            await writer.WriteLineAsync($"message {message.Name} {{");

            foreach (var nestedType in message.NestedTypes)
            {
                await WriteMessageDescriptor(nestedType, writer, indentation + Indent);
            }

            foreach (var field in message.Fields.InDeclarationOrder().Where(f => f.ContainingOneof is null))
            {
                await WriteFieldDescriptor(field, writer, indentation + Indent);
            }

            foreach (var oneof in message.Oneofs)
            {
                await WriteOneOfDescriptor(oneof, writer, indentation + Indent);
            }

            await writer.WriteLineAsync($"{indentation}}}");
        }

        public static async Task WriteOneOfDescriptor(OneofDescriptor oneof, TextWriter writer, string indentation = NoIndent)
        {
            await writer.WriteLineAsync($"{indentation}oneof {oneof.Name} {{");
            foreach (var field in oneof.Fields)
            {
                await WriteFieldDescriptor(field, writer, indentation + Indent);
            }
            await writer.WriteLineAsync($"{indentation}}}");
        }

        public static async Task WriteFieldDescriptor(FieldDescriptor field, TextWriter writer, string indentation = NoIndent)
        {
            await writer.WriteAsync(indentation);

            if (field.IsRepeated)
            {
                await writer.WriteAsync("repeated ");
            }

            switch (field.FieldType)
            {
                case FieldType.Double:
                case FieldType.Float:
                case FieldType.Int32:
                case FieldType.Int64:
                case FieldType.UInt32:
                case FieldType.UInt64:
                case FieldType.SInt32:
                case FieldType.SInt64:
                case FieldType.Fixed32:
                case FieldType.Fixed64:
                case FieldType.SFixed32:
                case FieldType.SFixed64:
                case FieldType.Bool:
                case FieldType.String:
                case FieldType.Bytes:
                    await writer.WriteAsync(field.FieldType.ToString().ToLowerInvariant());
                    break;
                case FieldType.Group:
                    break;
                case FieldType.Message:
                    await writer.WriteAsync(field.MessageType.Name);
                    break;
                case FieldType.Enum:
                    await writer.WriteAsync(field.EnumType.Name);
                    break;
                default:
                    throw new ArgumentOutOfRangeException();
            }

            await writer.WriteLineAsync($" {field.Name} = {field.FieldNumber};");
        }
        
        private const string NoIndent = "";
        private const string Indent = "  ";
    }
}