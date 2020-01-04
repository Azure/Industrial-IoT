// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IIoT.OpcUa.Protocol.Services {
    using Microsoft.Azure.IIoT.Utils;
    using Newtonsoft.Json;
    using Newtonsoft.Json.Linq;
    using Opc.Ua;
    using Opc.Ua.Encoders;
    using System;
    using System.IO;
    using System.Linq;
    using System.Text;

    /// <summary>
    /// Json variant codec
    /// </summary>
    public class VariantEncoderFactory : IVariantEncoderFactory {

        /// <inheritdoc/>
        public IVariantEncoder Default => new JsonVariantEncoder(new ServiceMessageContext());

        /// <inheritdoc/>
        public IVariantEncoder Create(ServiceMessageContext context) {
            return new JsonVariantEncoder(context);
        }

        /// <summary>
        /// Variant encoder implementation
        /// </summary>
        private sealed class JsonVariantEncoder : IVariantEncoder {

            /// <inheritdoc/>
            public ServiceMessageContext Context { get; }

            /// <summary>
            /// Create encoder
            /// </summary>
            /// <param name="context"></param>
            internal JsonVariantEncoder(ServiceMessageContext context) {
                Context = context ?? throw new ArgumentNullException(nameof(context));
            }

            /// <inheritdoc/>
            public JToken Encode(Variant value, out BuiltInType builtinType) {

                if (value == Variant.Null) {
                    builtinType = BuiltInType.Null;
                    return JValue.CreateNull();
                }
                using (var stream = new MemoryStream()) {
                    using (var encoder = new JsonEncoderEx(stream, Context) {
                        UseAdvancedEncoding = true
                    }) {
                        encoder.WriteVariant(nameof(value), value);
                    }
                    var json = Encoding.UTF8.GetString(stream.ToArray());
                    try {
                        var token = JToken.Parse(json);
                        Enum.TryParse((string)token.SelectToken("value.Type"),
                            true, out builtinType);
                        return token.SelectToken("value.Body");
                    }
                    catch (JsonReaderException jre) {
                        throw new FormatException($"Failed to parse '{json}'. " +
                            "See inner exception for more details.", jre);
                    }
                }
            }

            /// <inheritdoc/>
            public Variant Decode(JToken value, BuiltInType builtinType) {

                //
                // Sanitize json input from user
                //
                value = Sanitize(value, builtinType == BuiltInType.String);

                JObject json;
                if (builtinType == BuiltInType.Null ||
                    (builtinType == BuiltInType.Variant && value is JObject)) {
                        //
                        // Let the decoder try and decode the json variant.
                        //
                        json = new JObject {
                        { nameof(value), value }
                    };
                }
                else {
                    //
                    // Give decoder a hint as to the type to use to decode.
                    //
                    json = new JObject {
                        { nameof(value), new JObject {
                                { "Body", value },
                                { "Type", (byte)builtinType }
                            }
                        }
                    };
                }

                //
                // Decode json to a real variant
                //
                using (var decoder = new JsonDecoderEx(json, Context)) {
                    return decoder.ReadVariant(nameof(value));
                }
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
            internal static JToken Sanitize(JToken value, bool isString) {
                if (value == null || value.Type == JTokenType.Null) {
                    return value;
                }

                var asString = value.Type == JTokenType.String ?
                    (string)value : value.ToString(Formatting.None);

                if (value is JValue val) {
                    if (value.Type != JTokenType.String) {
                        //
                        // If this should be a string - return as such
                        //
                        return isString ? new JValue(asString) : value;
                    }
                }

                if (string.IsNullOrWhiteSpace(asString)) {
                    return value;
                }

                //
                // Try to parse string as json
                //
                if (value.Type != JTokenType.String) {
                    asString = asString.Replace("\\\"", "\"");
                }
                var token = Try.Op(() => JToken.Parse(asString));
                if (token != null) {
                    value = token;
                }

                if (value.Type == JTokenType.String) {

                    //
                    // try to split the string as comma seperated list
                    //
                    var elements = asString.Split(',');
                    if (isString) {
                        //
                        // If all elements are quoted, then this is a
                        // string array
                        //
                        if (elements.Length > 1) {
                            var array = new JArray();
                            foreach (var element in elements) {
                                var trimmed = element.Trim().TrimQuotes();
                                if (trimmed == element) {
                                    // Treat entire string as value
                                    return value;
                                }
                                array.Add(trimmed);
                            }
                            return array; // No need to sanitize contents
                        }
                    }
                    else {
                        //
                        // First trim any quotes from string before splitting.
                        //
                        if (elements.Length > 1) {
                            //
                            // Parse all contained elements and return as array
                            //
                            value = new JArray(elements
                                .Select(s => s.Trim()));
                        }
                        else {
                            //
                            // Try to remove next layer of quotes and try again.
                            //
                            var trimmed = asString.Trim().TrimQuotes();
                            if (trimmed != asString) {
                                return Sanitize(trimmed, isString);
                            }
                        }
                    }
                }

                if (value is JArray arr) {
                    //
                    // Sanitize each element accordingly
                    //
                    return new JArray(arr.Select(t => Sanitize(t, isString)));
                }
                return value;
            }
        }
    }
}
