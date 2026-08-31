//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Azure.IIoT.OpcUa.Publisher.Models.Tests
{
    using Azure.IIoT.OpcUa.Core.Serialization;
    using AutoFixture;
    using AutoFixture.Kernel;
    using FluentAssertions;
    using System;
    using System.ComponentModel.DataAnnotations;
    using System.Text.Json.Nodes;
    using System.Collections;
    using System.Collections.Generic;
    using System.Linq;
    using System.Text.Json;
    using Xunit;

    public class JsonSerializerTests
    {
        [Theory]
        [MemberData(nameof(TypeFixture.GetDataContractTypes), MemberType = typeof(TypeFixture))]
        public void SerializerDeserializeScalarTypeToBuffer(Type type)
        {
            var instance = Activator.CreateInstance(type);

            var buffer = Json.SerializeObjectToMemory(instance, type);
            var result = Json.Deserialize(Json.ContentEncoding.GetString(buffer.Span), type);

            result.Should().BeEquivalentTo(instance);
        }

        [Theory]
        [MemberData(nameof(TypeFixture.GetDataContractTypes), MemberType = typeof(TypeFixture))]
        public void SerializerDeserializeScalarTypeToString(Type type)
        {
            var instance = Activator.CreateInstance(type);

            var str = Json.SerializeObjectToString(instance);
            var result = Json.Deserialize(str, type);

            result.Should().BeEquivalentTo(instance);
            var expectedString = JsonSerializer.Serialize(instance, Json.Options);
            str.Should().Be(expectedString);
        }

        [Theory]
        [MemberData(nameof(TypeFixture.GetDataContractTypes), MemberType = typeof(TypeFixture))]
        public void SerializerDeserializeScalarTypeToBufferWithFixture(Type type)
        {
            var fixture = new Fixture();
            fixture.Customizations.Add(new TypeRelay(typeof(IReadOnlySet<>), typeof(HashSet<>)));
            fixture.Customizations.Add(new TypeRelay(typeof(IReadOnlyList<>), typeof(List<>)));
            fixture.Customizations.Add(new TypeRelay(typeof(IReadOnlyDictionary<,>), typeof(Dictionary<,>)));
            fixture.Customizations.Add(new TypeRelay(typeof(IReadOnlyCollection<>), typeof(List<>)));
            fixture.Behaviors
                .OfType<ThrowingRecursionBehavior>()
                .ToList()
                .ForEach(b => fixture.Behaviors.Remove(b));
            fixture.Behaviors.Add(new OmitOnRecursionBehavior(recursionDepth: 2));
            // Create some random variant value
            fixture.Register(() => Json.FromObject(Activator.CreateInstance(type)));
            // JsonNode dynamic values are not round-tripped by the fixture serializers
            fixture.Register<JsonNode>(() => null!);
            // Ensure utc datetimes
            fixture.Register(() => DateTimeOffset.UtcNow);
            fixture.Register(() => DateTime.UtcNow);
            var instance = new SpecimenContext(fixture).Resolve(new SeededRequest(type, null));

            var buffer = Json.SerializeObjectToMemory(instance, type);
            var result = Json.Deserialize(Json.ContentEncoding.GetString(buffer.Span), type);

            result.Should().BeEquivalentTo(instance, options => options.AllowingInfiniteRecursion());
        }

        [Theory]
        [MemberData(nameof(TypeFixture.GetDataContractTypes), MemberType = typeof(TypeFixture))]
        public void SerializerDeserializeArrayTypeToBufferWithFixture(Type type)
        {
            var fixture = new Fixture { RepeatCount = 2 };
            fixture.Customizations.Add(new TypeRelay(typeof(IReadOnlySet<>), typeof(HashSet<>)));
            fixture.Customizations.Add(new TypeRelay(typeof(IReadOnlyList<>), typeof(List<>)));
            fixture.Customizations.Add(new TypeRelay(typeof(IReadOnlyDictionary<,>), typeof(Dictionary<,>)));
            fixture.Customizations.Add(new TypeRelay(typeof(IReadOnlyCollection<>), typeof(List<>)));
            fixture.Behaviors
                .OfType<ThrowingRecursionBehavior>()
                .ToList()
                .ForEach(b => fixture.Behaviors.Remove(b));
            fixture.Behaviors.Add(new OmitOnRecursionBehavior(recursionDepth: 2));
            // Create some random variant value
            fixture.Register(() => Json.FromObject(Activator.CreateInstance(type)));
            // JsonNode dynamic values are not round-tripped by the fixture serializers
            fixture.Register<JsonNode>(() => null!);
            // Ensure utc datetimes
            fixture.Register(() => DateTimeOffset.UtcNow);
            fixture.Register(() => DateTime.UtcNow);
            var builder = new SpecimenContext(fixture);
            var instance = ((IEnumerable)builder.Resolve(
                new MultipleRequest(new SeededRequest(type, null)))).Cast<object>().ToArray();

            var buffer = Json.SerializeObjectToMemory(instance, instance.GetType());
            var result = Json.Deserialize(Json.ContentEncoding.GetString(buffer.Span), type.MakeArrayType());

            result.Should().BeEquivalentTo(instance, options => options.AllowingInfiniteRecursion());
        }

        [Fact]
        public void DataContractEnumsUseClosedAotSafeConverters()
        {
            var value = ConnectionOptions.UseReverseConnect |
                ConnectionOptions.DumpDiagnostics;

            Assert.Equal("\"UseReverseConnect, DumpDiagnostics\"",
                JsonSerializer.Serialize(value, Json.Options));
            Assert.Equal(value, JsonSerializer.Deserialize<ConnectionOptions>(
                "\"usereverseconnect, dumpdiagnostics\"", Json.Options));
            Assert.Equal((ConnectionOptions)17,
                JsonSerializer.Deserialize<ConnectionOptions>("17", Json.Options));
            Assert.Throws<JsonException>(() => JsonSerializer.Deserialize<ConnectionOptions>(
                "\"not-an-option\"", Json.Options));
        }

        [Fact]
        public void ShippingSerializationRootsResolveWithoutReflection()
        {
            var roots = TypeFixture.GetDataContractTypes()
                .Select(values => (Type)values[0])
                .Append(typeof(DiscoveryEventModel))
                .Append(typeof(List<PublishedNodesEntryModel>))
                .Append(typeof(IEnumerable<PublishedNodesEntryModel>));

            foreach (var root in roots.Distinct())
            {
                var typeInfo = Json.Options.GetTypeInfo(root);
                Assert.Equal("PublisherModelsJsonContext",
                    typeInfo.OriginatingResolver?.GetType().Name);
            }

            Assert.Throws<NotSupportedException>(() =>
                Json.Options.GetTypeInfo(typeof(ReflectionOnlyModel)));
        }

        [Fact]
        public void DataContractRequiredMembersRemainLenient()
        {
            var result = Json.Deserialize<PublishedNodesEntryModel>("{}");

            Assert.NotNull(result);
            Assert.Null(result.EndpointUrl);
        }

        [Theory]
        [InlineData("\"pubsub\"", MessagingMode.PubSub)]
        [InlineData("\"FullNetworkMessages\"", MessagingMode.FullNetworkMessages)]
        [InlineData("2", MessagingMode.FullNetworkMessages)]
        [InlineData("\"SingleRawDataSet\"", MessagingMode.SingleRawDataSet)]
        public void MessagingModeConverterReadsNamesCaseInsensitivelyAndDefinedNumbers(
            string json, MessagingMode expected)
        {
            var result = JsonSerializer.Deserialize<MessagingMode>(json, Json.Options);

            Assert.Equal(expected, result);
        }

        [Fact]
        public void MessagingModeConverterWritesEnumName()
        {
            var json = JsonSerializer.Serialize(MessagingMode.SingleDataSet, Json.Options);

            Assert.Equal("\"SingleDataSet\"", json);
        }

        [Theory]
        [InlineData("\"Samples\"", "PubSub")]
        [InlineData("\"fullsamples\"", "FullNetworkMessages")]
        public void MessagingModeConverterRejectsRemovedSampleModesWithReplacement(
            string json, string replacement)
        {
            var exception = Assert.Throws<JsonException>(() =>
                JsonSerializer.Deserialize<MessagingMode>(json, Json.Options));

            Assert.Contains("was removed in OPC Publisher 3.0", exception.Message,
                StringComparison.Ordinal);
            Assert.Contains($"Use '{replacement}' instead.", exception.Message,
                StringComparison.Ordinal);
        }

        [Theory]
        [InlineData("true", "A messaging mode must be written as a string.")]
        [InlineData("1", "A messaging mode must be written as a string.")]
        [InlineData("\"unknown\"", "'unknown' is not a known messaging mode.")]
        public void MessagingModeConverterRejectsUnsupportedJsonValues(
            string json, string expectedMessage)
        {
            var exception = Assert.Throws<JsonException>(() =>
                JsonSerializer.Deserialize<MessagingMode>(json, Json.Options));

            Assert.Contains(expectedMessage, exception.Message,
                StringComparison.Ordinal);
        }

        [Fact]
        public void RemovedMessagingModeReplacementLookupHandlesNullAndKnownNames()
        {
            var result = MessagingModeJsonConverter.TryGetRemovedModeReplacement(
                null, out var replacement);

            Assert.Equal(false, result);
            Assert.Equal(string.Empty, replacement);

            result = MessagingModeJsonConverter.TryGetRemovedModeReplacement(
                "Samples", out replacement);

            Assert.Equal(true, result);
            Assert.Equal(nameof(MessagingMode.PubSub), replacement);

            result = MessagingModeJsonConverter.TryGetRemovedModeReplacement(
                "FullSamples", out replacement);

            Assert.Equal(true, result);
            Assert.Equal(nameof(MessagingMode.FullNetworkMessages), replacement);
        }

        [Fact]
        public void SkipValidationAttributeAlwaysReturnsValid()
        {
            var attribute = new SkipValidationAttribute();

            Assert.Equal(true, attribute.IsValid(null));
            Assert.Equal(true, attribute.IsValid(new object()));
        }

        private sealed class ReflectionOnlyModel
        {
        }

    }
}
