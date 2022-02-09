// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IIoT.OpcUa.Protocol.Services {
    using Microsoft.Azure.IIoT.Serializers;
    using Microsoft.Azure.IIoT.Serializers.NewtonSoft;
    using Microsoft.Azure.IIoT.Utils;
    using Opc.Ua;
    using Opc.Ua.Encoders;
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Linq;

    /// <summary>
    /// Json variant codec
    /// </summary>
    public class VariantEncoderFactory : IVariantEncoderFactory {

        /// <summary>
        /// Create encoder
        /// </summary>
        /// <param name="serializer"></param>
        public VariantEncoderFactory(IJsonSerializer serializer = null) {
            _serializer = serializer ?? new NewtonSoftJsonSerializer();
        }

        /// <inheritdoc/>
        public IVariantEncoder Default =>
            new JsonVariantEncoder(new ServiceMessageContext(), _serializer);

        /// <inheritdoc/>
        public IVariantEncoder Create(IServiceMessageContext context) {
            return new JsonVariantEncoder(context, _serializer);
        }

        /// <summary>
        /// Variant encoder implementation
        /// </summary>
        private sealed class JsonVariantEncoder : IVariantEncoder {

            /// <inheritdoc/>
            public IServiceMessageContext Context { get; }

            /// <inheritdoc/>
            public IJsonSerializer Serializer { get; }

            /// <summary>
            /// Create encoder
            /// </summary>
            /// <param name="context"></param>
            /// <param name="serializer"></param>
            internal JsonVariantEncoder(IServiceMessageContext context, IJsonSerializer serializer) {
                Context = context ?? throw new ArgumentNullException(nameof(context));
                Serializer = serializer ?? new NewtonSoftJsonSerializer();
            }

            /// <inheritdoc/>
            public VariantValue Encode(Variant? value, out BuiltInType builtinType) {
                if (value == null || value == Variant.Null) {
                    builtinType = BuiltInType.Null;
                    return VariantValue.Null;
                }
                using (var stream = new MemoryStream()) {
                    using (var encoder = new JsonEncoderEx(stream, Context) {
                        UseAdvancedEncoding = true
                    }) {
                        encoder.WriteVariant(nameof(value), value.Value);
                    }
                    var token = Serializer.Parse(stream.ToArray());
                    Enum.TryParse((string)token.GetByPath("value.Type"),
                        true, out builtinType);
                    return token.GetByPath("value.Body");
                }
            }

            /// <inheritdoc/>
            public Variant Decode(VariantValue value, BuiltInType builtinType) {

                if (VariantValueEx.IsNull(value)) {
                    return Variant.Null;
                }

                //
                // Sanitize json input from user
                //
                value = Sanitize(value, builtinType == BuiltInType.String);

                VariantValue json;
                if (builtinType == BuiltInType.Null ||
                    (builtinType == BuiltInType.Variant &&
                        value.IsObject)) {
                    //
                    // Let the decoder try and decode the json variant.
                    //
                    json = Serializer.FromObject(new { value });
                }
                else {
                    //
                    // Give decoder a hint as to the type to use to decode.
                    //
                    json = Serializer.FromObject(new {
                        value = new {
                            Body = value,
                            Type = (byte)builtinType
                        }
                    });
                }

                //
                // Decode json to a real variant
                //
                using (var text = new StringReader(Serializer.SerializeToString(json)))
                using (var reader = new Newtonsoft.Json.JsonTextReader(text))
                using (var decoder = new JsonDecoderEx(reader, Context)) {
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
            internal VariantValue Sanitize(VariantValue value, bool isString) {
                if (VariantValueEx.IsNull(value)) {
                    return value;
                }

                if (!value.TryGetString(out var asString, true)) {
                    asString = Serializer.SerializeToString(value);
                }

                if (!value.IsObject && !value.IsListOfValues && !value.IsString) {
                    //
                    // If this should be a string - return as such
                    //
                    return isString ? asString : value;
                }

                if (string.IsNullOrWhiteSpace(asString)) {
                    return value;
                }

                //
                // Try to parse string as json
                //
                if (!value.IsString) {
                    asString = asString.Replace("\\\"", "\"");
                }
                var token = Try.Op(() => Serializer.Parse(asString));
                if (!(token is null)) {
                    value = token;
                }

                if (value.IsString) {

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
                            var array = new List<string>();
                            foreach (var element in elements) {
                                var trimmed = element.Trim().TrimQuotes();
                                if (trimmed == element) {
                                    // Treat entire string as value
                                    return value;
                                }
                                array.Add(trimmed);
                            }
                            // No need to sanitize contents
                            return Serializer.FromObject(array);
                        }
                    }
                    else {
                        //
                        // First trim any quotes from string before splitting.
                        //
                        if (elements.Length > 1) {
                            //
                            // Parse as array
                            //
                            var trimmed = elements.Select(e => e.TrimQuotes()).ToArray();
                            try {
                                value = Serializer.Parse(
                                    "[" + trimmed.Aggregate((x, y) => x + "," + y) + "]");
                            }
                            catch {
                                value = Serializer.Parse(
                                    "[\"" + trimmed.Aggregate((x, y) => x + "\",\"" + y) + "\"]");
                            }
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

                if (value.IsListOfValues) {
                    //
                    // Sanitize each element accordingly
                    //
                    return Serializer.FromObject(value.Values
                        .Select(t => Sanitize(t, isString)));
                }
                return value;
            }
        }

        private readonly IJsonSerializer _serializer;
    }
}
