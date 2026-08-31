// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License. See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Azure.IIoT.OpcUa.Core.Serialization
{
    using System;
    using System.Buffers;
    using System.IO;
    using System.Runtime.Serialization;
    using System.Collections.Generic;
    using System.Linq;
    using System.Text;
    using System.Text.Json;
    using System.Text.Json.Nodes;
    using System.Text.Json.Serialization;
    using System.Text.Json.Serialization.Metadata;
    using System.Threading.Tasks;
    using Azure.IIoT.OpcUa.Core.Exceptions;
    using Xunit;

    /// <summary>
    /// Tests for shared JSON serializer options.
    /// </summary>
    public sealed class JsonTests
    {
        [Fact]
        public void ApplyToPreservesSourceGeneratedResolverAndDataContractWireFormat()
        {
            var options = new JsonSerializerOptions
            {
                TypeInfoResolver = JsonTestsContext.Default
            };

            Json.ApplyTo(options);

            Assert.Same(JsonTestsContext.Default,
                options.GetTypeInfo(typeof(DataContractModel)).OriginatingResolver);

            var model = new DataContractModel
            {
                Value = "value",
                OmitWhenDefault = 0,
                NotADataMember = "excluded"
            };
            var json = JsonSerializer.Serialize(model, options);

            Assert.Equal("""{"ExactName":"value"}""", json);
            var result = JsonSerializer.Deserialize<DataContractModel>(
                """{"ExactName":"deserialized"}""", options);
            Assert.NotNull(result);
            Assert.Equal("deserialized", result.Value);
            Assert.Equal(0, result.OmitWhenDefault);
            Assert.Null(result.NotADataMember);
        }

        [Fact]
        public void ClosedConvertersPreserveSetAndMatrixWireFormats()
        {
            IReadOnlySet<string> values = new HashSet<string> { "one", "two" };
            var matrix = new[,] { { 1, 2 }, { 3, 4 } };

            Assert.Equal("""["one","two"]""", JsonSerializer.Serialize(values, Json.Options));
            Assert.Equivalent(values, JsonSerializer.Deserialize<IReadOnlySet<string>>(
                """["one","two"]""", Json.Options));
            Assert.Equal("""[[1,2],[3,4]]""", JsonSerializer.Serialize(matrix, Json.Options));
            Assert.Equal(matrix.Cast<int>(), JsonSerializer.Deserialize<int[,]>(
                """[[1,2],[3,4]]""", Json.Options)!.Cast<int>());
        }

        [Fact]
        public void ApplyToReplacesReflectionResolverWithRegisteredMetadata()
        {
            var options = new JsonSerializerOptions
            {
                TypeInfoResolver = new DefaultJsonTypeInfoResolver()
            };

            Json.ApplyTo(options);

            Assert.Throws<NotSupportedException>(() =>
                options.GetTypeInfo(typeof(ReflectionOnlyModel)));
        }

        [Fact]
        public void OptionsExposeLegacyCompatibleDefaults()
        {
            Assert.Equal(ContentMimeType.Json, Json.MimeType);
            Assert.Same(Encoding.UTF8, Json.ContentEncoding);
            Assert.Equal(false, Json.Options.WriteIndented);
            Assert.Equal(true, Json.IndentedOptions.WriteIndented);
            Assert.Equal(true, Json.Options.PropertyNameCaseInsensitive);
            Assert.Equal(true, Json.Options.AllowTrailingCommas);
            Assert.Equal(JsonNamingPolicy.CamelCase, Json.Options.PropertyNamingPolicy);
            Assert.InRange(Json.Options.MaxDepth, 0, 64);
        }

        [Fact]
        public void SourceGeneratedStringSerializationHonorsFormatting()
        {
            var model = new DataContractModel { Value = "value" };

            var compact = Json.SerializeToString(model,
                JsonTestsContext.Default.DataContractModel);
            var indented = Json.SerializeToString(model,
                JsonTestsContext.Default.DataContractModel, SerializeOption.Indented);

            Assert.Equal(
                """{"value":"value","omitWhenDefault":0,"notADataMember":null}""",
                compact);
            Assert.Contains(Environment.NewLine, indented);
            Assert.Contains("  \"value\": \"value\"", indented);
        }

        [Fact]
        public void ReflectionSerializationOverloadsUseDataContractResolver()
        {
            var model = new DataContractModel
            {
                Value = "value",
                NotADataMember = "ignored"
            };

            var asString = Json.SerializeToString(model);
            var asObjectString = Json.SerializeObjectToString(model, typeof(DataContractModel));
            var asMemory = Json.SerializeToMemory(model);
            var asObjectMemory = Json.SerializeObjectToMemory(model, typeof(DataContractModel));

            Assert.Equal("""{"ExactName":"value"}""", asString);
            Assert.Equal(asString, asObjectString);
            Assert.Equal(asString, Encoding.UTF8.GetString(asMemory.Span));
            Assert.Equal(asString, Encoding.UTF8.GetString(asObjectMemory.Span));
        }

        [Fact]
        public void SerializeObjectWritesToBufferWriter()
        {
            var buffer = new ArrayBufferWriter<byte>();
            var model = new DataContractModel { Value = "value" };

            Json.SerializeObject(buffer, model, typeof(DataContractModel));

            Assert.Equal("""{"ExactName":"value"}""",
                Encoding.UTF8.GetString(buffer.WrittenSpan));
        }

        [Fact]
        public void DeserializeOverloadsUseReflectionOrSuppliedMetadata()
        {
            const string payload = """{"ExactName":"value"}""";
            var bytes = Encoding.UTF8.GetBytes(payload);

            var fromString = Json.Deserialize<DataContractModel>(payload);
            var fromTypeInfo = Json.Deserialize(payload,
                JsonTestsContext.Default.DataContractModel);
            var fromMemory = Json.Deserialize<DataContractModel>(bytes);
            var fromMemoryTypeInfo = Json.Deserialize(bytes,
                JsonTestsContext.Default.DataContractModel);
            var fromSequence = Json.Deserialize<DataContractModel>(
                new ReadOnlySequence<byte>(bytes));
            var fromRuntimeType = Json.Deserialize(payload, typeof(DataContractModel));
            var fromTypeInfoWireName = Json.Deserialize(
                """{"value":"typed"}""", JsonTestsContext.Default.DataContractModel);

            Assert.Equal("value", fromString!.Value);
            Assert.Null(fromTypeInfo!.Value);
            Assert.Equal("value", fromMemory!.Value);
            Assert.Null(fromMemoryTypeInfo!.Value);
            Assert.Equal("value", fromSequence!.Value);
            Assert.Equal("value", Assert.IsType<DataContractModel>(fromRuntimeType).Value);
            Assert.Equal("typed", fromTypeInfoWireName!.Value);
        }

        [Fact]
        public async Task DeserializeAsyncReadsStreamAsync()
        {
            await using var stream = new MemoryStream(
                Encoding.UTF8.GetBytes("""{"ExactName":"value"}"""));

            var result = await Json.DeserializeAsync<DataContractModel>(stream);

            Assert.NotNull(result);
            Assert.Equal("value", result.Value);
        }

        [Fact]
        public void ParseStringAndMemoryAllowTrailingCommas()
        {
            const string payload = """{"Value":1,}""";

            var fromString = Assert.IsType<JsonObject>(Json.Parse(payload));
            var fromMemory = Assert.IsType<JsonObject>(
                Json.Parse(Encoding.UTF8.GetBytes(payload)));

            Assert.Equal(1, (int)fromString["value"]!);
            Assert.Equal(1, (int)fromMemory["value"]!);
        }

        [Fact]
        public void FromObjectAppliesDataContractMetadata()
        {
            var node = Assert.IsType<JsonObject>(Json.FromObject(
                new DataContractModel
                {
                    Value = "value",
                    NotADataMember = "ignored"
                }));

            Assert.Equal("value", (string?)node["ExactName"]);
            Assert.Null(node[nameof(DataContractModel.NotADataMember)]);
        }

        [Fact]
        public void InvalidJsonIsWrappedInSerializerException()
        {
            Assert.Throws<SerializerException>(() =>
                Json.Deserialize<DataContractModel>("{"));
            Assert.Throws<SerializerException>(() =>
                Json.Deserialize<DataContractModel>(Encoding.UTF8.GetBytes("{")));
            Assert.Throws<SerializerException>(() =>
                Json.Parse("{"));
            Assert.Throws<SerializerException>(() =>
                Json.Parse(Encoding.UTF8.GetBytes("{")));
        }

        [Fact]
        public void SourceGeneratedOverloadsRejectNullTypeInfo()
        {
            var model = new DataContractModel { Value = "value" };

            Assert.Throws<ArgumentNullException>(() =>
                Json.SerializeToString(model, null!));
            Assert.Throws<ArgumentNullException>(() =>
                Json.SerializeToMemory(model, null!));
            Assert.Throws<ArgumentNullException>(() =>
                Json.Deserialize("{}", (JsonTypeInfo<DataContractModel>)null!));
            Assert.Throws<ArgumentNullException>(() =>
                Json.Deserialize<DataContractModel>(Encoding.UTF8.GetBytes("{}"),
                    (JsonTypeInfo<DataContractModel>)null!));
        }

        [Fact]
        public void SerializeToString_Reflection_IndentedFormat()
        {
            var model = new DataContractModel { Value = "v" };

            var result = Json.SerializeToString(model, SerializeOption.Indented);

            Assert.Contains(Environment.NewLine, result);
        }

        [Fact]
        public void SerializeObjectToString_Indented_ProducesFormattedJson()
        {
            var model = new DataContractModel { Value = "v" };

            var result = Json.SerializeObjectToString(model, typeof(DataContractModel),
                SerializeOption.Indented);

            Assert.Contains(Environment.NewLine, result);
        }

        [Fact]
        public void SerializeToMemory_Indented_ProducesFormattedBytes()
        {
            var model = new DataContractModel { Value = "v" };

            var bytes = Json.SerializeToMemory(model, SerializeOption.Indented);
            var text = Encoding.UTF8.GetString(bytes.Span);

            Assert.Contains(Environment.NewLine, text);
        }

        [Fact]
        public void SerializeObjectToMemory_Indented_ProducesFormattedBytes()
        {
            var model = new DataContractModel { Value = "v" };

            var bytes = Json.SerializeObjectToMemory(model, typeof(DataContractModel),
                SerializeOption.Indented);
            var text = Encoding.UTF8.GetString(bytes.Span);

            Assert.Contains(Environment.NewLine, text);
        }

        [Fact]
        public void SerializeObject_Indented_ProducesFormattedOutput()
        {
            var buffer = new ArrayBufferWriter<byte>();
            var model = new DataContractModel { Value = "v" };

            Json.SerializeObject(buffer, model, typeof(DataContractModel),
                SerializeOption.Indented);
            var text = Encoding.UTF8.GetString(buffer.WrittenSpan);

            Assert.Contains(Environment.NewLine, text);
        }

        [Fact]
        public void Deserialize_ReadOnlyMemory_RuntimeType_ProducesTypedResult()
        {
            var bytes = Encoding.UTF8.GetBytes("""{"ExactName":"hello"}""");
            ReadOnlyMemory<byte> memory = bytes;

            var result = Json.Deserialize(memory, typeof(DataContractModel));

            var model = Assert.IsType<DataContractModel>(result);
            Assert.Equal("hello", model.Value);
        }

        [Fact]
        public void RegisterTypeInfoResolver_NullArgumentThrows()
        {
            Assert.Throws<ArgumentNullException>(() =>
                Json.RegisterTypeInfoResolver(null!));
        }

        [Fact]
        public void RegisterTypeInfoResolver_AcceptsNewContextWithoutThrowing()
        {
            // Just verifying the method itself doesn't throw for a valid resolver.
            // Type resolution is tested via the pre-registered CoreJsonContext.
            Json.RegisterTypeInfoResolver(JsonTestsContext.Default);
        }

        [Fact]
        public void GetTypeInfo_ForUnknownType_Throws()
        {
            Assert.Throws<NotSupportedException>(() =>
                Json.GetTypeInfo<UnknownType>());
        }

        private sealed class UnknownType { }

        [Fact]
        public void ApplyTo_NullArgumentThrows()
        {
            Assert.Throws<ArgumentNullException>(() => Json.ApplyTo(null!));
        }

        [Fact]
        public void ApplyTo_RegisterTypeInfoResolver_NullArgumentThrows()
        {
            Assert.Throws<ArgumentNullException>(() => Json.RegisterTypeInfoResolver(null!));
        }

    }

    [DataContract]
    internal sealed class DataContractModel
    {
        [DataMember(Name = "ExactName")]
        public string Value { get; set; }

        [DataMember(EmitDefaultValue = false)]
        public int OmitWhenDefault { get; set; }

        public string NotADataMember { get; set; }
    }

    internal sealed class ReflectionOnlyModel
    {
    }

    [JsonSourceGenerationOptions(PropertyNamingPolicy = JsonKnownNamingPolicy.CamelCase)]
    [JsonSerializable(typeof(DataContractModel))]
    internal sealed partial class JsonTestsContext : JsonSerializerContext
    {
    }
}
