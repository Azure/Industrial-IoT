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

    }
}
