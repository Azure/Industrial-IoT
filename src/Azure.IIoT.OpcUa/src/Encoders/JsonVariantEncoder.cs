// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Azure.IIoT.OpcUa.Encoders
{
    using Furly.Extensions.Serializers;
    using Furly.Extensions.Utils;
    using Opc.Ua;
    using System;
    using System.Collections.Generic;
    using System.Globalization;
    using System.IO;
    using System.Linq;

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
        /// <param name="serializer"></param>
        public JsonVariantEncoder(IServiceMessageContext context, IJsonSerializer serializer)
        {
            Context = context;
            _serializer = serializer;
        }

        /// <inheritdoc/>
        public VariantValue Encode(Variant? value, out BuiltInType builtinType)
        {
            if (value == null || value == Variant.Null)
            {
                builtinType = BuiltInType.Null;
                return VariantValue.Null;
            }
            using var stream = new MemoryStream();
            using (var encoder = new JsonEncoderEx(stream, Context)
            {
                UseAdvancedEncoding = true
            })
            {
                encoder.WriteVariant(nameof(value), value.Value);
            }
            var token = _serializer.Parse(stream.ToArray());
            Enum.TryParse((string?)token.GetByPath("value.Type"),
                true, out builtinType);
            return token.GetByPath("value.Body");
        }

        /// <inheritdoc/>
        public Variant Decode(VariantValue value, BuiltInType builtinType)
        {
            if (value.IsNull())
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
                    value.IsObject))
            {
                //
                // Let the decoder try and decode the json variant.
                //
                json = _serializer.SerializeToString(new { value });
            }
            else
            {
                //
                // Give decoder a hint as to the type to use to decode.
                //
                json = _serializer.SerializeToString(new
                {
                    value = new
                    {
                        Body = value,
                        Type = (byte)builtinType
                    }
                });
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
        internal VariantValue Sanitize(VariantValue value, bool isString)
        {
            if (value.IsNull())
            {
                return value;
            }

            if (!value.TryGetString(out var asString, true, CultureInfo.InvariantCulture))
            {
                asString = _serializer.SerializeToString(value);
            }

            if (!value.IsObject && !value.IsListOfValues && !value.IsString)
            {
                //
                // If this should be a string - return as such
                //
                return isString ? asString : value;
            }

            if (string.IsNullOrWhiteSpace(asString))
            {
                return value;
            }

            //
            // Try to parse string as json
            //
            if (!value.IsString)
            {
                asString = asString.Replace("\\\"", "\"", StringComparison.Ordinal);
            }
            var token = Try.Op(() => _serializer.Parse(asString));
            if (token is not null)
            {
                value = token;
            }

            if (value.IsString)
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
                        return _serializer.FromObject(array);
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
                            value = _serializer.Parse(
                                "[" + trimmed.Aggregate((x, y) => x + "," + y) + "]");
                        }
                        catch
                        {
                            value = _serializer.Parse(
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
                            return Sanitize(trimmed, isString);
                        }
                    }
                }
            }

            if (value.IsListOfValues)
            {
                //
                // Sanitize each element accordingly
                //
                return _serializer.FromObject(value.Values
                    .Select(t => Sanitize(t, isString)));
            }
            return value;
        }

        private readonly IJsonSerializer _serializer;
    }
}
