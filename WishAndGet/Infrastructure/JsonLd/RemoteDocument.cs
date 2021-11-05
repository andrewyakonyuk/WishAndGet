using Newtonsoft.Json.Linq;

namespace WishAndGet.Infrastructure.JsonLd
{
    public class RemoteDocument
    {
        public string DocumentUrl { get; set; }

        public JToken Document { get; set; }

        public string ContextUrl { get; set; }

        public JToken Context { get; set; }
    }
}
