﻿// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IIoT.Module.Framework.Hosting {
    using Microsoft.Azure.IIoT.Diagnostics;
    using Microsoft.Azure.IIoT.Http;
    using System;
    using System.Net;
    using System.Net.Http;
    using System.Net.Http.Headers;
    using System.Threading;
    using System.Threading.Tasks;
    using Xunit;

    public class EdgeClientTests {

        [Fact]
        public async Task EdgeLetGetModulesTestAsync() {
            var client = new EdgeletClient(new TestHttpClient(_edgeLetModulesJson),
                "unix://var/tmp/workload.sock", null, null, null, TraceLogger.Create());
            var result = await client.GetModulesAsync("testedge", CancellationToken.None);

            Assert.Collection(result,
                elem => {
                    Assert.NotNull(elem.Status);
                    Assert.Equal("running", elem.Status);
                    Assert.Equal("mcr.microsoft.com/iotedge/opc-twin:latest", elem.ImageName);
                    Assert.Equal("sha256:6d7e2211216d96270281b6a928988d2d320e91c6f4b428d16f54d2a1eb358115", elem.ImageHash);
                    Assert.Equal("opctwin", elem.Id);
                    Assert.Equal("latest", elem.Version);
                },
                elem => {
                    Assert.NotNull(elem.Status);
                    Assert.Equal("running", elem.Status);
                    Assert.Equal("mcr.microsoft.com/iotedge/opc-publisher:latest", elem.ImageName);
                    Assert.Equal("sha256:af35d95a68e8145dd8a15c5947bc4d5e4d78bafd4c057f844ba452b281efbadc", elem.ImageHash);
                    Assert.Equal("opcpublisher", elem.Id);
                    Assert.Equal("latest", elem.Version);
                },
                elem => {
                    Assert.NotNull(elem.Status);
                    Assert.Equal("running", elem.Status);
                    Assert.Equal("mcr.microsoft.com/azureiotedge-agent:1.0", elem.ImageName);
                    Assert.Equal("1.0", elem.Version);
                },
                elem => {
                    Assert.NotNull(elem.Status);
                    Assert.Equal("running", elem.Status);
                    Assert.Equal("mcr.microsoft.com/azureiotedge-hub:1.0", elem.ImageName);
                    Assert.Equal("1.0", elem.Version);
                });
        }

        public static string _edgeLetModulesJson = @"
{
    ""modules"": [
        {
            ""id"": ""id"",
            ""name"": ""opctwin"",
            ""type"": ""docker"",
            ""config"": {
                ""settings"": {
                    ""createOptions"": { ""Labels"": { ""net.azure-devices.edge.owner"": ""Microsoft.Azure.Devices.Edge.Agent"" } },
                    ""image"": ""mcr.microsoft.com/iotedge/opc-twin:latest"",
                    ""imageHash"": ""sha256:6d7e2211216d96270281b6a928988d2d320e91c6f4b428d16f54d2a1eb358115""
                },
                ""env"": []
            },
            ""status"": {
                ""startTime"": ""2019-04-04T13:00:38.362444628+00:00"",
                ""runtimeStatus"": {
                    ""status"": ""running"",
                    ""description"": ""running""
                }
            }
        },
        {
            ""id"": ""id"",
            ""name"": ""opcpublisher"",
            ""type"": ""docker"",
            ""config"": {
                ""settings"": {
                    ""createOptions"": { ""Labels"": { ""net.azure-devices.edge.owner"": ""Microsoft.Azure.Devices.Edge.Agent"" } },
                    ""image"": ""mcr.microsoft.com/iotedge/opc-publisher:latest"",
                    ""imageHash"": ""sha256:af35d95a68e8145dd8a15c5947bc4d5e4d78bafd4c057f844ba452b281efbadc""
                },
                ""env"": []
            },
            ""status"": {
                ""startTime"": ""2019-04-04T09:17:53.904941975+00:00"",
                ""exitStatus"": {
                    ""exitTime"": ""2019-04-04T09:17:37.581579597+00:00"",
                    ""statusCode"": ""0""
                },
                ""runtimeStatus"": {
                    ""status"": ""running"",
                    ""description"": ""running""
                }
            }
        },
        {
            ""id"": ""id"",
            ""name"": ""edgeAgent"",
            ""type"": ""docker"",
            ""config"": {
                ""settings"": {
                    ""createOptions"": { ""Labels"": { ""net.azure-devices.edge.owner"": ""Microsoft.Azure.Devices.Edge.Agent"" } },
                    ""image"": ""mcr.microsoft.com/azureiotedge-agent:1.0"",
                    ""imageHash"": ""sha256:e4917eb97425815862809020fd5968c67dea666df19150b9ad989924ed7f7561""
                },
                ""env"": []
            },
            ""status"": {
                ""startTime"": ""2019-04-04T09:17:48.533976645+00:00"",
                ""exitStatus"": {
                    ""exitTime"": ""2019-04-04T09:17:47.879363982+00:00"",
                    ""statusCode"": ""0""
                },
                ""runtimeStatus"": {
                    ""status"": ""running"",
                    ""description"": ""running""
                }
            }
        },
        {
            ""id"": ""id"",
            ""name"": ""edgeHub"",
            ""type"": ""docker"",
            ""config"": {
                ""settings"": {
                    ""createOptions"": { ""Labels"": { ""net.azure-devices.edge.owner"": ""Microsoft.Azure.Devices.Edge.Agent"" } },
                    ""image"": ""mcr.microsoft.com/azureiotedge-hub:1.0"",
                    ""imageHash"": ""sha256:aa5599173272887a368a879ca7452bdc2123c66d30d8247a46f2a858dd740444""
                },
                ""env"": []
            },
            ""status"": {
                ""startTime"": ""2019-04-04T09:17:59.862843939+00:00"",
                ""exitStatus"": {
                    ""exitTime"": ""2019-04-04T09:17:47.477202782+00:00"",
                    ""statusCode"": ""0""
                },
                ""runtimeStatus"": {
                    ""status"": ""running"",
                    ""description"": ""running""
                }
            }
        }
    ]
}
";

        private class TestHttpClient : IHttpClient, IHttpResponse, IHttpRequest {

            public TestHttpClient(string response) {
                _response = response;
            }
            public Task<IHttpResponse> DeleteAsync(IHttpRequest request, CancellationToken ct) {
                throw new NotImplementedException();
            }

            public Task<IHttpResponse> GetAsync(IHttpRequest request, CancellationToken ct) {
                return Task.FromResult<IHttpResponse>(this);
            }

            public Task<IHttpResponse> HeadAsync(IHttpRequest request, CancellationToken ct) {
                throw new NotImplementedException();
            }

            public IHttpRequest NewRequest(Uri uri, string resourceId = null) {
                Uri = uri;
                return this;
            }

            public Task<IHttpResponse> OptionsAsync(IHttpRequest request, CancellationToken ct) {
                throw new NotImplementedException();
            }

            public Task<IHttpResponse> PatchAsync(IHttpRequest request, CancellationToken ct) {
                throw new NotImplementedException();
            }

            public Task<IHttpResponse> PostAsync(IHttpRequest request, CancellationToken ct) {
                throw new NotImplementedException();
            }

            public Task<IHttpResponse> PutAsync(IHttpRequest request, CancellationToken ct) {
                throw new NotImplementedException();
            }

            public string ResourceId => throw new NotImplementedException();

            public HttpStatusCode StatusCode => HttpStatusCode.OK;

            public HttpResponseHeaders Headers => throw new NotImplementedException();

            public HttpContentHeaders ContentHeaders => throw new NotImplementedException();

            public byte[] Content => System.Text.Encoding.UTF8.GetBytes(_response);

            public Uri Uri { get; private set; }

            HttpRequestHeaders IHttpRequest.Headers => _request.Headers;

            HttpContent IHttpRequest.Content {
                get => _request.Content;
                set => _request.Content = value;
            }

            public HttpRequestOptions Options { get; } = new HttpRequestOptions();

            private readonly string _response;
            private readonly HttpRequestMessage _request = new HttpRequestMessage();
        }
    }
}
