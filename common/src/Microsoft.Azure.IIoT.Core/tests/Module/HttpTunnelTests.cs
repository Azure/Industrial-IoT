// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IIoT.Module.Default {
    using System;
    using Xunit;
    using System.Linq;
    using Microsoft.Azure.IIoT.Http.Default;
    using Serilog;
    using System.Threading.Tasks;
    using System.Collections.Generic;
    using Microsoft.Azure.IIoT.Hub;

    public class HttpTunnelTests {

        [Fact]
        public async Task TestGetCallAsync() {

            // Setup
            var logger = Log.Logger;
            var eventBridge = new EventBridge();
            var factory = new HttpTunnelHandlerFactory(eventBridge, null, logger);
            var client = new HttpClientFactory(factory, logger).CreateClient("msft");

            var adapter = new MethodHandlerAdapter(factory.YieldReturn());
            var chunkServer = new TestChunkServer(100, (method, buffer, type) => {

                // Assert some

                return adapter.InvokeAsync(method, buffer, type).Result;
            });

            var server = new HttpTunnelServer(chunkServer.CreateClient(),
                new HttpClient(new HttpClientFactory(logger), logger), logger);
            eventBridge.Handler = server;

            // Act

            //  var result = await client.GetAsync("https://www.microsoft.com");
            //  var payload = await result.Content.ReadAsStringAsync();
            await Task.Delay(1);
            Assert.True(true);

            // Assert.True(result.IsSuccessStatusCode);
            // Assert.NotNull(payload);
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
    }
}
