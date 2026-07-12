// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Azure.IIoT.OpcUa.Core.Serialization
{
    using Azure.IIoT.OpcUa.Core.Exceptions;
    using System;
    using System.Buffers;
    using System.Diagnostics.CodeAnalysis;
    using System.IO;
    using System.Text;
    using System.Text.Json;
    using System.Text.Json.Nodes;
    using System.Text.Json.Serialization;
    using System.Text.Json.Serialization.Metadata;
    using System.Threading;
    using System.Threading.Tasks;

    /// <summary>
    /// Serialization option.
    /// </summary>
    public enum SerializeOption
    {
        /// <summary> No special formatting </summary>
        None,

        /// <summary> Indented output </summary>
        Indented
    }

    /// <summary>
    /// Owned System.Text.Json serialization helper that replaces the former
    /// Legacy default json serializer. The configured <see cref="Options"/> mirror
    /// the Legacy settings (camel case naming, case insensitive, lenient number
    /// handling and the data contract aware converters) so that the produced wire
    /// format stays compatible with the API models that are annotated with
    /// <see cref="System.Runtime.Serialization.DataMemberAttribute"/>.
    ///
    /// The helper is reflection based (it uses the runtime type info resolver and
    /// the reflection based converters) and therefore not Native-AOT / trim safe.
    /// Every entry point is annotated with <see cref="RequiresUnreferencedCodeAttribute"/>
    /// and <see cref="RequiresDynamicCodeAttribute"/>; AOT hardening of the
    /// serialization pipeline is a later migration phase.
    /// </summary>
    public static class Json
    {
        /// <summary>
        /// Mime type emitted by the serializer.
        /// </summary>
        public const string MimeType = ContentMimeType.Json;

        /// <summary>
        /// Content encoding used by the serializer.
        /// </summary>
        public static Encoding ContentEncoding => Encoding.UTF8;

        /// <summary>
        /// The compact serializer options.
        /// </summary>
        public static JsonSerializerOptions Options { get; }

        /// <summary>
        /// The indented serializer options.
        /// </summary>
        public static JsonSerializerOptions IndentedOptions { get; }

        [UnconditionalSuppressMessage("Trimming", "IL2026",
            Justification = "Reflection based serializer, hardened in a later phase.")]
        [UnconditionalSuppressMessage("AotAnalysis", "IL3050",
            Justification = "Reflection based serializer, hardened in a later phase.")]
        static Json()
        {
            var settings = new JsonSerializerOptions(JsonSerializerDefaults.Web);
            settings.Converters.Add(new MatrixConverter());
            settings.Converters.Add(new ByteArrayConverter());
            settings.Converters.Add(new XmlElementConverter());
            settings.Converters.Add(new BigIntegerConverter());
            settings.Converters.Add(new DataContractEnumConverter(
                JsonNamingPolicy.CamelCase, true));
            settings.Converters.Add(new JsonStringEnumConverter(
                JsonNamingPolicy.CamelCase, true));
            settings.Converters.Add(new ReadOnlySetConverter());
            settings.NumberHandling =
                JsonNumberHandling.AllowReadingFromString |
                JsonNumberHandling.AllowNamedFloatingPointLiterals;
            settings.PropertyNamingPolicy = JsonNamingPolicy.CamelCase;
            settings.PropertyNameCaseInsensitive = true;
            settings.AllowTrailingCommas = true;
            settings.WriteIndented = false;
            settings.DefaultBufferSize = 128;
            // Reproduce the DataContract wire format (DataMember name / order /
            // emit-default semantics) as a metadata contract modifier instead of
            // the former whole-object reflection converter. The modifier works on
            // reflection and source generated type info alike and does not emit
            // dynamic code, which is the prerequisite for Native-AOT / trim safe
            // (source generated) serialization of the API models.
            var resolver = new DefaultJsonTypeInfoResolver();
            resolver.Modifiers.Add(DataContractResolver.Modify);
            settings.TypeInfoResolver = resolver;
            if (settings.MaxDepth > 64)
            {
                settings.MaxDepth = 64;
            }
            Options = settings;

            IndentedOptions = new JsonSerializerOptions(settings)
            {
                WriteIndented = true
            };
        }

        /// <summary>
        /// Apply the configured <see cref="Options"/> (naming policy, number
        /// handling and the data contract aware converters) onto an existing
        /// <see cref="JsonSerializerOptions"/> instance, such as the one owned by
        /// the ASP.NET Core MVC <c>JsonOptions</c>. This replaces the former Legacy
        /// <c>AddJsonSerializer()</c> MVC formatter so controller (de)serialization
        /// uses the same settings as the rest of the pipeline.
        /// </summary>
        /// <param name="target"></param>
        public static void ApplyTo(JsonSerializerOptions target)
        {
            ArgumentNullException.ThrowIfNull(target);
            target.NumberHandling = Options.NumberHandling;
            target.DefaultIgnoreCondition = Options.DefaultIgnoreCondition;
            target.DefaultBufferSize = Options.DefaultBufferSize;
            target.PropertyNamingPolicy = Options.PropertyNamingPolicy;
            target.PropertyNameCaseInsensitive = Options.PropertyNameCaseInsensitive;
            target.IncludeFields = Options.IncludeFields;
            target.UnknownTypeHandling = Options.UnknownTypeHandling;
            target.WriteIndented = Options.WriteIndented;
            target.DictionaryKeyPolicy = Options.DictionaryKeyPolicy;
            target.IgnoreReadOnlyProperties = Options.IgnoreReadOnlyProperties;
            target.AllowTrailingCommas = Options.AllowTrailingCommas;
            target.MaxDepth = Options.MaxDepth;
            if (target.TypeInfoResolver == null)
            {
                target.TypeInfoResolver = Options.TypeInfoResolver;
            }
            else
            {
                target.TypeInfoResolver = JsonTypeInfoResolver.WithAddedModifier(
                    target.TypeInfoResolver, DataContractResolver.Modify);
            }
            target.Converters.Clear();
            foreach (var converter in Options.Converters)
            {
                target.Converters.Add(converter);
            }
        }

        /// <summary>
        /// Serialize to string.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="o"></param>
        /// <param name="format"></param>
        [RequiresUnreferencedCode(kReflection)]
        [RequiresDynamicCode(kReflection)]
        public static string SerializeToString<T>(T? o,
            SerializeOption format = SerializeOption.None)
        {
            try
            {
                return JsonSerializer.Serialize(o, OptionsFor(format));
            }
            catch (JsonException ex)
            {
                throw new SerializerException(ex.Message, ex);
            }
        }

        /// <summary>
        /// Serialize object to string.
        /// </summary>
        /// <param name="o"></param>
        /// <param name="type"></param>
        /// <param name="format"></param>
        [RequiresUnreferencedCode(kReflection)]
        [RequiresDynamicCode(kReflection)]
        public static string SerializeObjectToString(object? o, Type? type = null,
            SerializeOption format = SerializeOption.None)
        {
            try
            {
                return JsonSerializer.Serialize(o,
                    type ?? o?.GetType() ?? typeof(object), OptionsFor(format));
            }
            catch (JsonException ex)
            {
                throw new SerializerException(ex.Message, ex);
            }
        }

        /// <summary>
        /// Serialize to memory.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="o"></param>
        /// <param name="format"></param>
        [RequiresUnreferencedCode(kReflection)]
        [RequiresDynamicCode(kReflection)]
        public static ReadOnlyMemory<byte> SerializeToMemory<T>(T? o,
            SerializeOption format = SerializeOption.None)
        {
            try
            {
                return JsonSerializer.SerializeToUtf8Bytes(o, OptionsFor(format));
            }
            catch (JsonException ex)
            {
                throw new SerializerException(ex.Message, ex);
            }
        }

        /// <summary>
        /// Serialize object to memory.
        /// </summary>
        /// <param name="o"></param>
        /// <param name="type"></param>
        /// <param name="format"></param>
        [RequiresUnreferencedCode(kReflection)]
        [RequiresDynamicCode(kReflection)]
        public static ReadOnlyMemory<byte> SerializeObjectToMemory(object? o,
            Type? type = null, SerializeOption format = SerializeOption.None)
        {
            try
            {
                return JsonSerializer.SerializeToUtf8Bytes(o,
                    type ?? o?.GetType() ?? typeof(object), OptionsFor(format));
            }
            catch (JsonException ex)
            {
                throw new SerializerException(ex.Message, ex);
            }
        }

        /// <summary>
        /// Serialize object into a buffer writer.
        /// </summary>
        /// <param name="buffer"></param>
        /// <param name="o"></param>
        /// <param name="type"></param>
        /// <param name="format"></param>
        [RequiresUnreferencedCode(kReflection)]
        [RequiresDynamicCode(kReflection)]
        public static void SerializeObject(IBufferWriter<byte> buffer, object? o,
            Type? type = null, SerializeOption format = SerializeOption.None)
        {
            try
            {
                var options = format == SerializeOption.Indented ?
                    new JsonWriterOptions { Indented = true } : default;
                using var writer = new Utf8JsonWriter(buffer, options);
                JsonSerializer.Serialize(writer, o,
                    type ?? o?.GetType() ?? typeof(object), Options);
            }
            catch (JsonException ex)
            {
                throw new SerializerException(ex.Message, ex);
            }
        }

        /// <summary>
        /// Deserialize from string.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="str"></param>
        [RequiresUnreferencedCode(kReflection)]
        [RequiresDynamicCode(kReflection)]
        public static T? Deserialize<T>(string str)
        {
            try
            {
                return JsonSerializer.Deserialize<T>(str, Options);
            }
            catch (JsonException ex)
            {
                throw new SerializerException(ex.Message, ex);
            }
        }

        /// <summary>
        /// Deserialize from buffer.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="buffer"></param>
        [RequiresUnreferencedCode(kReflection)]
        [RequiresDynamicCode(kReflection)]
        public static T? Deserialize<T>(ReadOnlyMemory<byte> buffer)
        {
            try
            {
                return JsonSerializer.Deserialize<T>(buffer.Span, Options);
            }
            catch (JsonException ex)
            {
                throw new SerializerException(ex.Message, ex);
            }
        }

        /// <summary>
        /// Deserialize from sequence.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="buffer"></param>
        [RequiresUnreferencedCode(kReflection)]
        [RequiresDynamicCode(kReflection)]
        public static T? Deserialize<T>(ReadOnlySequence<byte> buffer)
        {
            try
            {
                var reader = new Utf8JsonReader(buffer);
                return JsonSerializer.Deserialize<T>(ref reader, Options);
            }
            catch (JsonException ex)
            {
                throw new SerializerException(ex.Message, ex);
            }
        }

        /// <summary>
        /// Deserialize from string.
        /// </summary>
        /// <param name="str"></param>
        /// <param name="type"></param>
        [RequiresUnreferencedCode(kReflection)]
        [RequiresDynamicCode(kReflection)]
        public static object? Deserialize(string str, Type type)
        {
            try
            {
                return JsonSerializer.Deserialize(str, type, Options);
            }
            catch (JsonException ex)
            {
                throw new SerializerException(ex.Message, ex);
            }
        }

        /// <summary>
        /// Deserialize from buffer to a runtime type.
        /// </summary>
        /// <param name="buffer"></param>
        /// <param name="type"></param>
        [RequiresUnreferencedCode(kReflection)]
        [RequiresDynamicCode(kReflection)]
        public static object? Deserialize(ReadOnlyMemory<byte> buffer, Type type)
        {
            try
            {
                return JsonSerializer.Deserialize(buffer.Span, type, Options);
            }
            catch (JsonException ex)
            {
                throw new SerializerException(ex.Message, ex);
            }
        }

        /// <summary>
        /// Deserialize from stream.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="stream"></param>
        /// <param name="ct"></param>
        [RequiresUnreferencedCode(kReflection)]
        [RequiresDynamicCode(kReflection)]
        public static async ValueTask<T?> DeserializeAsync<T>(Stream stream,
            CancellationToken ct = default)
        {
            try
            {
                return await JsonSerializer.DeserializeAsync<T>(stream, Options, ct)
                    .ConfigureAwait(false);
            }
            catch (JsonException ex)
            {
                throw new SerializerException(ex.Message, ex);
            }
        }

        /// <summary>
        /// Parse json string into a node.
        /// </summary>
        /// <param name="str"></param>
        public static JsonNode? Parse(string str)
        {
            try
            {
                return JsonNode.Parse(str, kNodeOptions, kDocumentOptions);
            }
            catch (JsonException ex)
            {
                throw new SerializerException(ex.Message, ex);
            }
        }

        /// <summary>
        /// Parse json buffer into a node.
        /// </summary>
        /// <param name="buffer"></param>
        public static JsonNode? Parse(ReadOnlyMemory<byte> buffer)
        {
            try
            {
                var reader = new Utf8JsonReader(buffer.Span);
                return JsonNode.Parse(ref reader, kNodeOptions);
            }
            catch (JsonException ex)
            {
                throw new SerializerException(ex.Message, ex);
            }
        }

        /// <summary>
        /// Convert an object into a json node.
        /// </summary>
        /// <param name="o"></param>
        [RequiresUnreferencedCode(kReflection)]
        [RequiresDynamicCode(kReflection)]
        public static JsonNode? FromObject(object? o)
        {
            try
            {
                return JsonSerializer.SerializeToNode(o,
                    o?.GetType() ?? typeof(object), Options);
            }
            catch (JsonException ex)
            {
                throw new SerializerException(ex.Message, ex);
            }
        }

        private static JsonSerializerOptions OptionsFor(SerializeOption format)
        {
            return format == SerializeOption.Indented ? IndentedOptions : Options;
        }

        private const string kReflection =
            "System.Text.Json reflection based serialization is not AOT / trim safe.";

        private static readonly JsonNodeOptions kNodeOptions = new()
        {
            PropertyNameCaseInsensitive = true
        };

        private static readonly JsonDocumentOptions kDocumentOptions = new()
        {
            AllowTrailingCommas = true,
            MaxDepth = 64
        };
    }
}
