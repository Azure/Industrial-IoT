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
    public class ValueEncoder : IValueEncoder {

        /// <summary>
        /// Formats a variant as string
        /// </summary>
        /// <param name="value"></param>
        /// <param name="context"></param>
        /// <returns></returns>
        public JToken Encode(Variant value, ServiceMessageContext context) {
            if (value == Variant.Null) {
                return JValue.CreateNull();
            }
            using (var stream = new MemoryStream()) {
                using (var encoder = new JsonEncoderEx(context ?? _context,
                    stream)) {
                    encoder.WriteVariant(nameof(value), value);
                }
                var json = Encoding.UTF8.GetString(stream.ToArray());
                try {
                    return JToken.Parse(json).SelectToken("value.Body");
                }
                catch (JsonReaderException jre) {
                    throw new FormatException($"Failed to parse '{json}'. " +
                        "See inner exception for more details.", jre);
                }
            }
        }

        /// <summary>
        /// Parse variant value from string
        /// </summary>
        /// <param name="value"></param>
        /// <param name="builtinType"></param>
        /// <param name="valueRank"></param>
        /// <param name="context"></param>
        /// <returns></returns>
        public Variant Decode(JToken value, BuiltInType builtinType, int? valueRank,
            ServiceMessageContext context) {
            if (value == null || value.Type == JTokenType.Null) {
                return Variant.Null;
            }
            JObject json;
            if (builtinType == BuiltInType.Null) {
                //
                // No type provided - use decoders ability to convert jtoken
                // to variant.
                //
                json = new JObject {
                    { nameof(value), value = Sanitize(value, false, null) }
                };
            }
            else {
                //
                // Type is given, sanitze input and decode as reversible json
                // encoded variant.
                //
                value = Sanitize(value, builtinType == BuiltInType.String,
                    valueRank);
                json = new JObject {
                    { nameof(value), new JObject {
                            { "Body", value },
                            { "Type", (byte)builtinType }
                        }
                    }
                };
            }
            using (var decoder = new JsonDecoderEx(context ?? _context, json)) {
                return decoder.ReadVariant(nameof(value));
            }
        }

        /// <summary>
        /// Helper to parse and convert a token value
        /// </summary>
        /// <param name="value"></param>
        /// <param name="isString"></param>
        /// <param name="valueRank"></param>
        /// <returns></returns>
        private static JToken Sanitize(JToken value, bool isString,
            int? valueRank) {
            var array = valueRank.HasValue && valueRank.Value != ValueRanks.Scalar;
            if (!isString || array) {
                if (!array) {
                    value = value.ToString().TrimQuotes();
                }
                if (value.Type == JTokenType.String) {
                    // Try to convert to array or other value
                    var token = Try.Op(() => JToken.Parse(value.ToString()));
                    if (token != null) {
                        value = token;
                    }
                    if (array && !(value is JArray)) {
                        try {
                            value = JArray.Parse("[" + value + "]");
                        }
                        catch {
                            return new JArray(value);
                        }
                        return new JArray(((JArray)value)
                            .Select(t => Sanitize(t, isString, null)));
                    }
                }
            }
            return value;
        }

        private readonly ServiceMessageContext _context =
            new ServiceMessageContext();
    }
}
