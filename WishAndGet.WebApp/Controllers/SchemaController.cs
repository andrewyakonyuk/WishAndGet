using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using WishAndGet.Infrastructure.JsonLd;

namespace WishAndGet.WebApp.Controllers
{
    [ApiController]
    [Route("[controller]")]
    public class SchemaController : ControllerBase
    {
        private readonly SchemaDataGrabber schemaGrabber;
        private readonly SchemaDataProcessor schemaProcessor;

        public SchemaController(
            SchemaDataGrabber schemaGrabber,
            SchemaDataProcessor schemaProcessor)
        {
            this.schemaGrabber = schemaGrabber;
            this.schemaProcessor = schemaProcessor;
        }

        [HttpPost("[action]")]
        public async Task<IEnumerable<JObject>> Grab([FromForm] string url, CancellationToken token = default)
        {
            var schemaRawData = await schemaGrabber.GrabRawAsync(url, token);
            var schemaObjectsMap = CreateSchemaObjectsMap(schemaRawData);

            var result = new HashSet<JObject>();
            foreach (var product in schemaObjectsMap.Values.Where(p => IsSchemaType((string)p["type"], "Product")))
            {
                var visitor = new JsonLdObjectVisitor(product, schemaObjectsMap.Values);
                visitor.Traverse(context => result.Add(context.Root));
            }

            return result.ToList();
        }

        private Dictionary<string, JObject> CreateSchemaObjectsMap(List<string> schemaRawData)
        {
            var schemaObjectsMap = new Dictionary<string, JObject>(StringComparer.OrdinalIgnoreCase);
            foreach (var schemaObject in schemaRawData
                .SelectMany(data => schemaProcessor.Flatten(data)))
            {
                var idValue = (string)schemaObject["id"];
                if (!string.IsNullOrEmpty(idValue))
                    schemaObjectsMap[idValue] = schemaObject;
            }

            return schemaObjectsMap;
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
