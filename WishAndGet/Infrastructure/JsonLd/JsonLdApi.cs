using Newtonsoft.Json.Linq;

namespace WishAndGet.Infrastructure.JsonLd
{
    public class JsonLdApi
    {
        internal JsonLdOptions opts;

        internal JToken value = null;

        internal Context context = null;

        public JsonLdApi()
        {
            opts = new JsonLdOptions(string.Empty);
        }

        public JsonLdApi(JToken input, JsonLdOptions opts)
        {
            Initialize(input, null, opts);
        }

        public JsonLdApi(JToken input, JToken context, JsonLdOptions opts)
        {
            Initialize(input, null, opts);
        }

        public JsonLdApi(JsonLdOptions opts)
        {
            if (opts == null)
            {
                opts = new JsonLdOptions(string.Empty);
            }
            else
            {
                this.opts = opts;
            }
        }

        /// <exception cref="JsonLdError"></exception>
        private void Initialize(JToken input, JToken context, JsonLdOptions opts)
        {
            // set option defaults (TODO: clone?)
            // NOTE: sane defaults should be set in JsonLdOptions constructor
            this.opts = opts;
            if (input is JArray || input is JObject)
            {
                value = JsonLdUtils.Clone(input);
            }
            // TODO: string/IO input
            this.context = new Context(opts);
            if (!context.IsNull())
            {
                this.context = this.context.Parse(context);
            }
        }

        /// <summary>
        /// Compaction Algorithm
        /// http://json-ld.org/spec/latest/json-ld-api/#compaction-algorithm
        /// </summary>
        /// <param name="activeCtx"></param>
        /// <param name="activeProperty"></param>
        /// <param name="element"></param>
        /// <param name="compactArrays"></param>
        /// <returns></returns>
        /// <exception cref="JsonLD.Core.JsonLdError"></exception>
        public virtual JToken Compact(Context activeCtx, string activeProperty, JToken element
            , bool compactArrays)
        {
            // 2)
            if (element is JArray)
            {
                // 2.1)
                JArray result = new JArray();
                // 2.2)
                foreach (JToken item in element)
                {
                    // 2.2.1)
                    JToken compactedItem = Compact(activeCtx, activeProperty, item, compactArrays);
                    // 2.2.2)
                    if (!compactedItem.IsNull())
                    {
                        result.Add(compactedItem);
                    }
                }
                // 2.3)
                if (compactArrays && result.Count == 1 && activeCtx.GetContainer(activeProperty)
                    == null)
                {
                    return result[0];
                }
                // 2.4)
                return result;
            }
            // 3)
            if (element is JObject)
            {
                // access helper
                IDictionary<string, JToken> elem = (IDictionary<string, JToken>)element;
                // 4
                if (elem.ContainsKey("@value") || elem.ContainsKey("@id"))
                {
                    JToken compactedValue = activeCtx.CompactValue(activeProperty, (JObject)element);
                    if (!(compactedValue is JObject || compactedValue is JArray))
                    {
                        return compactedValue;
                    }
                }
                // 5)
                bool insideReverse = ("@reverse".Equals(activeProperty));
                // 6)
                JObject result = new JObject();
                // 7)
                JArray keys = new JArray(element.GetKeys());
                keys.SortInPlace();
                foreach (string expandedProperty in keys)
                {
                    JToken expandedValue = elem[expandedProperty];
                    // 7.1)
                    if ("@id".Equals(expandedProperty) || "@type".Equals(expandedProperty))
                    {
                        JToken compactedValue;
                        // 7.1.1)
                        if (expandedValue.Type == JTokenType.String)
                        {
                            compactedValue = activeCtx.CompactIri((string)expandedValue, "@type".Equals(expandedProperty
                                ));
                        }
                        else
                        {
                            // 7.1.2)
                            JArray types = new JArray();
                            // 7.1.2.2)
                            foreach (string expandedType in (JArray)expandedValue)
                            {
                                types.Add(activeCtx.CompactIri(expandedType, true));
                            }
                            // 7.1.2.3)
                            if (types.Count == 1)
                            {
                                compactedValue = types[0];
                            }
                            else
                            {
                                compactedValue = types;
                            }
                        }
                        // 7.1.3)
                        string alias = activeCtx.CompactIri(expandedProperty, true);
                        // 7.1.4)
                        result[alias] = compactedValue;
                        continue;
                    }
                    // TODO: old add value code, see if it's still relevant?
                    // addValue(rval, alias, compactedValue,
                    // isArray(compactedValue)
                    // && ((List<Object>) expandedValue).size() == 0);
                    // 7.2)
                    if ("@reverse".Equals(expandedProperty))
                    {
                        // 7.2.1)
                        JObject compactedValue = (JObject)Compact(activeCtx, "@reverse", expandedValue, compactArrays);
                        // 7.2.2)
                        List<string> properties = new List<string>(compactedValue.GetKeys());
                        foreach (string property in properties)
                        {
                            JToken value = compactedValue[property];
                            // 7.2.2.1)
                            if (activeCtx.IsReverseProperty(property))
                            {
                                // 7.2.2.1.1)
                                if (("@set".Equals(activeCtx.GetContainer(property)) || !compactArrays) && !(value
                                     is JArray))
                                {
                                    JArray tmp = new JArray();
                                    tmp.Add(value);
                                    result[property] = tmp;
                                }
                                // 7.2.2.1.2)
                                if (!result.ContainsKey(property))
                                {
                                    result[property] = value;
                                }
                                else
                                {
                                    // 7.2.2.1.3)
                                    if (!(result[property] is JArray))
                                    {
                                        JArray tmp = new JArray();
                                        tmp.Add(result[property]);
                                        result[property] = tmp;
                                    }
                                    if (value is JArray)
                                    {
                                        Collections.AddAll(((JArray)result[property]), (JArray)value
                                            );
                                    }
                                    else
                                    {
                                        ((JArray)result[property]).Add(value);
                                    }
                                }
                                // 7.2.2.1.4) TODO: this doesn't seem safe (i.e.
                                // modifying the map being used to drive the loop)!
                                Collections.Remove(compactedValue, property);
                            }
                        }
                        // 7.2.3)
                        if (compactedValue.Count != 0)
                        {
                            // 7.2.3.1)
                            string alias = activeCtx.CompactIri("@reverse", true);
                            // 7.2.3.2)
                            result[alias] = compactedValue;
                        }
                        // 7.2.4)
                        continue;
                    }
                    // 7.3)
                    if ("@index".Equals(expandedProperty) && "@index".Equals(activeCtx.GetContainer(activeProperty
                        )))
                    {
                        continue;
                    }
                    else
                    {
                        // 7.4)
                        if ("@index".Equals(expandedProperty) || "@value".Equals(expandedProperty) || "@language"
                            .Equals(expandedProperty))
                        {
                            // 7.4.1)
                            string alias = activeCtx.CompactIri(expandedProperty, true);
                            // 7.4.2)
                            result[alias] = expandedValue;
                            continue;
                        }
                    }
                    // NOTE: expanded value must be an array due to expansion
                    // algorithm.
                    // 7.5)
                    if (((JArray)expandedValue).Count == 0)
                    {
                        // 7.5.1)
                        string itemActiveProperty = activeCtx.CompactIri(expandedProperty, expandedValue,
                            true, insideReverse);
                        // 7.5.2)
                        if (!result.ContainsKey(itemActiveProperty))
                        {
                            result[itemActiveProperty] = new JArray();
                        }
                        else
                        {
                            JToken value = result[itemActiveProperty];
                            if (!(value is JArray))
                            {
                                JArray tmp = new JArray();
                                tmp.Add(value);
                                result[itemActiveProperty] = tmp;
                            }
                        }
                    }
                    // 7.6)
                    foreach (JToken expandedItem in (JArray)expandedValue)
                    {
                        // 7.6.1)
                        string itemActiveProperty = activeCtx.CompactIri(expandedProperty, expandedItem,
                            true, insideReverse);
                        // 7.6.2)
                        string container = activeCtx.GetContainer(itemActiveProperty);
                        // get @list value if appropriate
                        bool isList = (expandedItem is JObject && ((IDictionary<string, JToken>)expandedItem
                            ).ContainsKey("@list"));
                        JToken list = null;
                        if (isList)
                        {
                            list = ((IDictionary<string, JToken>)expandedItem)["@list"];
                        }
                        // 7.6.3)
                        JToken compactedItem = Compact(activeCtx, itemActiveProperty, isList ? list : expandedItem
                            , compactArrays);
                        // 7.6.4)
                        if (isList)
                        {
                            // 7.6.4.1)
                            if (!(compactedItem is JArray))
                            {
                                JArray tmp = new JArray();
                                tmp.Add(compactedItem);
                                compactedItem = tmp;
                            }
                            // 7.6.4.2)
                            if (!"@list".Equals(container))
                            {
                                // 7.6.4.2.1)
                                JObject wrapper = new JObject();
                                // TODO: SPEC: no mention of vocab = true
                                wrapper[activeCtx.CompactIri("@list", true)] = compactedItem;
                                compactedItem = wrapper;
                                // 7.6.4.2.2)
                                if (((IDictionary<string, JToken>)expandedItem).ContainsKey("@index"))
                                {
                                    ((IDictionary<string, JToken>)compactedItem)[activeCtx.CompactIri("@index", true)
                                        ] = ((IDictionary<string, JToken>)expandedItem)["@index"];
                                }
                            }
                            else
                            {
                                // TODO: SPEC: no mention of vocab =
                                // true
                                // 7.6.4.3)
                                if (result.ContainsKey(itemActiveProperty))
                                {
                                    throw new JsonLdError(JsonLdError.Error.CompactionToListOfLists, "There cannot be two list objects associated with an active property that has a container mapping"
                                        );
                                }
                            }
                        }
                        // 7.6.5)
                        if ("@language".Equals(container) || "@index".Equals(container))
                        {
                            // 7.6.5.1)
                            JObject mapObject;
                            if (result.ContainsKey(itemActiveProperty))
                            {
                                mapObject = (JObject)result[itemActiveProperty];
                            }
                            else
                            {
                                mapObject = new JObject();
                                result[itemActiveProperty] = mapObject;
                            }
                            // 7.6.5.2)
                            if ("@language".Equals(container) && (compactedItem is JObject && ((IDictionary
                                <string, JToken>)compactedItem).ContainsKey("@value")))
                            {
                                compactedItem = compactedItem["@value"];
                            }
                            // 7.6.5.3)
                            string mapKey = (string)expandedItem[container];
                            // 7.6.5.4)
                            if (!mapObject.ContainsKey(mapKey))
                            {
                                mapObject[mapKey] = compactedItem;
                            }
                            else
                            {
                                JArray tmp;
                                if (!(mapObject[mapKey] is JArray))
                                {
                                    tmp = new JArray();
                                    tmp.Add(mapObject[mapKey]);
                                    mapObject[mapKey] = tmp;
                                }
                                else
                                {
                                    tmp = (JArray)mapObject[mapKey];
                                }
                                tmp.Add(compactedItem);
                            }
                        }
                        else
                        {
                            // 7.6.6)
                            // 7.6.6.1)
                            bool check = (!compactArrays || "@set".Equals(container) || "@list".Equals(container
                                ) || "@list".Equals(expandedProperty) || "@graph".Equals(expandedProperty)) && (
                                !(compactedItem is JArray));
                            if (check)
                            {
                                JArray tmp = new JArray();
                                tmp.Add(compactedItem);
                                compactedItem = tmp;
                            }
                            // 7.6.6.2)
                            if (!result.ContainsKey(itemActiveProperty))
                            {
                                result[itemActiveProperty] = compactedItem;
                            }
                            else
                            {
                                if (!(result[itemActiveProperty] is JArray))
                                {
                                    JArray tmp = new JArray();
                                    tmp.Add(result[itemActiveProperty]);
                                    result[itemActiveProperty] = tmp;
                                }
                                if (compactedItem is JArray)
                                {
                                    Collections.AddAll(((JArray)result[itemActiveProperty]), (JArray)compactedItem);
                                }
                                else
                                {
                                    ((JArray)result[itemActiveProperty]).Add(compactedItem);
                                }
                            }
                        }
                    }
                }
                // 8)
                return result;
            }
            // 2)
            return element;
        }

        /// <exception cref="JsonLD.Core.JsonLdError"></exception>
        public virtual JToken Compact(Context activeCtx, string activeProperty, JToken element
            )
        {
            return Compact(activeCtx, activeProperty, element, true);
        }

        /// <summary>
        /// Expansion Algorithm
        /// http://json-ld.org/spec/latest/json-ld-api/#expansion-algorithm
        /// </summary>
        /// <param name="activeCtx"></param>
        /// <param name="activeProperty"></param>
        /// <param name="element"></param>
        /// <returns></returns>
        /// <exception cref="JsonLdError">JsonLdError</exception>
        /// <exception cref="JsonLdError"></exception>
        public virtual JToken Expand(Context activeCtx, string activeProperty, JToken element)
        {
            // 1)
            if (element.IsNull())
            {
                return null;
            }
            // 3)
            if (element is JArray)
            {
                // 3.1)
                JArray result = new JArray();
                // 3.2)
                foreach (JToken item in (JArray)element)
                {
                    // 3.2.1)
                    JToken v = Expand(activeCtx, activeProperty, item);
                    // 3.2.2)
                    if (("@list".Equals(activeProperty) || "@list".Equals(activeCtx.GetContainer(activeProperty
                        ))) && (v is JArray || (v is JObject && ((IDictionary<string, JToken>)v).ContainsKey
                        ("@list"))))
                    {
                        throw new JsonLdError(JsonLdError.Error.ListOfLists, "lists of lists are not permitted."
                            );
                    }
                    else
                    {
                        // 3.2.3)
                        if (!v.IsNull())
                        {
                            if (v is JArray)
                            {
                                Collections.AddAll(result, (JArray)v);
                            }
                            else
                            {
                                result.Add(v);
                            }
                        }
                    }
                }
                // 3.3)
                return result;
            }
            else
            {
                // 4)
                if (element is JObject)
                {
                    // access helper
                    IDictionary<string, JToken> elem = (JObject)element;
                    // 5)
                    if (elem.ContainsKey("@context"))
                    {
                        activeCtx = activeCtx.Parse(elem["@context"]);
                    }
                    // 6)
                    JObject result = new JObject();
                    // 7)
                    JArray keys = new JArray(element.GetKeys());
                    keys.SortInPlace();
                    foreach (string key in keys)
                    {
                        JToken value = elem[key];
                        // 7.1)
                        if (key.Equals("@context"))
                        {
                            continue;
                        }
                        // 7.2)
                        string expandedProperty = activeCtx.ExpandIri(key, false, true, null, null);
                        JToken expandedValue = null;
                        // 7.3)
                        if (expandedProperty == null || (!expandedProperty.Contains(":") && !JsonLdUtils.IsKeyword
                            (expandedProperty)))
                        {
                            continue;
                        }
                        // 7.4)
                        if (JsonLdUtils.IsKeyword(expandedProperty))
                        {
                            // 7.4.1)
                            if ("@reverse".Equals(activeProperty))
                            {
                                throw new JsonLdError(JsonLdError.Error.InvalidReversePropertyMap, "a keyword cannot be used as a @reverse propery"
                                    );
                            }
                            // 7.4.2)
                            if (result.ContainsKey(expandedProperty))
                            {
                                throw new JsonLdError(JsonLdError.Error.CollidingKeywords, expandedProperty + " already exists in result"
                                    );
                            }
                            // 7.4.3)
                            if ("@id".Equals(expandedProperty))
                            {
                                if (!(value.Type == JTokenType.String))
                                {
                                    throw new JsonLdError(JsonLdError.Error.InvalidIdValue, "value of @id must be a string"
                                        );
                                }
                                expandedValue = activeCtx.ExpandIri((string)value, true, false, null, null);
                            }
                            else
                            {
                                // 7.4.4)
                                if ("@type".Equals(expandedProperty))
                                {
                                    if (value is JArray)
                                    {
                                        expandedValue = new JArray();
                                        foreach (JToken v in (JArray)value)
                                        {
                                            if (v.Type != JTokenType.String)
                                            {
                                                throw new JsonLdError(JsonLdError.Error.InvalidTypeValue, "@type value must be a string or array of strings"
                                                    );
                                            }
                                            ((JArray)expandedValue).Add(activeCtx.ExpandIri((string)v, true, true, null
                                                , null));
                                        }
                                    }
                                    else
                                    {
                                        if (value.Type == JTokenType.String)
                                        {
                                            expandedValue = activeCtx.ExpandIri((string)value, true, true, null, null);
                                        }
                                        else
                                        {
                                            // TODO: SPEC: no mention of empty map check
                                            if (value is JObject)
                                            {
                                                if (((JObject)value).Count != 0)
                                                {
                                                    throw new JsonLdError(JsonLdError.Error.InvalidTypeValue, "@type value must be a an empty object for framing"
                                                        );
                                                }
                                                expandedValue = value;
                                            }
                                            else
                                            {
                                                throw new JsonLdError(JsonLdError.Error.InvalidTypeValue, "@type value must be a string or array of strings"
                                                    );
                                            }
                                        }
                                    }
                                }
                                else
                                {
                                    // 7.4.5)
                                    if ("@graph".Equals(expandedProperty))
                                    {
                                        expandedValue = Expand(activeCtx, "@graph", value);
                                    }
                                    else
                                    {
                                        // 7.4.6)
                                        if ("@value".Equals(expandedProperty))
                                        {
                                            if (!value.IsNull() && (value is JObject || value is JArray))
                                            {
                                                throw new JsonLdError(JsonLdError.Error.InvalidValueObjectValue, "value of " + expandedProperty
                                                     + " must be a scalar or null");
                                            }
                                            expandedValue = value;
                                            if (expandedValue.IsNull())
                                            {
                                                result["@value"] = null;
                                                continue;
                                            }
                                        }
                                        else
                                        {
                                            // 7.4.7)
                                            if ("@language".Equals(expandedProperty))
                                            {
                                                if (!(value.Type == JTokenType.String))
                                                {
                                                    throw new JsonLdError(JsonLdError.Error.InvalidLanguageTaggedString, "Value of " 
                                                        + expandedProperty + " must be a string");
                                                }
                                                expandedValue = ((string)value).ToLower();
                                            }
                                            else
                                            {
                                                // 7.4.8)
                                                if ("@index".Equals(expandedProperty))
                                                {
                                                    if (!(value.Type == JTokenType.String))
                                                    {
                                                        throw new JsonLdError(JsonLdError.Error.InvalidIndexValue, "Value of " + expandedProperty
                                                             + " must be a string");
                                                    }
                                                    expandedValue = value;
                                                }
                                                else
                                                {
                                                    // 7.4.9)
                                                    if ("@list".Equals(expandedProperty))
                                                    {
                                                        // 7.4.9.1)
                                                        if (activeProperty == null || "@graph".Equals(activeProperty))
                                                        {
                                                            continue;
                                                        }
                                                        // 7.4.9.2)
                                                        expandedValue = Expand(activeCtx, activeProperty, value);
                                                        // NOTE: step not in the spec yet
                                                        if (!(expandedValue is JArray))
                                                        {
                                                            JArray tmp = new JArray();
                                                            tmp.Add(expandedValue);
                                                            expandedValue = tmp;
                                                        }
                                                        // 7.4.9.3)
                                                        foreach (JToken o in (JArray)expandedValue)
                                                        {
                                                            if (o is JObject && ((JObject)o).ContainsKey("@list"))
                                                            {
                                                                throw new JsonLdError(JsonLdError.Error.ListOfLists, "A list may not contain another list"
                                                                    );
                                                            }
                                                        }
                                                    }
                                                    else
                                                    {
                                                        // 7.4.10)
                                                        if ("@set".Equals(expandedProperty))
                                                        {
                                                            expandedValue = Expand(activeCtx, activeProperty, value);
                                                        }
                                                        else
                                                        {
                                                            // 7.4.11)
                                                            if ("@reverse".Equals(expandedProperty))
                                                            {
                                                                if (!(value is JObject))
                                                                {
                                                                    throw new JsonLdError(JsonLdError.Error.InvalidReverseValue, "@reverse value must be an object"
                                                                        );
                                                                }
                                                                // 7.4.11.1)
                                                                expandedValue = Expand(activeCtx, "@reverse", value);
                                                                // NOTE: algorithm assumes the result is a map
                                                                // 7.4.11.2)
                                                                if (((IDictionary<string, JToken>)expandedValue).ContainsKey("@reverse"))
                                                                {
                                                                    JObject reverse = (JObject)((JObject)expandedValue)["@reverse"];
                                                                    foreach (string property in reverse.GetKeys())
                                                                    {
                                                                        JToken item = reverse[property];
                                                                        // 7.4.11.2.1)
                                                                        if (!result.ContainsKey(property))
                                                                        {
                                                                            result[property] = new JArray();
                                                                        }
                                                                        // 7.4.11.2.2)
                                                                        if (item is JArray)
                                                                        {
                                                                            Collections.AddAll((JArray)result[property], (JArray)item);
                                                                        }
                                                                        else
                                                                        {
                                                                            ((JArray)result[property]).Add(item);
                                                                        }
                                                                    }
                                                                }
                                                                // 7.4.11.3)
                                                                if (((JObject)expandedValue).Count > (((JObject)expandedValue).ContainsKey("@reverse") ? 1 : 0))
                                                                {
                                                                    // 7.4.11.3.1)
                                                                    if (!result.ContainsKey("@reverse"))
                                                                    {
                                                                        result["@reverse"] = new JObject();
                                                                    }
                                                                    // 7.4.11.3.2)
                                                                    JObject reverseMap = (JObject)result["@reverse"];
                                                                    // 7.4.11.3.3)
                                                                    foreach (string property in expandedValue.GetKeys())
                                                                    {
                                                                        if ("@reverse".Equals(property))
                                                                        {
                                                                            continue;
                                                                        }
                                                                        // 7.4.11.3.3.1)
                                                                        JArray items = (JArray)((JObject)expandedValue)[property];
                                                                        foreach (JToken item in items)
                                                                        {
                                                                            // 7.4.11.3.3.1.1)
                                                                            if (item is JObject && (((JObject)item).ContainsKey("@value") || ((JObject)item).ContainsKey("@list")))
                                                                            {
                                                                                throw new JsonLdError(JsonLdError.Error.InvalidReversePropertyValue);
                                                                            }
                                                                            // 7.4.11.3.3.1.2)
                                                                            if (!reverseMap.ContainsKey(property))
                                                                            {
                                                                                reverseMap[property] = new JArray();
                                                                            }
                                                                            // 7.4.11.3.3.1.3)
                                                                            ((JArray)reverseMap[property]).Add(item);
                                                                        }
                                                                    }
                                                                }
                                                                // 7.4.11.4)
                                                                continue;
                                                            }
                                                            else
                                                            {
                                                                // TODO: SPEC no mention of @explicit etc in spec
                                                                if ("@explicit".Equals(expandedProperty) || "@default".Equals(expandedProperty) ||
                                                                     "@embed".Equals(expandedProperty) || "@embedChildren".Equals(expandedProperty) 
                                                                    || "@omitDefault".Equals(expandedProperty))
                                                                {
                                                                    expandedValue = Expand(activeCtx, expandedProperty, value);
                                                                }
                                                            }
                                                        }
                                                    }
                                                }
                                            }
                                        }
                                    }
                                }
                            }
                            // 7.4.12)
                            if (!expandedValue.IsNull())
                            {
                                result[expandedProperty] = expandedValue;
                            }
                            // 7.4.13)
                            continue;
                        }
                        else
                        {
                            // 7.5
                            if ("@language".Equals(activeCtx.GetContainer(key)) && value is JObject)
                            {
                                // 7.5.1)
                                expandedValue = new JArray();
                                // 7.5.2)
                                foreach (string language in value.GetKeys())
                                {
                                    JToken languageValue = ((IDictionary<string, JToken>)value)[language];
                                    // 7.5.2.1)
                                    if (!(languageValue is JArray))
                                    {
                                        JToken tmp = languageValue;
                                        languageValue = new JArray();
                                        ((JArray)languageValue).Add(tmp);
                                    }
                                    // 7.5.2.2)
                                    foreach (JToken item in (JArray)languageValue)
                                    {
                                        // 7.5.2.2.1)
                                        if (!(item.Type == JTokenType.String))
                                        {
                                            throw new JsonLdError(JsonLdError.Error.InvalidLanguageMapValue, "Expected " + item
                                                .ToString() + " to be a string");
                                        }
                                        // 7.5.2.2.2)
                                        JObject tmp = new JObject();
                                        tmp["@value"] = item;
                                        tmp["@language"] = language.ToLower();
                                        ((JArray)expandedValue).Add(tmp);
                                    }
                                }
                            }
                            else
                            {
                                // 7.6)
                                if ("@index".Equals(activeCtx.GetContainer(key)) && value is JObject)
                                {
                                    // 7.6.1)
                                    expandedValue = new JArray();
                                    // 7.6.2)
                                    JArray indexKeys = new JArray(value.GetKeys());
                                    indexKeys.SortInPlace();
                                    foreach (string index in indexKeys)
                                    {
                                        JToken indexValue = ((JObject)value)[index];
                                        // 7.6.2.1)
                                        if (!(indexValue is JArray))
                                        {
                                            JToken tmp = indexValue;
                                            indexValue = new JArray();
                                            ((JArray)indexValue).Add(tmp);
                                        }
                                        // 7.6.2.2)
                                        indexValue = Expand(activeCtx, key, indexValue);
                                        // 7.6.2.3)
                                        foreach (JObject item in (JArray)indexValue)
                                        {
                                            // 7.6.2.3.1)
                                            if (!item.ContainsKey("@index"))
                                            {
                                                item["@index"] = index;
                                            }
                                            // 7.6.2.3.2)
                                            ((JArray)expandedValue).Add(item);
                                        }
                                    }
                                }
                                else
                                {
                                    // 7.7)
                                    expandedValue = Expand(activeCtx, key, value);
                                }
                            }
                        }
                        // 7.8)
                        if (expandedValue.IsNull())
                        {
                            continue;
                        }
                        // 7.9)
                        if ("@list".Equals(activeCtx.GetContainer(key)))
                        {
                            if (!(expandedValue is JObject) || !((JObject)expandedValue).ContainsKey("@list"))
                            {
                                JToken tmp = expandedValue;
                                if (!(tmp is JArray))
                                {
                                    tmp = new JArray();
                                    ((JArray)tmp).Add(expandedValue);
                                }
                                expandedValue = new JObject();
                                ((JObject)expandedValue)["@list"] = tmp;
                            }
                        }
                        // 7.10)
                        if (activeCtx.IsReverseProperty(key))
                        {
                            // 7.10.1)
                            if (!result.ContainsKey("@reverse"))
                            {
                                result["@reverse"] = new JObject();
                            }
                            // 7.10.2)
                            JObject reverseMap = (JObject)result["@reverse"];
                            // 7.10.3)
                            if (!(expandedValue is JArray))
                            {
                                JToken tmp = expandedValue;
                                expandedValue = new JArray();
                                ((JArray)expandedValue).Add(tmp);
                            }
                            // 7.10.4)
                            foreach (JToken item in (JArray)expandedValue)
                            {
                                // 7.10.4.1)
                                if (item is JObject && (((JObject)item).ContainsKey("@value") || ((JObject)item).ContainsKey("@list")))
                                {
                                    throw new JsonLdError(JsonLdError.Error.InvalidReversePropertyValue);
                                }
                                // 7.10.4.2)
                                if (!reverseMap.ContainsKey(expandedProperty))
                                {
                                    reverseMap[expandedProperty] = new JArray();
                                }
                                // 7.10.4.3)
                                if (item is JArray)
                                {
                                    Collections.AddAll((JArray)reverseMap[expandedProperty], (JArray)item);
                                }
                                else
                                {
                                    ((JArray)reverseMap[expandedProperty]).Add(item);
                                }
                            }
                        }
                        else
                        {
                            // 7.11)
                            // 7.11.1)
                            if (!result.ContainsKey(expandedProperty))
                            {
                                result[expandedProperty] = new JArray();
                            }
                            // 7.11.2)
                            if (expandedValue is JArray)
                            {
                                Collections.AddAll((JArray)result[expandedProperty], (JArray)expandedValue);
                            }
                            else
                            {
                                ((JArray)result[expandedProperty]).Add(expandedValue);
                            }
                        }
                    }
                    // 8)
                    if (result.ContainsKey("@value"))
                    {
                        // 8.1)
                        // TODO: is this method faster than just using containsKey for
                        // each?
                        ICollection<string> keySet = new HashSet<string>(result.GetKeys());
                        keySet.Remove("@value");
                        keySet.Remove("@index");
                        bool langremoved = keySet.Remove("@language");
                        bool typeremoved = keySet.Remove("@type");
                        if ((langremoved && typeremoved) || !keySet.IsEmpty())
                        {
                            throw new JsonLdError(JsonLdError.Error.InvalidValueObject, "value object has unknown keys"
                                );
                        }
                        // 8.2)
                        JToken rval = result["@value"];
                        if (rval.IsNull())
                        {
                            // nothing else is possible with result if we set it to
                            // null, so simply return it
                            return null;
                        }
                        // 8.3)
                        if (!(rval.Type == JTokenType.String) && result.ContainsKey("@language"))
                        {
                            throw new JsonLdError(JsonLdError.Error.InvalidLanguageTaggedValue, "when @language is used, @value must be a string"
                                );
                        }
                        else
                        {
                            // 8.4)
                            if (result.ContainsKey("@type"))
                            {
                                // TODO: is this enough for "is an IRI"
                                if (!(result["@type"].Type == JTokenType.String) || ((string)result["@type"]).StartsWith("_:") ||
                                     !((string)result["@type"]).Contains(":"))
                                {
                                    throw new JsonLdError(JsonLdError.Error.InvalidTypedValue, "value of @type must be an IRI"
                                        );
                                }
                            }
                        }
                    }
                    else
                    {
                        // 9)
                        if (result.ContainsKey("@type"))
                        {
                            JToken rtype = result["@type"];
                            if (!(rtype is JArray))
                            {
                                JArray tmp = new JArray();
                                tmp.Add(rtype);
                                result["@type"] = tmp;
                            }
                        }
                        else
                        {
                            // 10)
                            if (result.ContainsKey("@set") || result.ContainsKey("@list"))
                            {
                                // 10.1)
                                if (result.Count > (result.ContainsKey("@index") ? 2 : 1))
                                {
                                    throw new JsonLdError(JsonLdError.Error.InvalidSetOrListObject, "@set or @list may only contain @index"
                                        );
                                }
                                // 10.2)
                                if (result.ContainsKey("@set"))
                                {
                                    // result becomes an array here, thus the remaining checks
                                    // will never be true from here on
                                    // so simply return the value rather than have to make
                                    // result an object and cast it with every
                                    // other use in the function.
                                    return result["@set"];
                                }
                            }
                        }
                    }
                    // 11)
                    if (result.ContainsKey("@language") && result.Count == 1)
                    {
                        result = null;
                    }
                    // 12)
                    if (activeProperty == null || "@graph".Equals(activeProperty))
                    {
                        // 12.1)
                        if (result != null && (result.Count == 0 || result.ContainsKey("@value") || result
                            .ContainsKey("@list")))
                        {
                            result = null;
                        }
                        else
                        {
                            // 12.2)
                            if (result != null && result.ContainsKey("@id") && result.Count == 1)
                            {
                                result = null;
                            }
                        }
                    }
                    // 13)
                    return result;
                }
                else
                {
                    // 2) If element is a scalar
                    // 2.1)
                    if (activeProperty == null || "@graph".Equals(activeProperty))
                    {
                        return null;
                    }
                    return activeCtx.ExpandValue(activeProperty, element);
                }
            }
        }

        /// <exception cref="JsonLdError"></exception>
        public virtual JToken Expand(Context activeCtx, JToken element)
        {
            return Expand(activeCtx, null, element);
        }

        /// <summary>
        /// _____ _ _ _ _ _ _ _ _ | ___| | __ _| |_| |_ ___ _ __ / \ | | __ _ ___ _
        /// __(_) |_| |__ _ __ ___ | |_ | |/ _` | __| __/ _ \ '_ \ / _ \ | |/ _` |/ _
        /// \| '__| | __| '_ \| '_ ` _ \ | _| | | (_| | |_| || __/ | | | / ___ \| |
        /// (_| | (_) | | | | |_| | | | | | | | | |_| |_|\__,_|\__|\__\___|_| |_| /_/
        /// \_\_|\__, |\___/|_| |_|\__|_| |_|_| |_| |_| |___/
        /// </summary>
        /// <exception cref="JsonLdError"></exception>
        internal virtual void GenerateNodeMap(JToken element, JObject
             nodeMap)
        {
            GenerateNodeMap(element, nodeMap, "@default", null, null, null);
        }

        /// <exception cref="JsonLdError"></exception>
        internal virtual void GenerateNodeMap(JToken element, JObject
             nodeMap, string activeGraph)
        {
            GenerateNodeMap(element, nodeMap, activeGraph, null, null, null);
        }

        /// <exception cref="JsonLdError"></exception>
        internal virtual void GenerateNodeMap(JToken element, JObject
             nodeMap, string activeGraph, JToken activeSubject, string activeProperty, JObject list)
        {
            GenerateNodeMap(element, nodeMap, activeGraph, activeSubject, activeProperty, list, skipSetContainsCheck: false);
        }

        private void GenerateNodeMap(JToken element, JObject nodeMap,
            string activeGraph, JToken activeSubject, string activeProperty, JObject list, bool skipSetContainsCheck)
        {
            // 1)
            if (element is JArray)
            {
                JsonLdSet set = null;

                if (list == null)
                {
                    set = new JsonLdSet();
                }

                // 1.1)
                foreach (JToken item in (JArray)element)
                {
                    skipSetContainsCheck = false;

                    if (set != null)
                    {
                        skipSetContainsCheck = set.Add(item);
                    }

                    GenerateNodeMap(item, nodeMap, activeGraph, activeSubject, activeProperty, list, skipSetContainsCheck);
                }
                return;
            }
            // for convenience
            IDictionary<string, JToken> elem = (IDictionary<string, JToken>)element;
            // 2)
            if (!((IDictionary<string,JToken>)nodeMap).ContainsKey(activeGraph))
            {
                nodeMap[activeGraph] = new JObject();
            }
            JObject graph = (JObject)nodeMap[activeGraph
                ];
            JObject node = (JObject)((activeSubject.IsNull() || activeSubject.Type != JTokenType.String) 
                ? null : graph[(string)activeSubject]);
            // 3)
            if (elem.ContainsKey("@type"))
            {
                // 3.1)
                JArray oldTypes;
                JArray newTypes = new JArray();
                if (elem["@type"] is JArray)
                {
                    oldTypes = (JArray)elem["@type"];
                }
                else
                {
                    oldTypes = new JArray();
                    oldTypes.Add((string)elem["@type"]);
                }
                foreach (string item in oldTypes)
                {
                    if (item.StartsWith("_:"))
                    {
                        newTypes.Add(GenerateBlankNodeIdentifier(item));
                    }
                    else
                    {
                        newTypes.Add(item);
                    }
                }
                if (elem["@type"] is JArray)
                {
                    elem["@type"] = newTypes;
                }
                else
                {
                    elem["@type"] = newTypes[0];
                }
            }
            // 4)
            if (elem.ContainsKey("@value"))
            {
                // 4.1)
                if (list == null)
                {
                    JsonLdUtils.MergeValue(node, activeProperty, (JObject)elem);
                }
                else
                {
                    // 4.2)
                    JsonLdUtils.MergeValue(list, "@list", (JObject)elem);
                }
            }
            else
            {
                // 5)
                if (elem.ContainsKey("@list"))
                {
                    // 5.1)
                    JObject result = new JObject();
                    result["@list"] = new JArray();
                    // 5.2)
                    //for (final Object item : (List<Object>) elem.get("@list")) {
                    //    generateNodeMap(item, nodeMap, activeGraph, activeSubject, activeProperty, result);
                    //}
                    GenerateNodeMap(elem["@list"], nodeMap, activeGraph, activeSubject, activeProperty
                        , result);
                    // 5.3)
                    JsonLdUtils.MergeValue(node, activeProperty, result);
                }
                else
                {
                    // 6)
                    // 6.1)
                    string id = (string)Collections.Remove(elem, "@id");
                    if (id != null)
                    {
                        if (id.StartsWith("_:"))
                        {
                            id = GenerateBlankNodeIdentifier(id);
                        }
                    }
                    else
                    {
                        // 6.2)
                        id = GenerateBlankNodeIdentifier(null);
                    }
                    // 6.3)
                    if (!graph.ContainsKey(id))
                    {
                        JObject tmp = new JObject();
                        tmp["@id"] = id;
                        graph[id] = tmp;
                    }
                    // 6.4) TODO: SPEC this line is asked for by the spec, but it breaks various tests
                    //node = (Map<String, Object>) graph.get(id);
                    // 6.5)
                    if (activeSubject is JObject)
                    {
                        // 6.5.1)
                        JsonLdUtils.MergeValue((JObject)graph[id], activeProperty, activeSubject
                            );
                    }
                    else
                    {
                        // 6.6)
                        if (activeProperty != null)
                        {
                            JObject reference = new JObject();
                            reference["@id"] = id;
                            // 6.6.2)
                            if (list == null)
                            {
                                // 6.6.2.1+2)
                                JsonLdUtils.MergeValue(node, activeProperty, reference, skipSetContainsCheck);
                            }
                            else
                            {
                                // 6.6.3) TODO: SPEC says to add ELEMENT to @list member, should
                                // be REFERENCE
                                JsonLdUtils.MergeValue(list, "@list", reference);
                            }
                        }
                    }
                    // TODO: SPEC this is removed in the spec now, but it's still needed (see 6.4)
                    node = (JObject)graph[id];
                    // 6.7)
                    if (elem.ContainsKey("@type"))
                    {
                        foreach (JToken type in (JArray)Collections.Remove(elem, "@type"
                            ))
                        {
                            JsonLdUtils.MergeValue(node, "@type", type);
                        }
                    }
                    // 6.8)
                    if (elem.ContainsKey("@index"))
                    {
                        JToken elemIndex = Collections.Remove(elem, "@index");
                        if (node.ContainsKey("@index"))
                        {
                            if (!JsonLdUtils.DeepCompare(node["@index"], elemIndex))
                            {
                                throw new JsonLdError(JsonLdError.Error.ConflictingIndexes);
                            }
                        }
                        else
                        {
                            node["@index"] = elemIndex;
                        }
                    }
                    // 6.9)
                    if (elem.ContainsKey("@reverse"))
                    {
                        // 6.9.1)
                        JObject referencedNode = new JObject();
                        referencedNode["@id"] = id;
                        // 6.9.2+6.9.4)
                        JObject reverseMap = (JObject)Collections.Remove
                            (elem, "@reverse");
                        // 6.9.3)
                        foreach (string property in reverseMap.GetKeys())
                        {
                            JArray values = (JArray)reverseMap[property];
                            // 6.9.3.1)
                            foreach (JToken value in values)
                            {
                                // 6.9.3.1.1)
                                GenerateNodeMap(value, nodeMap, activeGraph, referencedNode, property, null);
                            }
                        }
                    }
                    // 6.10)
                    if (elem.ContainsKey("@graph"))
                    {
                        GenerateNodeMap(Collections.Remove(elem, "@graph"), nodeMap, id, null, 
                            null, null);
                    }
                    // 6.11)
                    JArray keys = new JArray(element.GetKeys());
                    keys.SortInPlace();
                    foreach (string property_1 in keys)
                    {
                        var eachProperty_1 = property_1;
                        JToken value = elem[eachProperty_1];
                        // 6.11.1)
                        if (eachProperty_1.StartsWith("_:"))
                        {
                            eachProperty_1 = GenerateBlankNodeIdentifier(eachProperty_1);
                        }
                        // 6.11.2)
                        if (!node.ContainsKey(eachProperty_1))
                        {
                            node[eachProperty_1] = new JArray();
                        }
                        // 6.11.3)
                        GenerateNodeMap(value, nodeMap, activeGraph, id, eachProperty_1, null);
                    }
                }
            }
        }

        private readonly JObject blankNodeIdentifierMap = new JObject();

        private int blankNodeCounter = 0;

        internal virtual string GenerateBlankNodeIdentifier(string id)
        {
            if (id != null && blankNodeIdentifierMap.ContainsKey(id))
            {
                return (string)blankNodeIdentifierMap[id];
            }
            //string bnid = "_:b" + blankNodeCounter++;
            string bnid = "_:b" + Guid.NewGuid().ToString("N");
            if (id != null)
            {
                blankNodeIdentifierMap[id] = bnid;
            }
            return bnid;
        }

        internal virtual string GenerateBlankNodeIdentifier()
        {
            return GenerateBlankNodeIdentifier(null);
        }
    }
}
