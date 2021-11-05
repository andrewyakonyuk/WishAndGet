using Newtonsoft.Json.Linq;

namespace WishAndGet.Infrastructure.JsonLd
{
    /// <summary>http://json-ld.org/spec/latest/json-ld-api/#the-jsonldprocessor-interface
    /// 	</summary>
    /// <author>tristan</author>
    public class JsonLdProcessor
    {
        public static JArray Expand(JToken input, JsonLdOptions opts)
        {
            // 1)
            // TODO: look into java futures/promises

            // 2) verification of DOMString IRI
            bool isIriString = input.Type == JTokenType.String;
            if (isIriString)
            {
                bool hasColon = false;
                foreach (var c in (string) input)
                {
                    if (c == ':')
                    {
                        hasColon = true;
                    }
                    
                    if (!hasColon && (c == '{' || c == '['))
                    {
                        isIriString = false;
                        break;
                    }
                }
            }

            if (isIriString)
            {
                try
                {
                    RemoteDocument tmp = opts.documentLoader.LoadDocument((string)input);
                    input = tmp.Document;
                }
                catch (Exception e)
                {
                    // TODO: figure out how to deal with remote context
                    throw new JsonLdError(JsonLdError.Error.LoadingDocumentFailed, e.Message);
                }
                // if set the base in options should override the base iri in the
                // active context
                // thus only set this as the base iri if it's not already set in
                // options
                if (opts.GetBase() == null)
                {
                    opts.SetBase((string)input);
                }
            }
            // 3)
            Context activeCtx = new Context(opts);
            // 4)
            if (opts.GetExpandContext() != null)
            {
                JObject exCtx = opts.GetExpandContext();
                if (exCtx is JObject && ((IDictionary<string, JToken>)exCtx).ContainsKey("@context"
                    ))
                {
                    exCtx = (JObject)((IDictionary<string, JToken>)exCtx)["@context"];
                }
                activeCtx = activeCtx.Parse(exCtx);
            }
            // 5)
            // TODO: add support for getting a context from HTTP when content-type
            // is set to a jsonld compatable format
            // 6)
            JToken expanded = new JsonLdApi(opts).Expand(activeCtx, input);
            // final step of Expansion Algorithm
            if (expanded is JObject && ((IDictionary<string,JToken>)expanded).ContainsKey("@graph") && (
                (IDictionary<string, JToken>)expanded).Count == 1)
            {
                expanded = ((JObject)expanded)["@graph"];
            }
            else
            {
                if (expanded.IsNull())
                {
                    expanded = new JArray();
                }
            }
            // normalize to an array
            if (!(expanded is JArray))
            {
                JArray tmp = new JArray();
                tmp.Add(expanded);
                expanded = tmp;
            }
            return (JArray)expanded;
        }

        public static JArray Expand(JToken input)
        {
            return Expand(input, new JsonLdOptions(string.Empty));
        }

        public static JToken Flatten(JToken input, JToken context, JsonLdOptions opts)
        {
            // 2-6) NOTE: these are all the same steps as in expand
            JArray expanded = Expand(input, opts);
            // 7)
            if (context is JObject && ((IDictionary<string, JToken>)context).ContainsKey(
                "@context"))
            {
                context = context["@context"];
            }
            // 8) NOTE: blank node generation variables are members of JsonLdApi
            // 9) NOTE: the next block is the Flattening Algorithm described in
            // http://json-ld.org/spec/latest/json-ld-api/#flattening-algorithm
            // 1)
            JObject nodeMap = new JObject();
            nodeMap["@default"] = new JObject();
            // 2)
            new JsonLdApi(opts).GenerateNodeMap(expanded, nodeMap);
            // 3)
            JObject defaultGraph = (JObject)Collections.Remove
                (nodeMap, "@default");
            // 4)
            foreach (string graphName in nodeMap.GetKeys())
            {
                JObject graph = (JObject)nodeMap[graphName];
                // 4.1+4.2)
                JObject entry;
                if (!defaultGraph.ContainsKey(graphName))
                {
                    entry = new JObject();
                    entry["@id"] = graphName;
                    defaultGraph[graphName] = entry;
                }
                else
                {
                    entry = (JObject)defaultGraph[graphName];
                }
                // 4.3)
                // TODO: SPEC doesn't specify that this should only be added if it
                // doesn't exists
                if (!entry.ContainsKey("@graph"))
                {
                    entry["@graph"] = new JArray();
                }
                JArray keys = new JArray(graph.GetKeys());
                keys.SortInPlace();
                foreach (string id in keys)
                {
                    JObject node = (JObject)graph[id];
                    if (!(node.ContainsKey("@id") && node.Count == 1))
                    {
                        ((JArray)entry["@graph"]).Add(node);
                    }
                }
            }
            // 5)
            JArray flattened = new JArray();
            // 6)
            JArray keys_1 = new JArray(defaultGraph.GetKeys());
            keys_1.SortInPlace();
            foreach (string id_1 in keys_1)
            {
                JObject node = (JObject)defaultGraph[id_1
                    ];
                if (!(node.ContainsKey("@id") && node.Count == 1))
                {
                    flattened.Add(node);
                }
            }
            // 8)
            if (!context.IsNull() && !flattened.IsEmpty())
            {
                Context activeCtx = new Context(opts);
                activeCtx = activeCtx.Parse(context);
                // TODO: only instantiate one jsonldapi
                JToken compacted = new JsonLdApi(opts).Compact(activeCtx, null, flattened, opts.GetCompactArrays
                    ());
                if (!(compacted is JArray))
                {
                    JArray tmp = new JArray();
                    tmp.Add(compacted);
                    compacted = tmp;
                }
                string alias = activeCtx.CompactIri("@graph");
                JObject rval = activeCtx.Serialize();
                rval[alias] = compacted;
                return rval;
            }
            return flattened;
        }

        public static JToken Flatten(JToken input, JsonLdOptions opts)
        {
            return Flatten(input, null, opts);
        }
    }
}
