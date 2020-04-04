//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IIoT.Api.MsgPack {
    using Microsoft.Azure.IIoT.Serializers;
    using Microsoft.Azure.IIoT.Serializers.MessagePack;
    using Newtonsoft.Json.Linq;
    using System;
    using Xunit;
    using AutoFixture;
    using AutoFixture.Kernel;
    using FluentAssertions;
    using System.Linq;
    using System.Collections;

    public class MsgPackSerializerTests {
        [Theory]
        [MemberData(nameof(TypeFixture.GetDataContractTypes), MemberType = typeof(TypeFixture))]
        public void SerializerDeserializeScalarTypeToBuffer(Type type) {

            var instance = Activator.CreateInstance(type);

            var buffer = _serializer.SerializeToBytes(instance);
            var result = _serializer.Deserialize(buffer.ToArray(), type);

            result.Should().BeEquivalentTo(instance);
        }

        [Theory]
        [MemberData(nameof(TypeFixture.GetDataContractTypes), MemberType = typeof(TypeFixture))]
        public void SerializerDeserializeScalarTypeToBufferWithFixture(Type type) {

            var fixture = new Fixture();
            // Create some random variant value
            fixture.Register(() => _serializer.FromObject(Activator.CreateInstance(type)));
            // Ensure utc datetimes
            fixture.Register(() => DateTime.UtcNow);
            var instance = new SpecimenContext(fixture).Resolve(new SeededRequest(type, null));

            var buffer = _serializer.SerializeToBytes(instance);
            var result = _serializer.Deserialize(buffer.ToArray(), type);

            result.Should().BeEquivalentTo(instance);
        }

        [Theory]
        [MemberData(nameof(TypeFixture.GetDataContractTypes), MemberType = typeof(TypeFixture))]
        public void SerializerDeserializeArrayTypeToBufferWithFixture(Type type) {

            var fixture = new Fixture();
            // Create some random variant value
            fixture.Register(() => _serializer.FromObject(Activator.CreateInstance(type)));
            // Ensure utc datetimes
            fixture.Register(() => DateTime.UtcNow);
            var builder = new SpecimenContext(fixture);
            var instance = Enumerable.Cast<object>((IEnumerable)builder.Resolve(
                new MultipleRequest(new SeededRequest(type, null)))).ToArray();

            var buffer = _serializer.SerializeToBytes(instance);
            var result = _serializer.Deserialize(buffer.ToArray(), type.MakeArrayType());

            result.Should().BeEquivalentTo(instance);
        }

        private readonly IBinarySerializer _serializer = new MessagePackSerializer();
    }
}
