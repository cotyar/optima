using System.Globalization;
using System.Text.Json;

namespace Optima.Interfaces
{
    public static class ProtoFriendlyJsonSerializerOptions
    {
        public static JsonSerializerOptions JsonSerializerOptions = new JsonSerializerOptions
        {
            WriteIndented = true,
            
        };
    }
}