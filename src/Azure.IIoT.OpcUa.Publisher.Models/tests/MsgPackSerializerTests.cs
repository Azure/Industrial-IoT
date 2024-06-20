//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Azure.IIoT.OpcUa.Publisher.Models.Tests
{
    using AutoFixture;
    using AutoFixture.Kernel;
    using FluentAssertions;
    using Furly.Extensions.Serializers;
    using Furly.Extensions.Serializers.MessagePack;
    using System;
    using System.Collections;
    using System.Collections.Generic;
    using System.Linq;
    using Xunit;

    public class MsgPackSerializerTests
    {
        [Theory]
        [MemberData(nameof(TypeFixture.GetDataContractTypes), MemberType = typeof(TypeFixture))]
        public void SerializerDeserializeScalarTypeToBuffer(Type type)
        {
            var instance = Activator.CreateInstance(type);

            var buffer = _serializer.SerializeObjectToMemory(instance, type);
            var result = _serializer.Deserialize(buffer.ToArray(), type);

            result.Should().BeEquivalentTo(instance);
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
            fixture.Register(() => _serializer.FromObject(Activator.CreateInstance(type)));
            // Ensure utc datetimes
            fixture.Register(() => DateTimeOffset.UtcNow);
            fixture.Register(() => DateTime.UtcNow);
            var instance = new SpecimenContext(fixture).Resolve(new SeededRequest(type, null));

            var buffer = _serializer.SerializeObjectToMemory(instance, type);
            var result = _serializer.Deserialize(buffer.ToArray(), type);

            result.Should().BeEquivalentTo(instance, options => options.AllowingInfiniteRecursion());
        }

        [Theory]
        [MemberData(nameof(TypeFixture.GetDataContractTypes), MemberType = typeof(TypeFixture))]
        public void SerializerDeserializeArrayTypeToBufferWithFixture(Type type)
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
            fixture.Register(() => _serializer.FromObject(Activator.CreateInstance(type)));
            // Ensure utc datetimes
            fixture.Register(() => DateTimeOffset.UtcNow);
            fixture.Register(() => DateTime.UtcNow);
            var builder = new SpecimenContext(fixture);
            var instance = ((IEnumerable)builder.Resolve(
                new MultipleRequest(new SeededRequest(type, null)))).Cast<object>().ToArray();

            var buffer = _serializer.SerializeObjectToMemory(instance, instance.GetType());
            var result = _serializer.Deserialize(buffer.ToArray(), type.MakeArrayType());

            result.Should().BeEquivalentTo(instance, options => options.AllowingInfiniteRecursion());
        }

        private readonly MessagePackSerializer _serializer = new();
    }
}
