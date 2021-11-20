using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace WishAndGet.WebApp.Controllers
{
    [ApiController]
    [Route("[controller]")]
    public class SchemaController : ControllerBase
    {
        private readonly PageSchemaOrgGrabber grabber;
        private readonly SchemaOrgDataProcessor schemaOrgDataProcessor;

        public SchemaController(
            PageSchemaOrgGrabber grabber,
            SchemaOrgDataProcessor schemaOrgDataProcessor)
        {
            this.grabber = grabber;
            this.schemaOrgDataProcessor = schemaOrgDataProcessor;
        }

        [HttpPost("[action]")]
        public async Task<IEnumerable<JObject>> Grab([FromForm] string url, CancellationToken token = default)
        {
            var schemaRawData = await grabber.GrabAsync(url, token);

            var schemaObjectsMap = new Dictionary<string, JObject>(StringComparer.OrdinalIgnoreCase);
            foreach (var schemaObject in schemaRawData
                .SelectMany(data => schemaOrgDataProcessor.Flatten(data)))
            {
                var idValue = (string)schemaObject["id"];
                if (!string.IsNullOrEmpty(idValue))
                    schemaObjectsMap[idValue] = schemaObject;
            }

            var result = new HashSet<JObject>();
            foreach (var product in schemaObjectsMap.Values.Where(p => IsSchemaType((string)p["type"], "Product")))
            {
                var visitor = new JsonLdObjectVisitor(product, schemaObjectsMap.Values);
                visitor.Traverse(context => result.Add(context.Root));
            }

            return result.ToList();
        }

        bool IsSchemaType(string value, string type)
        {
            // todo: flatten method should normalize type automatically
            if (string.Equals(value, type, StringComparison.OrdinalIgnoreCase))
                return true;

            if (string.Equals(value, $"https://schema.org/{type}", StringComparison.OrdinalIgnoreCase))
                return true;

            return false;
        }
    }
}
