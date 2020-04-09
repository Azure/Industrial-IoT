// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IIoT.Module {
    using Microsoft.Azure.IIoT.Serializers;
    using Microsoft.Azure.IIoT.Serializers.NewtonSoft;
    using System;
    using System.Text;
    using Xunit;
    using AutoFixture;

    public partial class ChunkMethodTests {

        [Theory]
        [InlineData(120 * 1024)]
        [InlineData(100000)]
        [InlineData(20)]
        [InlineData(13)]
        [InlineData(1)]
        [InlineData(0)]
        public void SendReceiveJsonTestWithVariousChunkSizes(int chunkSize) {
            var fixture = new Fixture();

            var expectedMethod = fixture.Create<string>();
            var expectedContentType = fixture.Create<string>();
            var expectedRequest = _serializer.SerializeToString(new {
                test1 = fixture.Create<string>(),
                test2 = fixture.Create<long>()
            });
            var expectedResponse = _serializer.SerializeToString(new {
                test1 = fixture.Create<byte[]>(),
                test2 = fixture.Create<string>()
            });
            var server = new TestChunkServer(_serializer, chunkSize, (method, buffer, type) => {
                Assert.Equal(expectedMethod, method);
                Assert.Equal(expectedContentType, type);
                Assert.Equal(expectedRequest, Encoding.UTF8.GetString(buffer));
                return Encoding.UTF8.GetBytes(expectedResponse);
            });
            var result = server.CreateClient().CallMethodAsync(
                fixture.Create<string>(), fixture.Create<string>(), expectedMethod,
                Encoding.UTF8.GetBytes(expectedRequest), expectedContentType).Result;
            Assert.Equal(expectedResponse, Encoding.UTF8.GetString(result));
        }

        [Theory]
        [InlineData(455585)]
        [InlineData(300000)]
        [InlineData(233433)]
        [InlineData(200000)]
        [InlineData(100000)]
        [InlineData(120 * 1024)]
        [InlineData(99)]
        [InlineData(13)]
        [InlineData(20)]
        [InlineData(0)]
        public void SendReceiveLargeBufferTestWithVariousChunkSizes(int chunkSize) {
            var fixture = new Fixture();

            var expectedMethod = fixture.Create<string>();
            var expectedContentType = fixture.Create<string>();

            var expectedRequest = new byte[200000];
            kR.NextBytes(expectedRequest);
            var expectedResponse = new byte[300000];
            kR.NextBytes(expectedResponse);

            var server = new TestChunkServer(_serializer, chunkSize, (method, buffer, type) => {
                Assert.Equal(expectedMethod, method);
                Assert.Equal(expectedContentType, type);
                Assert.Equal(expectedRequest, buffer);
                return expectedResponse;
            });
            var result = server.CreateClient().CallMethodAsync(
                fixture.Create<string>(), fixture.Create<string>(), expectedMethod,
                expectedRequest, expectedContentType).Result;
            Assert.Equal(expectedResponse, result);
        }

        private static readonly Random kR = new Random();
        private readonly IJsonSerializer _serializer = new NewtonSoftJsonSerializer();
    }
}
