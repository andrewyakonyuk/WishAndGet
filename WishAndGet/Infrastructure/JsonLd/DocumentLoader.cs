using System;
using System.Collections;
using System.IO;
using System.Linq;
using WishAndGet.Infrastructure.JsonLd;
using System.Net;
using System.Collections.Generic;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Threading.Tasks;
using System.Runtime.InteropServices;
using Newtonsoft.Json.Linq;
using Newtonsoft.Json;
using System.Text;

namespace WishAndGet.Infrastructure.JsonLd
{
    public class DocumentLoader
    {
        enum JsonLDContentType
        {
            JsonLD,
            PlainJson,
            Other
        }

        JsonLDContentType GetJsonLDContentType(string contentTypeStr)
        {
            switch (contentTypeStr)
            {
                case "application/ld+json":
                    return JsonLDContentType.JsonLD;
                // From RFC 6839, it looks like plain JSON is content type application/json and any MediaType ending in "+json".
                case "application/json":
                case string type when type.EndsWith("+json"):
                    return JsonLDContentType.PlainJson;
                default:
                    return JsonLDContentType.Other;
            }
        }

        public virtual RemoteDocument LoadDocument(string url)
        {
            return LoadDocumentAsync(url).ConfigureAwait(false).GetAwaiter().GetResult();
        }

        public virtual async Task<RemoteDocument> LoadDocumentAsync(string url)
        {
            RemoteDocument doc = new() { DocumentUrl = url };
            try
            {
                using (HttpResponseMessage response = await LDHttpClient.FetchAsync(url).ConfigureAwait(false))
                {

                    var code = (int)response.StatusCode;

                    if (code >= 400)
                    {
                        throw new JsonLdError(JsonLdError.Error.LoadingDocumentFailed, $"HTTP {code} {url}");
                    }

                    var finalUrl = response.RequestMessage.RequestUri.ToString();

                    var contentType = GetJsonLDContentType(response.Content.Headers.ContentType.MediaType);

                    if (contentType == JsonLDContentType.Other)
                    {
                        throw new JsonLdError(JsonLdError.Error.LoadingDocumentFailed, url);
                    }

                    // For plain JSON, see if there's a context document linked in the HTTP response headers.
                    if (contentType == JsonLDContentType.PlainJson && response.Headers.TryGetValues("Link", out var linkHeaders))
                    {
                        linkHeaders = linkHeaders.SelectMany((h) => h.Split(",".ToCharArray()))
                                                    .Select(h => h.Trim()).ToArray();
                        IEnumerable<string> linkedContexts = linkHeaders.Where(v => v.EndsWith("rel=\"http://www.w3.org/ns/json-ld#context\""));
                        if (linkedContexts.Count() > 1)
                        {
                            throw new JsonLdError(JsonLdError.Error.MultipleContextLinkHeaders);
                        }
                        string header = linkedContexts.First();
                        string linkedUrl = header.Substring(1, header.IndexOf(">") - 1);
                        string resolvedUrl = URL.Resolve(finalUrl, linkedUrl);
                        var remoteContext = await LoadDocumentAsync(resolvedUrl).ConfigureAwait(false);
                        doc.ContextUrl = remoteContext.DocumentUrl;
                        doc.Context = remoteContext.Document;
                    }

                    Stream stream = await response.Content.ReadAsStreamAsync().ConfigureAwait(false);

                    doc.DocumentUrl = finalUrl;
                    doc.Document = JToken.ReadFrom(new JsonTextReader(new StreamReader(stream, Encoding.Unicode)));
                }
            }
            catch (JsonLdError)
            {
                throw;
            }
            catch (Exception exception)
            {
                throw new JsonLdError(JsonLdError.Error.LoadingDocumentFailed, url, exception);
            }
            return doc;
        }
    }
}
