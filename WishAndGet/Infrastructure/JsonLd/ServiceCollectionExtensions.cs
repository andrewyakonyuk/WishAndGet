using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace WishAndGet.Infrastructure.JsonLd
{
    public static class ServiceCollectionExtensions
    {
        private const string ACCEPT_HEADER = "application/ld+json, application/json;q=0.9, application/javascript;q=0.5, text/javascript;q=0.5, text/plain;q=0.2, */*;q=0.1";

        public static void AddJsonLd(this IServiceCollection services)
        {
            services.AddHttpClient("json-ld", (_, client) =>
            {
                client.DefaultRequestHeaders.Add("Accept", ACCEPT_HEADER);
            })
            .AddTypedClient<IJsonLdDocumentLoader, DefaultJsonLdDocumentLoader>();
        }
    }
}
