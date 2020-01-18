// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IIoT.Module {
    using Microsoft.Azure.IIoT.Module.Default;
    using Microsoft.Azure.IIoT.Module.Models;
    using Microsoft.Azure.IIoT.Diagnostics;
    using Microsoft.Azure.IIoT.Hub;
    using Newtonsoft.Json;
    using System;
    using System.Text;
    using System.Threading.Tasks;
    using System.Threading;
    using Xunit;
    using AutoFixture;

    public class ChunkMethodTests {

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
            var expectedRequest = JsonConvertEx.SerializeObject(new {
                test1 = fixture.Create<string>(),
                test2 = fixture.Create<long>()
            });
            var expectedResponse = JsonConvertEx.SerializeObject(new {
                test1 = fixture.Create<byte[]>(),
                test2 = fixture.Create<string>()
            });
            var server = new TestServer(chunkSize, (method, buffer, type) => {
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

            var server = new TestServer(chunkSize, (method, buffer, type) => {
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
        public class TestServer : IJsonMethodClient, IMethodHandler {

            public TestServer(int size,
                Func<string, byte[], string, byte[]> handler) {
                MaxMethodPayloadCharacterCount = size;
                _handler = handler;
                _server = new ChunkMethodServer(this, TraceLogger.Create());
            }

            public IMethodClient CreateClient() {
                return new ChunkMethodClient(this, TraceLogger.Create());
            }

            public int MaxMethodPayloadCharacterCount { get; }

            public async Task<string> CallMethodAsync(string deviceId,
                string moduleId, string method, string json, TimeSpan? timeout,
                CancellationToken ct) {
                var processed = await _server.ProcessAsync(
                    JsonConvertEx.DeserializeObject<MethodChunkModel>(json));
                return JsonConvertEx.SerializeObject(processed);
            }

            public Task<byte[]> InvokeAsync(string method, byte[] payload, string contentType) {
                return Task.FromResult(_handler.Invoke(method, payload, contentType));
            }

            private readonly ChunkMethodServer _server;
            private readonly Func<string, byte[], string, byte[]> _handler;
        }
    }
}
