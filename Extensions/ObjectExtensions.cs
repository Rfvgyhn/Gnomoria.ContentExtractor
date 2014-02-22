using Newtonsoft.Json;
using Newtonsoft.Json.Converters;

namespace Gnomoria.ContentExtractor.Extensions
{
    public static class ObjectExtensions
    {
        public static string Dump(this object obj)
        {
            return JsonConvert.SerializeObject(obj, Formatting.Indented, new StringEnumConverter());
        }
    }
}
