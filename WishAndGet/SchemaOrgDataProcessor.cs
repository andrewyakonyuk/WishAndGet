using Newtonsoft.Json.Linq;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using WishAndGet.Infrastructure.JsonLd;

namespace WishAndGet
{
    public class SchemaOrgDataProcessor
    {
        public IReadOnlyCollection<JObject> Flatten(string rawSchemaData)
        {
            var options = new JsonLdOptions
            {
                documentLoader = new CachedDocumentLoader(new SchemaDocumentLoader())
            };
            var remoteContext = JObject.Parse("{'@context':'https://schema.org/'}");
            var jsonData = JObject.Parse(rawSchemaData);
            var flattened = JsonLdProcessor.Flatten(jsonData, remoteContext, options);

            return flattened["@graph"].ToObject<List<JObject>>();
        }

        public class SchemaDocumentLoader : DocumentLoader
        {
            readonly static Uri schemaOrgUri = new("https://schema.org", UriKind.Absolute);

            public override async Task<RemoteDocument> LoadDocumentAsync(string url)
            {
                var uri = new Uri(url);
                if (uri.Host == schemaOrgUri.Host)
                    url = "https://schema.org/docs/jsonldcontext.json";

                return await base.LoadDocumentAsync(url).ConfigureAwait(false);
            }
        }

        public class CachedDocumentLoader : DocumentLoader
        {
            readonly DocumentLoader loader;
            readonly ConcurrentDictionary<string, RemoteDocument> cache = new();

            public CachedDocumentLoader(DocumentLoader loader)
            {
                this.loader = loader ?? throw new ArgumentNullException(nameof(loader));
            }

            public override async Task<RemoteDocument> LoadDocumentAsync(string url)
            {
                if (cache.TryGetValue(url, out RemoteDocument result))
                    return result;

                var document = await loader.LoadDocumentAsync(url).ConfigureAwait(false);
                cache.TryAdd(url, document);

                return document;
            }
        }
    }
}
