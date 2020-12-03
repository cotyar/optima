using System;
using System.Text.Json;
using System.Text.Json.Serialization;
using Google.Protobuf;

namespace Optima.Interfaces
{
    public class ProtoMessageConverter : JsonConverter<IMessage>
    {
        public override bool CanConvert(Type typeToConvert) =>
            typeof(IMessage).IsAssignableFrom(typeToConvert);
        
        public override IMessage Read(
            ref Utf8JsonReader reader,
            Type typeToConvert,
            JsonSerializerOptions options) =>
            (IMessage) typeof(JsonParser).GetMethod(nameof(JsonParser.Parse), 1, new[] {typeof(string)})
                !.MakeGenericMethod(typeToConvert).Invoke(null, new object [] { reader.GetString() });

        public override void Write(
            Utf8JsonWriter writer,
            IMessage message,
            JsonSerializerOptions options) =>
            JsonFormatter.Default.Format(message);
    }
}