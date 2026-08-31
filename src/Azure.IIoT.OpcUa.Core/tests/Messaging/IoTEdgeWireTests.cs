// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Azure.IIoT.OpcUa.Core.Messaging.Clients.IoTEdge
{
    using Azure.IIoT.OpcUa.Core.Exceptions;
    using FluentAssertions;
    using System;
    using System.Collections.Generic;
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
        public async Task WorkloadCryptoFacadeDelegatesAndPreservesOutputShapeAsync()
        {
            using var handler = new CaptureHandler(
                "{\"ciphertext\":\"AQID\"}",
                "{\"plaintext\":\"ZFhObGNnPT0=\"}");
            using var workload = new IoTEdgeWorkloadApi("http://edge/", "gen1",
                "publisher", "2019-01-30", handler);

            var encrypted = await workload.EncryptAsync("alKGJdfsgidfasdO",
                Encoding.UTF8.GetBytes("user"),
                CancellationToken.None);

            encrypted.ToArray().Should().Equal(1, 2, 3);
            var plaintext = await workload.DecryptAsync("alKGJdfsgidfasdO",
                encrypted, CancellationToken.None);

            Encoding.UTF8.GetString(plaintext.Span).Should().Be("user");
            handler.RequestCount.Should().Be(2);
        }

        private sealed class CaptureHandler : HttpMessageHandler
        {
            public CaptureHandler(params string[] responses)
            {
                _responses = new Queue<string>(responses);
            }

            protected override Task<HttpResponseMessage> SendAsync(
                HttpRequestMessage request, CancellationToken cancellationToken)
            {
                Interlocked.Increment(ref _requestCount);
                return Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK)
                {
                    Content = new StringContent(_responses.Dequeue(), Encoding.UTF8,
                        "application/json")
                });
            }

            public int RequestCount => _requestCount;

            private readonly Queue<string> _responses;
            private int _requestCount;
        }
    }
}
