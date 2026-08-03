// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Azure.IIoT.OpcUa.Core.Messaging.Clients.IoTEdge
{
    using System;
    using System.Collections.Generic;
    using System.Net;
    using System.Net.Http;
    using System.Text;
    using System.Text.Json;
    using System.Threading;
    using System.Threading.Tasks;
    using Xunit;

    public sealed class IoTEdgeWorkloadApiTests
    {
        [Fact]
        public async Task ConstructorWithoutEdgeConfigurationLeavesApiUnavailableAsync()
        {
            using var handler = new RecordingHandler("{}");
            using var api = new IoTEdgeWorkloadApi(null, null, null, null,
                handler);

            Assert.False(api.IsAvailable);
            await Assert.ThrowsAsync<NotSupportedException>(async () =>
                await api.SignAsync(new byte[] { 1, 2, 3 }));
            await Assert.ThrowsAsync<NotSupportedException>(async () =>
                await api.EncryptAsync("iv", new byte[] { 1, 2, 3 }));
        }

        [Theory]
        [InlineData("/var/run/iotedge/workload.sock", "unix:///var/run/iotedge/workload.sock")]
        [InlineData("unix:///var/run/iotedge/workload.sock", "unix:///var/run/iotedge/workload.sock")]
        [InlineData("npipe://./pipe/iotedge-workload", "npipe://./pipe/iotedge-workload")]
        [InlineData("http://edge/workload", "http://edge/workload")]
        public void CreateWorkloadUriAcceptsSupportedAbsoluteForms(
            string value, string expected)
        {
            var uri = WorkloadApiHttpClient.CreateWorkloadUri(value);

            Assert.Equal(expected, uri.AbsoluteUri);
        }

        [Fact]
        public void CreateWorkloadUriRejectsInvalidValues()
        {
            Assert.Throws<InvalidOperationException>(() =>
                WorkloadApiHttpClient.CreateWorkloadUri("not a uri"));
        }

        [Fact]
        public void CreateRequestUriUsesTcpBasePathAndEscapesQuery()
        {
            using var client = new WorkloadApiHttpClient(
                new Uri("http://edge/workload"), "2020-01-01 preview",
                "module/id", "gen 1", new RecordingHandler("{}"));

            var uri = client.CreateRequestUriForTest("certificate/server");

            Assert.Equal("http://edge/workload/certificate/server?api-version=2020-01-01%20preview",
                uri.AbsoluteUri);
        }

        [Theory]
        [InlineData("unix:///var/run/iotedge/workload.sock")]
        [InlineData("npipe://./pipe/iotedge-workload")]
        public void CreateRequestUriUsesLocalhostBaseForSocketTransports(
            string workloadUri)
        {
            using var client = new WorkloadApiHttpClient(new Uri(workloadUri),
                "2019-01-30", "module", "generation", new RecordingHandler("{}"));

            var uri = client.CreateRequestUriForTest("trust-bundle");

            Assert.Equal("http://localhost/trust-bundle?api-version=2019-01-30",
                uri.AbsoluteUri);
        }

        [Theory]
        [InlineData("http://edge/", "", "generation", "apiVersion")]
        [InlineData("http://edge/", "module", "", "apiVersion")]
        [InlineData("http://edge/", "module", "generation", "")]
        public void WorkloadClientConstructorRejectsMissingRequiredValues(
            string workloadUri, string moduleId, string generationId, string apiVersion)
        {
            using var handler = new RecordingHandler("{}");

            Assert.Throws<ArgumentException>(() => new WorkloadApiHttpClient(
                workloadUri, apiVersion, moduleId, generationId, handler));
        }

        [Fact]
        public async Task SignAsyncPostsLegacyPayloadShapeAsync()
        {
            using var handler = new RecordingHandler("{\"digest\":\"BAUG\"}");
            using var client = new WorkloadApiHttpClient(
                new Uri("http://edge/workload"), "2020-01-01",
                "module/id", "gen 1", handler);

            var digest = await client.SignAsync("secondary", "HMACSHA256",
                "YWJj", CancellationToken.None);

            Assert.Equal(new byte[] { 4, 5, 6 }, digest);
            var request = Assert.Single(handler.Requests);
            Assert.Equal(HttpMethod.Post, request.Method);
            Assert.Equal("http://edge/workload/modules/module%2Fid/genid/gen%201/sign?api-version=2020-01-01",
                request.Uri.AbsoluteUri);
            using var document = JsonDocument.Parse(request.Body);
            var root = document.RootElement;
            Assert.Equal("secondary", root.GetProperty("keyId").GetString());
            Assert.Equal("HMACSHA256", root.GetProperty("algo").GetString());
            Assert.Equal(Convert.ToBase64String(Encoding.UTF8.GetBytes("YWJj")),
                root.GetProperty("data").GetString());
        }

        [Fact]
        public async Task FacadeSignUsesDefaultsAndEncodesInputAsync()
        {
            using var handler = new RecordingHandler("{\"digest\":\"AQID\"}");
            using var api = new IoTEdgeWorkloadApi("http://edge/", "generation",
                "publisher", "2019-01-30", handler);

            var digest = await api.SignAsync(Encoding.UTF8.GetBytes("payload"));

            Assert.True(api.IsAvailable);
            Assert.Equal(new byte[] { 1, 2, 3 }, digest.ToArray());
            var request = Assert.Single(handler.Requests);
            using var document = JsonDocument.Parse(request.Body);
            var root = document.RootElement;
            Assert.Equal("primary", root.GetProperty("keyId").GetString());
            Assert.Equal("HMACSHA256", root.GetProperty("algo").GetString());
            Assert.Equal(Convert.ToBase64String(Encoding.UTF8.GetBytes(
                Convert.ToBase64String(Encoding.UTF8.GetBytes("payload")))),
                root.GetProperty("data").GetString());
        }

        [Fact]
        public async Task GetTrustBundleAsyncSendsGetAndReturnsCertificateAsync()
        {
            using var handler = new RecordingHandler("{\"certificate\":\"pem\"}");
            using var client = new WorkloadApiHttpClient(
                new Uri("http://edge/workload"), "2019-01-30",
                "module", "generation", handler);

            var pem = await client.GetTrustBundleAsync(CancellationToken.None);

            Assert.Equal("pem", pem);
            var request = Assert.Single(handler.Requests);
            Assert.Equal(HttpMethod.Get, request.Method);
            Assert.Equal("http://edge/workload/trust-bundle?api-version=2019-01-30",
                request.Uri.AbsoluteUri);
        }

        [Fact]
        public async Task GetManifestTrustBundleAsyncSendsGetAndReturnsCertificateAsync()
        {
            using var handler = new RecordingHandler("{\"certificate\":\"manifest\"}");
            using var client = new WorkloadApiHttpClient(
                new Uri("http://edge/workload"), "2019-01-30",
                "module", "generation", handler);

            var pem = await client.GetManifestTrustBundleAsync(CancellationToken.None);

            Assert.Equal("manifest", pem);
            var request = Assert.Single(handler.Requests);
            Assert.Equal(HttpMethod.Get, request.Method);
            Assert.Equal("http://edge/workload/manifest-trust-bundle?api-version=2019-01-30",
                request.Uri.AbsoluteUri);
        }

        [Fact]
        public async Task NullTrustBundleCertificateMapsToEmptyStringAsync()
        {
            using var handler = new RecordingHandler("{\"certificate\":null}");
            using var client = new WorkloadApiHttpClient(
                new Uri("http://edge/workload"), "2019-01-30",
                "module", "generation", handler);

            var pem = await client.GetTrustBundleAsync(CancellationToken.None);

            Assert.Equal(string.Empty, pem);
        }

        [Fact]
        public async Task EmptyTrustBundleFacadeReturnsEmptyCollectionAsync()
        {
            using var handler = new RecordingHandler("{\"certificate\":\"\"}");
            using var api = new IoTEdgeWorkloadApi("http://edge/", "generation",
                "publisher", "2019-01-30", handler);

            var certificates = await api.GetTrustBundleAsync();

            Assert.Empty(certificates);
        }

        [Fact]
        public async Task CreateServerCertificateAsyncPostsRequestAsync()
        {
            var expiration = new DateTime(2030, 1, 2, 3, 4, 5, DateTimeKind.Utc);
            using var handler = new RecordingHandler(
                "{\"privateKey\":{\"type\":\"key\",\"ref\":\"ref\",\"bytes\":null}," +
                "\"certificate\":\"pem\",\"expiration\":\"2030-01-02T03:04:05Z\"}");
            using var client = new WorkloadApiHttpClient(
                new Uri("http://edge/workload"), "2019-01-30",
                "module", "generation", handler);

            var response = await client.CreateServerCertificateAsync("publisher",
                expiration, CancellationToken.None);

            Assert.Equal("pem", response.Certificate);
            Assert.Equal("key", response.PrivateKey?.Type);
            Assert.Equal("ref", response.PrivateKey?.Ref);
            var request = Assert.Single(handler.Requests);
            Assert.Equal(HttpMethod.Post, request.Method);
            Assert.Equal("http://edge/workload/modules/module/genid/generation/certificate/server?api-version=2019-01-30",
                request.Uri.AbsoluteUri);
            using var document = JsonDocument.Parse(request.Body);
            var root = document.RootElement;
            Assert.Equal("publisher", root.GetProperty("commonName").GetString());
            Assert.Equal(expiration, root.GetProperty("expiration").GetDateTime());
        }

        [Fact]
        public async Task NullJsonResponseThrowsInvalidOperationExceptionAsync()
        {
            using var handler = new RecordingHandler("null");
            using var client = new WorkloadApiHttpClient(
                new Uri("http://edge/workload"), "2019-01-30",
                "module", "generation", handler);

            await Assert.ThrowsAsync<InvalidOperationException>(async () =>
                await client.GetTrustBundleAsync(CancellationToken.None));
        }

        [Fact]
        public async Task NonSuccessResponseThrowsHttpRequestExceptionAsync()
        {
            using var handler = new RecordingHandler("{}", HttpStatusCode.BadGateway);
            using var client = new WorkloadApiHttpClient(
                new Uri("http://edge/workload"), "2019-01-30",
                "module", "generation", handler);

            await Assert.ThrowsAsync<HttpRequestException>(async () =>
                await client.GetTrustBundleAsync(CancellationToken.None));
        }

        private sealed class RecordingHandler : HttpMessageHandler
        {
            public RecordingHandler(string response,
                HttpStatusCode statusCode = HttpStatusCode.OK)
            {
                _responses = new Queue<(string, HttpStatusCode)>(
                    [(response, statusCode)]);
            }

            public List<RequestRecord> Requests { get; } = [];

            protected override async Task<HttpResponseMessage> SendAsync(
                HttpRequestMessage request, CancellationToken cancellationToken)
            {
                var body = request.Content == null ? string.Empty :
                    await request.Content.ReadAsStringAsync(cancellationToken)
                        .ConfigureAwait(false);
                Requests.Add(new RequestRecord(request.Method, request.RequestUri!, body));
                var (response, statusCode) = _responses.Dequeue();
                return new HttpResponseMessage(statusCode)
                {
                    Content = new StringContent(response, Encoding.UTF8,
                        "application/json")
                };
            }

            private readonly Queue<(string Response, HttpStatusCode StatusCode)> _responses;
        }

        private sealed record class RequestRecord(HttpMethod Method, Uri Uri, string Body);
    }
}
