using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace WishAndGet.Infrastructure.JsonLd
{
    public static class JsonLdDocumentLoaderExtensions
    {
        public static RemoteDocument LoadDocument(this IJsonLdDocumentLoader documentLoader, string url)
        {
            return documentLoader.LoadDocumentAsync(url).AnyContext().GetAwaiter().GetResult();
        }
    }
}
