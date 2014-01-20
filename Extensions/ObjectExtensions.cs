using Newtonsoft.Json;

namespace Gnomoria.ContentExtractor.Extensions
{
    public static class ObjectExtensions
    {
        public static string Dump(this object obj)
        {
            return JsonConvert.SerializeObject(obj, Formatting.Indented);
        }
    }
}
