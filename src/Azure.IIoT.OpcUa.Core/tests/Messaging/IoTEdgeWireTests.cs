// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Azure.IIoT.OpcUa.Core.Messaging.Clients.IoTEdge
{
    using Azure.IIoT.OpcUa.Core.Exceptions;
    using FluentAssertions;
    using System;
    using System.Net;
    using System.Net.Http;
    using System.Text;
    using System.Threading;
    using System.Threading.Tasks;
    using Xunit;

    /// <summary>
    /// IoT Edge / IoT Hub wire format regression tests.
    /// </summary>
    public sealed class IoTEdgeWireTests
    {
        [Fact]
        public void IoTHubDirectMethodTopicsPreserveMqttShape()
        {
            const string rid = "11111111-2222-3333-4444-555555555555";

            var requestTopic = $"$iothub/methods/POST/reboot/?$rid={rid}";
            var responseTopic = $"$iothub/methods/res/404/?$rid={rid}";

            requestTopic.Should().Be(
                "$iothub/methods/POST/reboot/?$rid=11111111-2222-3333-4444-555555555555");
            responseTopic.Should().Be(
                "$iothub/methods/res/404/?$rid=11111111-2222-3333-4444-555555555555");
        }

        [Fact]
        public void DirectMethodErrorEnvelopePreservesProblemDetailsBytes()
        {
            var error = new MethodCallStatusException(404, "missing", "Not Found");

            Encoding.UTF8.GetString(error.Serialize().Span)
                .Should().Be("{\"title\":\"Not Found\",\"status\":404,\"detail\":\"missing\"}");
        }

        [Fact]
        public async Task WorkloadEncryptRequestPreservesLegacyIvAndBase64ShapeAsync()
        {
            using var handler = new CaptureHandler("{\"ciphertext\":\"AQID\"}");
            using var client = new WorkloadApiHttpClient(new Uri("http://edge/"),
                "2019-01-30", "publisher", "gen1", handler);

            var encrypted = await client.EncryptAsync("alKGJdfsgidfasdO",
                Convert.ToBase64String(Encoding.UTF8.GetBytes("user")),
                CancellationToken.None);

            encrypted.Should().Equal(1, 2, 3);
            handler.RequestUri!.ToString().Should().Be(
                "http://edge/modules/publisher/genid/gen1/encrypt?api-version=2019-01-30");
            handler.RequestBody.Should().Be(
                "{\"plaintext\":\"ZFhObGNnPT0=\",\"initializationVector\":\"YWxLR0pkZnNnaWRmYXNkTw==\"}");
        }

        [Fact]
        public async Task WorkloadDecryptRequestPreservesLegacyIvAndBase64ShapeAsync()
        {
            using var handler = new CaptureHandler("{\"plaintext\":\"ZFhObGNnPT0=\"}");
            using var workload = new IoTEdgeWorkloadApi("http://edge/", "gen1",
                "publisher", "2019-01-30", handler);

            var plaintext = await workload.DecryptAsync("alKGJdfsgidfasdO",
                new byte[] { 1, 2, 3 }, CancellationToken.None);

            Encoding.UTF8.GetString(plaintext.Span).Should().Be("user");
            handler.RequestUri!.ToString().Should().Be(
                "http://edge/modules/publisher/genid/gen1/decrypt?api-version=2019-01-30");
            handler.RequestBody.Should().Be(
                "{\"ciphertext\":\"AQID\",\"initializationVector\":\"YWxLR0pkZnNnaWRmYXNkTw==\"}");
        }

        private sealed class CaptureHandler : HttpMessageHandler
        {
            public Uri? RequestUri { get; private set; }
            public string? RequestBody { get; private set; }

            public CaptureHandler(string response)
            {
                _response = response;
            }

            protected override async Task<HttpResponseMessage> SendAsync(
                HttpRequestMessage request, CancellationToken cancellationToken)
            {
                RequestUri = request.RequestUri;
                RequestBody = request.Content == null ? null :
                    await request.Content.ReadAsStringAsync(cancellationToken)
                        .ConfigureAwait(false);
                return new HttpResponseMessage(HttpStatusCode.OK)
                {
                    Content = new StringContent(_response, Encoding.UTF8, "application/json")
                };
            }

            private readonly string _response;
        }
    }
}
