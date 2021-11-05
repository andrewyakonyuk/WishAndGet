using Newtonsoft.Json.Linq;

namespace WishAndGet
{
    public class JsonLdObjectVisitor
    {
        readonly JObject rootObject;
        readonly Dictionary<string, JObject> objectsMap;

        public JsonLdObjectVisitor(JObject rootObject, IEnumerable<JObject> other)
        {
            this.rootObject = rootObject ?? throw new ArgumentNullException(nameof(rootObject));
            this.objectsMap = new Dictionary<string, JObject>(StringComparer.OrdinalIgnoreCase);

            foreach (var item in other)
            {
                var idValue = (string)item["id"];
                if (!string.IsNullOrEmpty(idValue))
                    objectsMap[idValue] = item;
            }
        }

        public void Traverse(Action<JsonLdObjectVisitorContext> action)
        {
            TraverseCore(action, rootObject);
        }

        private void TraverseCore(Action<JsonLdObjectVisitorContext> action, JObject root)
        {
            foreach (var property in root)
            {
                void InvokeAction() => action(new JsonLdObjectVisitorContext(property.Key, property.Value, root));

                void HandleProperty(string idValue)
                {
                    JObject? relatedObject = null;
                    if (!string.IsNullOrEmpty(idValue))
                        relatedObject = objectsMap.GetValueOrDefault(idValue);
                    if (relatedObject == null)
                        InvokeAction();
                    else
                        TraverseCore(action, relatedObject);
                }

                if (property.Key == "id")
                {
                    InvokeAction();
                    continue;
                }

                switch (property.Value.Type)
                {
                    case JTokenType.String:
                        {
                            var idValue = (string)property.Value;
                            HandleProperty(idValue);
                            break;
                        }

                    case JTokenType.Object:
                        {
                            var idValue = (string)property.Value["id"];
                            HandleProperty(idValue);
                            break;
                        }
                    case JTokenType.Array:
                        {
                            foreach (var item in (JArray)property.Value)
                            {
                                if (item.Type == JTokenType.String)
                                {
                                    var idValue = item.Value<string>();
                                    HandleProperty(idValue);
                                }
                                else if (item.Type == JTokenType.Object)
                                {
                                    var idValue = (string)item["id"];
                                    HandleProperty(idValue);
                                }
                            }
                        }
                        break;
                    default:
                        InvokeAction();
                        break;
                }
            }
        }
    }

    public class JsonLdObjectVisitorContext
    {
        public JsonLdObjectVisitorContext(string name, JToken? value, JObject root)
        {
            Name = name ?? throw new ArgumentNullException(nameof(name));
            Value = value;
            Root = root ?? throw new ArgumentNullException(nameof(root));
        }

        public string Name { get; }

        public JToken? Value { get; }

        public JObject Root { get; }
    }
}
