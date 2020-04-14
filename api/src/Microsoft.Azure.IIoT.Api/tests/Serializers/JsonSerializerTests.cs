//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IIoT.Api.Json {
    using AutoFixture;
    using AutoFixture.Kernel;
    using FluentAssertions;
    using Microsoft.Azure.IIoT.Serializers;
    using Microsoft.Azure.IIoT.Serializers.NewtonSoft;
    using Newtonsoft.Json;
    using Newtonsoft.Json.Linq;
    using System;
    using System.Collections;
    using System.Linq;
    using Xunit;

    public class JsonSerializerTests {

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
        public void SerializerDeserializeScalarTypeToString(Type type) {

            var instance = Activator.CreateInstance(type);

            var str = _serializer.SerializeToString(instance);
            var result = _serializer.Deserialize(str, type);

            result.Should().BeEquivalentTo(instance);
            var expectedString = JsonConvert.SerializeObject(instance, _serializer.Settings);
            str.Should().Be(expectedString);
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

        private readonly NewtonSoftJsonSerializer _serializer = new NewtonSoftJsonSerializer();
    }
}
