// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Azure.IIoT.OpcUa.Encoders
{
    using Azure.IIoT.OpcUa.Publisher.Models;
    using Azure.IIoT.OpcUa.Core.Utils;
    using Opc.Ua;
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Linq;
    using System.Text.Json;
    using System.Text.Json.Nodes;

    /// <summary>
    /// Variant encoder implementation
    /// </summary>
    public sealed class JsonVariantEncoder : IVariantEncoder
    {
        /// <inheritdoc/>
        public IServiceMessageContext Context { get; }

        /// <summary>
        /// Create encoder
        /// </summary>
        /// <param name="context"></param>
        public JsonVariantEncoder(IServiceMessageContext context)
        {
            Context = context;
        }

        /// <inheritdoc/>
        public JsonNode? Encode(Variant? value, out BuiltInType builtinType,
            ValueEncoding encoding = ValueEncoding.Reversible)
        {
            if (value == null || value == Variant.Null)
            {
                builtinType = BuiltInType.Null;
                return null;
            }
            var useReversibleEncoding = encoding != ValueEncoding.NonReversible;
            using var stream = new MemoryStream();
            using (var encoder = new JsonEncoderEx(stream, Context)
            {
                UseAdvancedEncoding = true,
                UseReversibleEncoding = useReversibleEncoding
            })
            {
                encoder.WriteVariant(nameof(value), value.Value);
            }
            var token = JsonNode.Parse(stream.ToArray());
            if (useReversibleEncoding)
            {
                Enum.TryParse(ToStringValue(token?["value"]?["Type"]),
                    true, out builtinType);
                return token?["value"]?["Body"]?.DeepClone();
            }

            //
            // The non-reversible encoding writes the value contents directly
            // without the Type/Body envelope, so derive the built in type
            // from the variant type information instead.
            //
            builtinType = value.Value.TypeInfo?.BuiltInType ?? BuiltInType.Null;
            return token?["value"]?.DeepClone();
        }

        /// <inheritdoc/>
        public Variant Decode(JsonNode? value, BuiltInType builtinType)
        {
            if (value is null)
            {
                return Variant.Null;
            }

            //
            // Sanitize json input from user
            //
            value = Sanitize(value, builtinType == BuiltInType.String);

            string json;
            if (builtinType == BuiltInType.Null ||
                (builtinType == BuiltInType.Variant &&
                    value is JsonObject))
            {
                //
                // Let the decoder try and decode the json variant.
                //
                json = new JsonObject
                {
                    ["value"] = value?.DeepClone()
                }.ToJsonString();
            }
            else
            {
                //
                // Give decoder a hint as to the type to use to decode.
                //
                json = new JsonObject
                {
                    ["value"] = new JsonObject
                    {
                        ["Body"] = value?.DeepClone(),
                        ["Type"] = (byte)builtinType
                    }
                }.ToJsonString();
            }

            //
            // Decode json to a real variant
            //
            using var text = new StringReader(json);
            using var reader = new Newtonsoft.Json.JsonTextReader(text);
            using var decoder = new JsonDecoderEx(reader, Context);
            return decoder.ReadVariant(nameof(value));
        }

        /// <summary>
        /// Sanitizes user input by removing quotes around non strings,
        /// or adding array brackets to comma seperated values that are
        /// not string type and recursing through arrays to do the same.
        /// The output is a pure json token that can be passed to the
        /// json decoder.
        /// </summary>
        /// <param name="value"></param>
        /// <param name="isString"></param>
        /// <returns></returns>
        internal JsonNode? Sanitize(JsonNode? value, bool isString)
        {
            if (value is null)
            {
                return value;
            }

            string asString;
            if (IsJsonString(value))
            {
                asString = value.GetValue<string>();
            }
            else
            {
                asString = value.ToJsonString();
            }

            if (value is not JsonObject && value is not JsonArray && !IsJsonString(value))
            {
                //
                // If this should be a string - return as such
                //
                return isString ? JsonValue.Create(asString) : value;
            }

            if (string.IsNullOrWhiteSpace(asString))
            {
                return value;
            }

            //
            // Try to parse string as json
            //
            if (!IsJsonString(value))
            {
                asString = asString.Replace("\\\"", "\"", StringComparison.Ordinal);
            }
            var token = Try.Op(() => ParseLenient(asString));
            if (token is not null)
            {
                value = token;
            }

            if (IsJsonString(value))
            {
                //
                // try to split the string as comma seperated list
                //
                var elements = asString.Split(',');
                if (isString)
                {
                    //
                    // If all elements are quoted, then this is a
                    // string array
                    //
                    if (elements.Length > 1)
                    {
                        var array = new List<string>();
                        foreach (var element in elements)
                        {
                            var trimmed = element.Trim().TrimQuotes();
                            if (trimmed == element)
                            {
                                // Treat entire string as value
                                return value;
                            }
                            array.Add(trimmed);
                        }
                        // No need to sanitize contents
                        return new JsonArray(array
                            .Select(s => (JsonNode?)JsonValue.Create(s)).ToArray());
                    }
                }
                else
                {
                    //
                    // First trim any quotes from string before splitting.
                    //
                    if (elements.Length > 1)
                    {
                        //
                        // Parse as array
                        //
                        var trimmed = elements.Select(e => e.TrimQuotes()).ToArray();
                        try
                        {
                            value = ParseLenient(
                                "[" + trimmed.Aggregate((x, y) => x + "," + y) + "]");
                        }
                        catch
                        {
                            value = ParseLenient(
                                "[\"" + trimmed.Aggregate((x, y) => x + "\",\"" + y) + "\"]");
                        }
                    }
                    else
                    {
                        //
                        // Try to remove next layer of quotes and try again.
                        //
                        var trimmed = asString.Trim().TrimQuotes();
                        if (trimmed != asString)
                        {
                            return Sanitize(JsonValue.Create(trimmed), isString);
                        }
                    }
                }
            }

            if (value is JsonArray list)
            {
                //
                // Sanitize each element accordingly
                //
                return new JsonArray(list
                    .Select(t => Sanitize(t, isString)?.DeepClone()).ToArray());
            }
            return value;
        }

        /// <summary>
        /// Value is a json string
        /// </summary>
        /// <param name="node"></param>
        /// <returns></returns>
        private static bool IsJsonString(JsonNode? node)
        {
            return node is JsonValue value &&
                value.GetValueKind() == JsonValueKind.String;
        }

        /// <summary>
        /// Parse a json string leniently. Unlike
        /// <see cref="JsonNode.Parse(string, JsonNodeOptions?, JsonDocumentOptions)"/>
        /// this accepts the same relaxed json the previous Newtonsoft based
        /// implementation did (e.g. single quoted strings) so that user
        /// provided values continue to sanitize identically. The value is
        /// reparsed into a <see cref="JsonNode"/> via a strict serialization.
        /// </summary>
        /// <param name="value"></param>
        /// <returns></returns>
        private static JsonNode? ParseLenient(string value)
        {
            var token = Newtonsoft.Json.Linq.JToken.Parse(value);
            return JsonNode.Parse(token.ToString(Newtonsoft.Json.Formatting.None));
        }

        /// <summary>
        /// Coerce a json node to its string representation
        /// </summary>
        /// <param name="node"></param>
        /// <returns></returns>
        private static string? ToStringValue(JsonNode? node)
        {
            if (node is null)
            {
                return null;
            }
            if (node is JsonValue value && value.TryGetValue<string>(out var s))
            {
                return s;
            }
            return node.ToJsonString();
        }
    }
}
