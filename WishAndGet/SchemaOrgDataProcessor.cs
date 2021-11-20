﻿using Newtonsoft.Json.Linq;
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
        private readonly IJsonLdDocumentLoader documentLoader;

        public SchemaOrgDataProcessor(IJsonLdDocumentLoader documentLoader)
        {
            this.documentLoader = documentLoader ?? throw new ArgumentNullException(nameof(documentLoader));
        }

        public IReadOnlyCollection<JObject> Flatten(string rawSchemaData)
        {
            var options = new JsonLdOptions
            {
                DocumentLoader = new CachedDocumentLoader(new SchemaDocumentLoader(documentLoader))
            };
            var remoteContext = JObject.Parse("{'@context':'https://schema.org/'}");
            var jsonData = JObject.Parse(rawSchemaData);
            var flattened = JsonLdProcessor.Flatten(jsonData, remoteContext, options);

            return flattened["@graph"].ToObject<List<JObject>>();
        }

        public class SchemaDocumentLoader : IJsonLdDocumentLoader
        {
            readonly static Uri schemaOrgUri = new("https://schema.org", UriKind.Absolute);
            readonly IJsonLdDocumentLoader documentLoader;

            public SchemaDocumentLoader(IJsonLdDocumentLoader documentLoader)
            {
                this.documentLoader = documentLoader ?? throw new ArgumentNullException(nameof(documentLoader));
            }

            public Task<RemoteDocument> LoadDocumentAsync(string url, CancellationToken token = default)
            {
                var uri = new Uri(url);
                if (uri.Host == schemaOrgUri.Host)
                    url = "https://schema.org/docs/jsonldcontext.json";

                return documentLoader.LoadDocumentAsync(url, token);
            }
        }

        public class CachedDocumentLoader : IJsonLdDocumentLoader
        {
            readonly IJsonLdDocumentLoader loader;
            readonly ConcurrentDictionary<string, RemoteDocument> cache = new();

            public CachedDocumentLoader(IJsonLdDocumentLoader loader)
            {
                this.loader = loader ?? throw new ArgumentNullException(nameof(loader));
            }

            public async Task<RemoteDocument> LoadDocumentAsync(string url, CancellationToken token = default)
            {
                if (cache.TryGetValue(url, out RemoteDocument result))
                    return result;

                var document = await loader.LoadDocumentAsync(url, token).ConfigureAwait(false);
                cache.TryAdd(url, document);

                return document;
            }
        }
    }
}
