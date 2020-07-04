// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IIoT.Serializers.NewtonSoft {
    using Microsoft.Azure.IIoT.Exceptions;
    using Microsoft.Azure.IIoT.Serializers;
    using Newtonsoft.Json;
    using Newtonsoft.Json.Converters;
    using Newtonsoft.Json.Linq;
    using Newtonsoft.Json.Serialization;
    using System;
    using System.Buffers;
    using System.Collections.Generic;
    using System.IO;
    using System.Linq;
    using System.Text;

    /// <summary>
    /// Newtonsoft json serializer
    /// </summary>
    public class NewtonSoftJsonSerializer : IJsonSerializerSettingsProvider,
        IJsonSerializer {

        /// <inheritdoc/>
        public string MimeType => ContentMimeType.Json;

        /// <inheritdoc/>
        public Encoding ContentEncoding => Encoding.UTF8;

        /// <summary>
        /// Json serializer settings
        /// </summary>
        public JsonSerializerSettings Settings { get; }

        /// <summary>
        /// Create serializer
        /// </summary>
        /// <param name="providers"></param>
        public NewtonSoftJsonSerializer(
            IEnumerable<IJsonSerializerConverterProvider> providers = null) {
            var settings = new JsonSerializerSettings();
            if (providers != null) {
                foreach (var provider in providers) {
                    settings.Converters.AddRange(provider.GetConverters());
                }
            }
            settings.ContractResolver = new DefaultContractResolver {
                NamingStrategy = new CamelCaseDictionaryKeys()
            };
            settings.Converters.Add(new JsonVariantConverter(this));
            settings.Converters.Add(new StringEnumConverter {
                AllowIntegerValues = true,
                NamingStrategy = new CamelCaseNamingStrategy()
            });
            settings.FloatFormatHandling = FloatFormatHandling.String;
            settings.FloatParseHandling = FloatParseHandling.Double;
            settings.DateParseHandling = DateParseHandling.DateTime;
            settings.DateTimeZoneHandling = DateTimeZoneHandling.Utc;
            settings.DateFormatHandling = DateFormatHandling.IsoDateFormat;
            settings.ReferenceLoopHandling = ReferenceLoopHandling.Error;
            if (settings.MaxDepth > 64) {
                settings.MaxDepth = 64;
            }
            Settings = settings;
        }

        /// <inheritdoc/>
        public object Deserialize(ReadOnlyMemory<byte> buffer, Type type) {
            try {
                // TODO move to .net 3 to use readonly span as stream source
                var jsonSerializer = JsonSerializer.CreateDefault(Settings);
                using (var stream = new MemoryStream(buffer.ToArray()))
                using (var reader = new StreamReader(stream, ContentEncoding)) {
                    return jsonSerializer.Deserialize(reader, type);
                }
            }
            catch (JsonReaderException ex) {
                throw new SerializerException(ex.Message, ex);
            }
        }

        /// <inheritdoc/>
        public void Serialize(IBufferWriter<byte> buffer, object o, SerializeOption format) {
            try {
                var jsonSerializer = JsonSerializer.CreateDefault(Settings);
                jsonSerializer.Formatting = format == SerializeOption.Indented ?
                    Formatting.Indented :
                    Formatting.None;
                // TODO move to .net 3 to use buffer writer as stream sink
                using (var stream = new MemoryStream()) {
                    using (var writer = new StreamWriter(stream)) {
                        jsonSerializer.Serialize(writer, o);
                    }
                    var written = stream.ToArray();
                    buffer.Write(written);
                }
            }
            catch (JsonReaderException ex) {
                throw new SerializerException(ex.Message, ex);
            }
        }

        /// <inheritdoc/>
        public VariantValue Parse(ReadOnlyMemory<byte> buffer) {
            try {
                // TODO move to .net 3 to use readonly span as stream source
                using (var stream = new MemoryStream(buffer.ToArray()))
                using (var reader = new StreamReader(stream, ContentEncoding))
                using (var jsonReader = new JsonTextReader(reader)) {

                    jsonReader.FloatParseHandling = Settings.FloatParseHandling;
                    jsonReader.DateParseHandling = Settings.DateParseHandling;
                    jsonReader.DateTimeZoneHandling = Settings.DateTimeZoneHandling;
                    jsonReader.MaxDepth = Settings.MaxDepth;

                    var token = JToken.Load(jsonReader);

                    while (jsonReader.Read()) {
                        // Read to end or throw
                    }
                    return new JsonVariantValue(token, this);
                }
            }
            catch (JsonReaderException ex) {
                throw new SerializerException(ex.Message, ex);
            }
        }

        /// <inheritdoc/>
        public VariantValue FromObject(object o) {
            try {
                return new JsonVariantValue(this, o);
            }
            catch (JsonReaderException ex) {
                throw new SerializerException(ex.Message, ex);
            }
        }

        /// <summary>
        /// Token wrapper
        /// </summary>
        internal class JsonVariantValue : VariantValue {

            /// <summary>
            /// The wrapped token
            /// </summary>
            internal JToken Token { get; set; }

            /// <summary>
            /// Create value
            /// </summary>
            /// <param name="o"></param>
            /// <param name="serializer"></param>
            internal JsonVariantValue(NewtonSoftJsonSerializer serializer, object o) {
                _serializer = serializer;
                Token = o == null ? JValue.CreateNull() : FromObject(o);
            }

            /// <summary>
            /// Create value
            /// </summary>
            /// <param name="token"></param>
            /// <param name="serializer"></param>
            internal JsonVariantValue(JToken token, NewtonSoftJsonSerializer serializer) {
                _serializer = serializer;
                Token = token ?? JValue.CreateNull();
            }

            /// <inheritdoc/>
            protected override VariantValueType GetValueType() {
                switch (Token.Type) {
                    case JTokenType.Object:
                        return VariantValueType.Object;
                    case JTokenType.Array:
                        return VariantValueType.Values;
                    case JTokenType.None:
                    case JTokenType.Null:
                    case JTokenType.Undefined:
                    case JTokenType.Constructor:
                    case JTokenType.Property:
                    case JTokenType.Comment:
                        return VariantValueType.Null;
                    default:
                        return VariantValueType.Primitive;
                }
            }

            /// <inheritdoc/>
            protected override object GetRawValue() {
                if (Token is JValue v) {
                    if (v.Value is Uri u) {
                        return u.ToString();
                    }
                    return v.Value;
                }
                return Token;
            }

            /// <inheritdoc/>
            protected override IEnumerable<string> GetObjectProperties() {
                if (Token is JObject o) {
                    return o.Properties().Select(p => p.Name);
                }
                return Enumerable.Empty<string>();
            }

            /// <inheritdoc/>
            protected override IEnumerable<VariantValue> GetArrayElements() {
                if (Token is JArray array) {
                    return array.Select(i => new JsonVariantValue(i, _serializer));
                }
                return Enumerable.Empty<VariantValue>();
            }

            /// <inheritdoc/>
            protected override int GetArrayCount() {
                if (Token is JArray array) {
                    return array.Count;
                }
                return 0;
            }

            /// <inheritdoc/>
            public override VariantValue Copy(bool shallow) {
                return new JsonVariantValue(shallow ? Token :
                    Token.DeepClone(), _serializer);
            }

            /// <inheritdoc/>
            public override object ConvertTo(Type type) {
                try {
                    return Token.ToObject(type,
                        JsonSerializer.CreateDefault(_serializer.Settings));
                }
                catch (JsonReaderException ex) {
                    throw new SerializerException(ex.Message, ex);
                }
            }

            /// <inheritdoc/>
            public override VariantValue GetByPath(string path, StringComparison compare) {
                try {
                    if (compare == StringComparison.InvariantCultureIgnoreCase ||
                        compare == StringComparison.OrdinalIgnoreCase) {
                        return base.GetByPath(path, compare);
                    }
                    var selected = Token.SelectToken(path, false);
                    return new JsonVariantValue(selected, _serializer);
                }
                catch (JsonReaderException ex) {
                    throw new SerializerException(ex.Message, ex);
                }
            }

            /// <inheritdoc/>
            protected override void AppendTo(StringBuilder sb) {
                sb.Append(Token.ToString(Formatting.None,
                    _serializer.Settings.Converters.ToArray()));
            }

            /// <inheritdoc/>
            public override bool TryGetProperty(string key, out VariantValue value,
                StringComparison compare) {
                if (Token is JObject o) {
                    var success = o.TryGetValue(key, compare, out var token);
                    if (success) {
                        value = new JsonVariantValue(token, _serializer);
                        return true;
                    }
                }
                value = new JsonVariantValue(null, _serializer);
                return false;
            }

            /// <inheritdoc/>
            public override bool TryGetElement(int index, out VariantValue value) {
                if (index >= 0 && Token is JArray o && index < o.Count) {
                    value = new JsonVariantValue(o[index], _serializer);
                    return true;
                }
                value = new JsonVariantValue(null, _serializer);
                return false;
            }

            /// <inheritdoc/>
            protected override VariantValue AddProperty(string property) {
                if (Token is JObject o) {
                    var child = new JsonVariantValue(null, _serializer);
                    // Add to object
                    o.Add(property, child.Token);
                    return child;
                }
                throw new NotSupportedException("Not an object");
            }

            /// <inheritdoc/>
            public override void AssignValue(object value) {
                switch (Token.Parent) {
                    case JObject o:
                        // Part of an object - update object
                        var property = o.Properties().FirstOrDefault(p => p.Value == Token);
                        if (property == null) {
                            throw new ArgumentOutOfRangeException("No parent found");
                        }
                        Token = FromObject(value);
                        property.Value = Token;
                        break;
                    case JArray a:
                        // Part of an object - update object
                        for (var i = 0; i < a.Count; i++) {
                            if (a[i] == Token) {
                                Token = FromObject(value);
                                a[i] = Token;
                                return;
                            }
                        }
                        throw new ArgumentOutOfRangeException("No parent found");
                    case JProperty p:
                        Token = FromObject(value);
                        p.Value = Token;
                        break;
                    default:
                        throw new NotSupportedException("Not an object or array");
                }
            }

            /// <inheritdoc/>
            protected override bool TryEqualsValue(object o, out bool equality) {
                if (o is JToken t) {
                    equality = DeepEquals(Token, t);
                    return true;
                }
                return base.TryEqualsValue(o, out equality);
            }

            /// <inheritdoc/>
            protected override bool TryEqualsVariant(VariantValue v, out bool equality) {
                if (v is JsonVariantValue json) {
                    equality = DeepEquals(Token, json.Token);
                    return true;
                }
                return base.TryEqualsVariant(v, out equality);
            }

            /// <inheritdoc/>
            protected override bool TryCompareToValue(object o, out int result) {
                if (Token is JValue v1 && o is JValue v2) {
                    result = v1.CompareTo(v2);
                    return true;
                }
                return base.TryCompareToValue(o, out result);
            }

            /// <inheritdoc/>
            protected override bool TryCompareToVariantValue(VariantValue v, out int result) {
                if (v is JsonVariantValue json) {
                    return TryCompareToValue(json.Token, out result);
                }
                return base.TryCompareToVariantValue(v, out result);
            }

            /// <summary>
            /// Compare tokens in more consistent fashion
            /// </summary>
            /// <param name="t1"></param>
            /// <param name="t2"></param>
            /// <returns></returns>
            internal bool DeepEquals(JToken t1, JToken t2) {
                if (t1 == null || t2 == null) {
                    return t1 == t2;
                }
                if (ReferenceEquals(t1, t2)) {
                    return true;
                }
                if (t1 is JObject o1 && t2 is JObject o2) {
                    // Compare properties in order of key
                    var props1 = o1.Properties().OrderBy(k => k.Name)
                        .Select(p => p.Value);
                    var props2 = o2.Properties().OrderBy(k => k.Name)
                        .Select(p => p.Value);
                    return props1.SequenceEqual(props2,
                        Compare.Using<JToken>((x, y) => DeepEquals(x, y)));
                }

                if (t1 is JContainer c1 && t2 is JContainer c2) {
                    // For all other containers - order is important
                    return c1.Children().SequenceEqual(c2.Children(),
                        Compare.Using<JToken>((x, y) => DeepEquals(x, y)));
                }

                if (t1 is JValue && t2 is JValue) {
                    if (t1.Equals(t2)) {
                        return true;
                    }
                    var s1 = t1.ToString(Formatting.None,
                        _serializer.Settings.Converters.ToArray());
                    var s2 = t2.ToString(Formatting.None,
                        _serializer.Settings.Converters.ToArray());
                    if (s1 == s2) {
                        return true;
                    }
                }
                return false;
            }

            /// <summary>
            /// Create token from object and rethrow serializer exception
            /// </summary>
            /// <param name="o"></param>
            /// <returns></returns>
            private JToken FromObject(object o) {
                try {
                    return JToken.FromObject(o,
                        JsonSerializer.CreateDefault(_serializer.Settings));
                }
                catch (JsonReaderException ex) {
                    throw new SerializerException(ex.Message, ex);
                }
            }

            private readonly NewtonSoftJsonSerializer _serializer;
        }

        /// <summary>
        /// Json veriant converter
        /// </summary>
        internal sealed class JsonVariantConverter : JsonConverter {

            /// <summary>
            /// Converter
            /// </summary>
            /// <param name="serializer"></param>
            public JsonVariantConverter(NewtonSoftJsonSerializer serializer) {
                _serializer = serializer;
            }

            /// <inheritdoc/>
            public override void WriteJson(JsonWriter writer, object value,
                JsonSerializer serializer) {
                switch (value) {
                    case JsonVariantValue json:
                        json.Token.WriteTo(writer, serializer.Converters.ToArray());
                        break;
                    case VariantValue variant:
                        if (VariantValueEx.IsNull(variant)) {
                            writer.WriteNull();
                        }
                        else if (variant.IsListOfValues) {
                            writer.WriteStartArray();
                            foreach (var item in variant.Values) {
                                WriteJson(writer, item, serializer);
                            }
                            writer.WriteEndArray();
                        }
                        else if (variant.IsObject) {
                            writer.WriteStartObject();
                            foreach (var key in variant.PropertyNames) {
                                var item = variant[key];
                                if (VariantValueEx.IsNull(item)) {
                                    if (serializer.NullValueHandling != NullValueHandling.Include ||
                                        serializer.DefaultValueHandling != DefaultValueHandling.Include) {
                                        break;
                                    }
                                }
                                writer.WritePropertyName(key);
                                WriteJson(writer, item, serializer);
                            }
                            writer.WriteEndObject();
                        }
                        else if (variant.TryGetValue(out var primitive)) {
                            serializer.Serialize(writer, primitive);
                            break;
                        }
                        else {
                            serializer.Serialize(writer, variant.Value);
                            break;
                        }
                        break;
                    default:
                        throw new NotSupportedException("Unexpected type passed");
                }
            }

            /// <inheritdoc/>
            public override object ReadJson(JsonReader reader, Type objectType,
                object existingValue, JsonSerializer serializer) {
                // Read variant from json
                var token = JToken.Load(reader);
                if (token.Type == JTokenType.Null) {
                    return null;
                }
                return new JsonVariantValue(token, _serializer);
            }

            /// <inheritdoc/>
            public override bool CanConvert(Type objectType) {
                return typeof(VariantValue).IsAssignableFrom(objectType);
            }

            private readonly NewtonSoftJsonSerializer _serializer;
        }

        /// <summary>
        /// Strategy to only camel case dictionary keys
        /// </summary>
        private class CamelCaseDictionaryKeys : CamelCaseNamingStrategy {

            /// <summary>
            /// Create strategy
            /// </summary>
            public CamelCaseDictionaryKeys() {
                ProcessDictionaryKeys = true;
            }

            /// <inheritdoc/>
            protected override string ResolvePropertyName(string name) {
                return name;
            }
        }
    }
}