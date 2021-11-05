using System;
using System.Collections;
using System.IO;
using System.Linq;
using Newtonsoft.Json;
using System.Net;
using Newtonsoft.Json.Linq;
using System.Net.Http;
using System.Threading.Tasks;

namespace WishAndGet.Infrastructure.JsonLd
{
    internal class JSONUtils
    {
        public static JToken FromReader(TextReader r)
        {
            var serializer = new JsonSerializer();
            
            using (var reader = new JsonTextReader(r))
            {
                var result = (JToken)serializer.Deserialize(reader);
                return result;
            }
        }

        public static JToken FromInputStream(Stream content)
        {
            return FromInputStream(content, "UTF-8");
        }

        public static JToken FromInputStream(Stream content, string enc)
        {
            return FromReader(new StreamReader(content, System.Text.Encoding.GetEncoding(enc)));
        }
    }
}
