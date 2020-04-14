// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IIoT.Module.Default {
    using Microsoft.Azure.IIoT.Http;
    using Microsoft.Azure.IIoT.Http.Default;
    using Microsoft.Azure.IIoT.Serializers;
    using Microsoft.Azure.IIoT.Serializers.NewtonSoft;
    using Microsoft.Azure.IIoT.Hub;
    using Microsoft.Azure.IIoT.Messaging;
    using AutoFixture;
    using Moq;
    using Serilog;
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Threading;
    using System.Threading.Tasks;
    using Xunit;
    using System.Net.Http;

    public class HttpTunnelTests {

        [Fact]
        public async Task TestGetWebAsync() {

            // Setup
            var logger = Log.Logger;
            var eventBridge = new EventBridge();
            var factory = new HttpTunnelHandlerFactory(eventBridge, _serializer, null, logger);
            var client = new HttpClientFactory(factory, logger).CreateClient("msft");

            var adapter = new MethodHandlerAdapter(factory.YieldReturn());
            var chunkServer = new TestChunkServer(_serializer, 100, (method, buffer, type) => {
                Assert.Equal(MethodNames.Response, method);
                return adapter.InvokeAsync(method, buffer, type).Result;
            });

            var server = new HttpTunnelServer(
                new Http.Default.HttpClient(new HttpClientFactory(logger), logger),
                chunkServer.CreateClient(), _serializer, logger);
            eventBridge.Handler = server;

            // Act

            var result = await client.GetAsync("https://www.microsoft.com");

            // Assert

            Assert.NotNull(result);
            Assert.True(result.IsSuccessStatusCode);
            Assert.NotNull(result.Content);
            var payload = await result.Content.ReadAsStringAsync();
            Assert.NotNull(payload);
            Assert.NotNull(result.Headers);
            Assert.True(result.Headers.Any());
            Assert.Contains("<!DOCTYPE html>", payload);
        }


        [Fact]
        public async Task TestGetAsync() {

            // Setup
            var logger = Log.Logger;
            var eventBridge = new EventBridge();
            var factory = new HttpTunnelHandlerFactory(eventBridge, _serializer, null, logger);
            var client = new HttpClientFactory(factory, logger).CreateClient("msft");

            var adapter = new MethodHandlerAdapter(factory.YieldReturn());
            var chunkServer = new TestChunkServer(_serializer, 1000, (method, buffer, type) => {
                Assert.Equal(MethodNames.Response, method);
                return adapter.InvokeAsync(method, buffer, type).Result;
            });

            var rand = new Random();
            var fix = new Fixture();
            var responseBuffer = new byte[10000];
            rand.NextBytes(responseBuffer);
            var response = Mock.Of<IHttpResponse>(r =>
                r.Content == responseBuffer &&
                r.StatusCode == System.Net.HttpStatusCode.OK);
            var httpclientMock = Mock.Of<IHttpClient>();
            Mock.Get(httpclientMock)
                .Setup(m => m.GetAsync(It.IsAny<IHttpRequest>(), It.IsAny<CancellationToken>()))
                .Returns(Task.FromResult(response));
            var server = new HttpTunnelServer(httpclientMock,
                chunkServer.CreateClient(), _serializer, logger);
            eventBridge.Handler = server;

            // Act

            var result = await client.GetAsync("https://test/test/test?test=test");

            // Assert

            Assert.NotNull(result);
            Assert.Equal(System.Net.HttpStatusCode.OK, result.StatusCode);
            Assert.NotNull(result.Content);
            var payload = await result.Content.ReadAsByteArrayAsync();
            Assert.Equal(response.Content.Length, payload.Length);
            Assert.Equal(responseBuffer, payload);
            Assert.NotNull(result.Headers);
            Assert.Empty(result.Headers);
        }


        [Theory]
        [InlineData(5 * 1024 * 1024)]
        [InlineData(1000 * 1024)]
        [InlineData(100000)]
        [InlineData(20)]
        [InlineData(13)]
        [InlineData(1)]
        [InlineData(0)]
        public async Task TestPostAsync(int requestSize) {

            // Setup
            var logger = Log.Logger;
            var eventBridge = new EventBridge();
            var factory = new HttpTunnelHandlerFactory(eventBridge, _serializer, null, logger);
            var client = new HttpClientFactory(factory, logger).CreateClient("msft");

            var adapter = new MethodHandlerAdapter(factory.YieldReturn());
            var chunkServer = new TestChunkServer(_serializer, 100000, (method, buffer, type) => {
                Assert.Equal(MethodNames.Response, method);
                return adapter.InvokeAsync(method, buffer, type).Result;
            });

            var uri = new Uri("https://test/test/test?test=test");
            var rand = new Random();
            var fix = new Fixture();
            var requestBuffer = new byte[requestSize];
            rand.NextBytes(requestBuffer);
            var responseBuffer = new byte[10000];
            rand.NextBytes(responseBuffer);
            var request = Mock.Of<IHttpRequest>(r =>
                r.Uri == uri);
            var response = Mock.Of<IHttpResponse>(r =>
                r.Content == responseBuffer &&
                r.StatusCode == System.Net.HttpStatusCode.OK);
            var httpclientMock = Mock.Of<IHttpClient>();
            Mock.Get(httpclientMock)
                .Setup(m => m.PostAsync(It.IsAny<IHttpRequest>(), It.IsAny<CancellationToken>()))
                .Returns(Task.FromResult(response));
            Mock.Get(httpclientMock)
                .Setup(m => m.NewRequest(It.Is<Uri>(u => u == uri), It.IsAny<string>()))
                .Returns(request);

            var server = new HttpTunnelServer(httpclientMock, chunkServer.CreateClient(), _serializer, logger);
            eventBridge.Handler = server;

            // Act

            var result = await client.PostAsync(uri, new ByteArrayContent(requestBuffer));

            // Assert

            Assert.NotNull(result);
            Assert.Equal(System.Net.HttpStatusCode.OK, result.StatusCode);
            Assert.NotNull(result.Content);
            var payload = await result.Content.ReadAsByteArrayAsync();
            Assert.Equal(response.Content.Length, payload.Length);
            Assert.Equal(responseBuffer, payload);
            Assert.NotNull(result.Headers);
            Assert.Empty(result.Headers);
        }

        [Theory]
        [InlineData(5 * 1024 * 1024)]
        [InlineData(1000 * 1024)]
        [InlineData(100000)]
        [InlineData(20)]
        [InlineData(13)]
        [InlineData(1)]
        [InlineData(0)]
        public async Task TestPutAsync(int requestSize) {

            // Setup
            var logger = Log.Logger;
            var eventBridge = new EventBridge();
            var factory = new HttpTunnelHandlerFactory(eventBridge, _serializer, null, logger);
            var client = new HttpClientFactory(factory, logger).CreateClient("msft");

            var adapter = new MethodHandlerAdapter(factory.YieldReturn());
            var chunkServer = new TestChunkServer(_serializer, 128 * 1024, (method, buffer, type) => {
                Assert.Equal(MethodNames.Response, method);
                return adapter.InvokeAsync(method, buffer, type).Result;
            });

            var uri = new Uri("https://test/test/test?test=test");
            var rand = new Random();
            var fix = new Fixture();
            var requestBuffer = new byte[requestSize];
            rand.NextBytes(requestBuffer);
            var request = Mock.Of<IHttpRequest>(r =>
                r.Uri == uri);
            var response = Mock.Of<IHttpResponse>(r =>
                r.Content == null &&
                r.StatusCode == System.Net.HttpStatusCode.OK);
            var httpclientMock = Mock.Of<IHttpClient>();
            Mock.Get(httpclientMock)
                .Setup(m => m.PutAsync(It.IsAny<IHttpRequest>(), It.IsAny<CancellationToken>()))
                .Returns(Task.FromResult(response));
            Mock.Get(httpclientMock)
                .Setup(m => m.NewRequest(It.Is<Uri>(u => u == uri), It.IsAny<string>()))
                .Returns(request);

            var server = new HttpTunnelServer(httpclientMock, chunkServer.CreateClient(), _serializer, logger);
            eventBridge.Handler = server;

            // Act

            var result = await client.PutAsync(uri, new ByteArrayContent(requestBuffer));

            // Assert

            Assert.NotNull(result);
            Assert.Equal(System.Net.HttpStatusCode.OK, result.StatusCode);
            Assert.Null(result.Content);
            Assert.NotNull(result.Headers);
            Assert.Empty(result.Headers);
        }

        [Fact]
        public async Task TestDeleteAsync() {

            // Setup
            var logger = Log.Logger;
            var eventBridge = new EventBridge();
            var factory = new HttpTunnelHandlerFactory(eventBridge, _serializer, null, logger);
            var client = new HttpClientFactory(factory, logger).CreateClient("msft");

            var adapter = new MethodHandlerAdapter(factory.YieldReturn());
            var chunkServer = new TestChunkServer(_serializer, 128 * 1024, (method, buffer, type) => {
                Assert.Equal(MethodNames.Response, method);
                return adapter.InvokeAsync(method, buffer, type).Result;
            });

            var uri = new Uri("https://test/test/test?test=test");
            var rand = new Random();
            var fix = new Fixture();
            var request = Mock.Of<IHttpRequest>(r =>
                r.Uri == uri);
            var response = Mock.Of<IHttpResponse>(r =>
                r.Content == null &&
                r.StatusCode == System.Net.HttpStatusCode.OK);
            var httpclientMock = Mock.Of<IHttpClient>();
            Mock.Get(httpclientMock)
                .Setup(m => m.DeleteAsync(It.IsAny<IHttpRequest>(), It.IsAny<CancellationToken>()))
                .Returns(Task.FromResult(response));
            Mock.Get(httpclientMock)
                .Setup(m => m.NewRequest(It.Is<Uri>(u => u == uri), It.IsAny<string>()))
                .Returns(request);

            var server = new HttpTunnelServer(httpclientMock, chunkServer.CreateClient(), _serializer, logger);
            eventBridge.Handler = server;

            // Act

            var result = await client.DeleteAsync(uri);

            // Assert

            Assert.NotNull(result);
            Assert.Equal(System.Net.HttpStatusCode.OK, result.StatusCode);
            Assert.Null(result.Content);
            Assert.NotNull(result.Headers);
            Assert.Empty(result.Headers);
        }

        public class EventBridge : IEventClient {

            /// <summary>
            /// Handler
            /// </summary>
            public IDeviceTelemetryHandler Handler { get; set; }

            public Task SendEventAsync(byte[] data, string contentType,
                string eventSchema, string contentEncoding) {
                return Handler.HandleAsync("test", "test", data, new Dictionary<string, string> {
                    ["content-type"] = contentType
                }, () => Task.CompletedTask);
            }

            public Task SendEventAsync(IEnumerable<byte[]> batch,
                string contentType, string eventSchema, string contentEncoding) {
                throw new NotImplementedException();
            }
        }

        private readonly IJsonSerializer _serializer = new NewtonSoftJsonSerializer();
    }
}
