// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Azure.IIoT.OpcUa.Core.Serialization
{
    using Azure.IIoT.OpcUa.Core.Exceptions;
    using System;
    using System.Buffers;
    using System.Collections.Generic;
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
    /// Source generated contexts can be registered by the assembly which owns a
    /// contract. The shared options only contain closed converters, so shipping
    /// contracts avoid runtime converter factories. A reflection fallback remains
    /// for the public <see cref="Options"/> compatibility surface when callers pass
    /// runtime-only types.
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

        /// <summary>
        /// Gets registered source-generated metadata for <typeparamref name="T"/>.
        /// </summary>
        /// <typeparam name="T">The contract type.</typeparam>
        /// <exception cref="NotSupportedException">
        /// No source-generated metadata was registered for the type.
        /// </exception>
        public static JsonTypeInfo<T> GetTypeInfo<T>()
        {
            return (JsonTypeInfo<T>)Options.GetTypeInfo(typeof(T));
        }

        static Json()
        {
            var settings = new JsonSerializerOptions(JsonSerializerDefaults.Web);
            settings.Converters.Add(new MatrixConverter<bool>());
            settings.Converters.Add(new MatrixConverter<byte>());
            settings.Converters.Add(new MatrixConverter<short>());
            settings.Converters.Add(new MatrixConverter<int>());
            settings.Converters.Add(new MatrixConverter<long>());
            settings.Converters.Add(new MatrixConverter<float>());
            settings.Converters.Add(new MatrixConverter<double>());
            settings.Converters.Add(new MatrixConverter<string>());
            settings.Converters.Add(new ByteArrayConverter());
            settings.Converters.Add(new XmlElementConverter());
            settings.Converters.Add(new BigIntegerConverter());
            settings.Converters.Add(new ReadOnlySetConverter<string>());
            settings.NumberHandling =
                JsonNumberHandling.AllowReadingFromString |
                JsonNumberHandling.AllowNamedFloatingPointLiterals;
            settings.PropertyNamingPolicy = JsonNamingPolicy.CamelCase;
            settings.PropertyNameCaseInsensitive = true;
            settings.AllowTrailingCommas = true;
            settings.WriteIndented = false;
            settings.DefaultBufferSize = 128;
            settings.TypeInfoResolver = CreateResolver();
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
        /// Registers source generated metadata owned by another assembly.
        /// Registrations are visible to the shared resolver immediately, including
        /// after the serializer options have been made read-only.
        /// </summary>
        /// <param name="resolver">The source generated resolver.</param>
        public static void RegisterTypeInfoResolver(IJsonTypeInfoResolver resolver)
        {
            ArgumentNullException.ThrowIfNull(resolver);
            lock (kResolverLock)
            {
                kResolvers.Add(JsonTypeInfoResolver.WithAddedModifier(resolver,
                    DataContractResolver.Modify));
            }
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
                return JsonSerializer.Serialize(o, OptionsFor<T>(format));
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
                    type ?? o?.GetType() ?? typeof(object), OptionsFor(
                        type ?? o?.GetType() ?? typeof(object), format));
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
                return JsonSerializer.SerializeToUtf8Bytes(o, OptionsFor<T>(format));
            }
            catch (JsonException ex)
            {
                throw new SerializerException(ex.Message, ex);
            }
        }

        /// <summary>
        /// Serialize to memory using source generated metadata.
        /// </summary>
        /// <typeparam name="T">The value type.</typeparam>
        /// <param name="o">The value to serialize.</param>
        /// <param name="typeInfo">The source generated type metadata.</param>
        public static ReadOnlyMemory<byte> SerializeToMemory<T>(T? o,
            JsonTypeInfo<T> typeInfo)
        {
            ArgumentNullException.ThrowIfNull(typeInfo);
            try
            {
                return JsonSerializer.SerializeToUtf8Bytes<T>(o!, typeInfo);
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
                    type ?? o?.GetType() ?? typeof(object), OptionsFor(
                        type ?? o?.GetType() ?? typeof(object), format));
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
                    type ?? o?.GetType() ?? typeof(object), OptionsFor(
                        type ?? o?.GetType() ?? typeof(object), format));
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
                return JsonSerializer.Deserialize<T>(str, OptionsFor<T>());
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
                return JsonSerializer.Deserialize<T>(buffer.Span, OptionsFor<T>());
            }
            catch (JsonException ex)
            {
                throw new SerializerException(ex.Message, ex);
            }
        }

        /// <summary>
        /// Deserialize from memory using source generated metadata.
        /// </summary>
        /// <typeparam name="T">The value type.</typeparam>
        /// <param name="buffer">The JSON buffer.</param>
        /// <param name="typeInfo">The source generated type metadata.</param>
        public static T? Deserialize<T>(ReadOnlyMemory<byte> buffer,
            JsonTypeInfo<T> typeInfo)
        {
            ArgumentNullException.ThrowIfNull(typeInfo);
            try
            {
                return JsonSerializer.Deserialize(buffer.Span, typeInfo);
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
                return JsonSerializer.Deserialize<T>(ref reader, OptionsFor<T>());
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
                return JsonSerializer.Deserialize(str, type, OptionsFor(type));
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
                return JsonSerializer.Deserialize(buffer.Span, type, OptionsFor(type));
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
                return await JsonSerializer.DeserializeAsync<T>(stream, OptionsFor<T>(), ct)
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
                var type = o?.GetType() ?? typeof(object);
                return JsonSerializer.SerializeToNode(o, type, OptionsFor(type));
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

        [RequiresUnreferencedCode(kReflection)]
        [RequiresDynamicCode(kReflection)]
        private static JsonSerializerOptions OptionsFor<T>(
            SerializeOption format = SerializeOption.None)
        {
            return OptionsFor(typeof(T), format);
        }

        [RequiresUnreferencedCode(kReflection)]
        [RequiresDynamicCode(kReflection)]
        private static JsonSerializerOptions OptionsFor(Type type,
            SerializeOption format = SerializeOption.None)
        {
            var options = OptionsFor(format);
            try
            {
                _ = options.GetTypeInfo(type);
                return options;
            }
            catch (NotSupportedException)
            {
                var reflection = new DefaultJsonTypeInfoResolver();
                reflection.Modifiers.Add(DataContractResolver.Modify);
                return new JsonSerializerOptions(options)
                {
                    TypeInfoResolver = JsonTypeInfoResolver.Combine(
                        options.TypeInfoResolver!, reflection)
                };
            }
        }

        private const string kReflection =
            "System.Text.Json reflection based serialization is not AOT / trim safe.";

        private static IJsonTypeInfoResolver CreateResolver()
        {
            return kRegisteredResolver;
        }

        private static readonly JsonNodeOptions kNodeOptions = new()
        {
            PropertyNameCaseInsensitive = true
        };

        private static readonly JsonDocumentOptions kDocumentOptions = new()
        {
            AllowTrailingCommas = true,
            MaxDepth = 64
        };

        private static readonly object kResolverLock = new();
        private static readonly List<IJsonTypeInfoResolver> kResolvers =
        [
            JsonTypeInfoResolver.WithAddedModifier(CoreJsonContext.Default,
                DataContractResolver.Modify)
        ];
        private static readonly IJsonTypeInfoResolver kRegisteredResolver =
            new RegisteredTypeInfoResolver();

        private sealed class RegisteredTypeInfoResolver : IJsonTypeInfoResolver
        {
            [UnconditionalSuppressMessage("Trimming", "IL2026",
                Justification = "The fallback preserves the public Json.Options " +
                    "contract for callers which pass runtime-only types. Shipping " +
                    "DTOs resolve through registered source-generated metadata.")]
            [UnconditionalSuppressMessage("AotAnalysis", "IL3050",
                Justification = "The fallback preserves the public Json.Options " +
                    "contract for callers which pass runtime-only types. Shipping " +
                    "DTOs resolve through registered source-generated metadata.")]
            public RegisteredTypeInfoResolver()
            {
                _reflectionFallback = new DefaultJsonTypeInfoResolver();
                _reflectionFallback.Modifiers.Add(DataContractResolver.Modify);
            }

            public JsonTypeInfo? GetTypeInfo(Type type,
                JsonSerializerOptions options)
            {
                lock (kResolverLock)
                {
                    foreach (var resolver in kResolvers)
                    {
                        var typeInfo = resolver.GetTypeInfo(type, options);
                        if (typeInfo != null)
                        {
                            return typeInfo;
                        }
                    }
                }
                return _reflectionFallback.GetTypeInfo(type, options);
            }

            private readonly DefaultJsonTypeInfoResolver _reflectionFallback;
        }
    }
}
