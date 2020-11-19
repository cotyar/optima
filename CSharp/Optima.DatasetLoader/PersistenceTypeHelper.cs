using Google.Protobuf.Reflection;
using Optima.Domain.DatasetDefinition;

namespace Optima.DatasetLoader
{
    public static class PersistenceTypeHelper
    {
        public static FieldDescriptorProto ColumnToProto(FieldDef column) =>
            new FieldDescriptorProto
            {
                Name = column.Name,
                Type = column.NativeFieldType switch
                {
                    "System.Int32" => FieldDescriptorProto.Types.Type.Int32,
                    "System.Int64" => FieldDescriptorProto.Types.Type.Int64,
                    "System.Double" => FieldDescriptorProto.Types.Type.Double,
                    "System.Boolean" => FieldDescriptorProto.Types.Type.Bool,
                    "Binary" => FieldDescriptorProto.Types.Type.Bytes,
                    "Text" => FieldDescriptorProto.Types.Type.String,
                    _ => FieldDescriptorProto.Types.Type.String
                }
            };
    }
}