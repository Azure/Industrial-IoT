// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Azure.IIoT.OpcUa.Core.Rpc.Router
{
    using Azure.IIoT.OpcUa.Core;
    using Azure.IIoT.OpcUa.Core.Rpc;
    using Azure.IIoT.OpcUa.Core.Rpc.Protocol;
    using Azure.IIoT.OpcUa.Core.Serialization;
    using FluentAssertions;
    using Microsoft.Extensions.Logging.Abstractions;
    using System;
    using System.Buffers;
    using System.Text;
    using System.Text.RegularExpressions;
    using System.Threading;
    using System.Threading.Tasks;
    using Xunit;

    public sealed partial class MethodRouterWireTests
    {
        [Fact]
        public async Task ChunkProtocolAcceptsCaseVariantQuotedNumericEnvelopeAsync()
        {
            await using var router = CreateRouter();
            var client = new ChunkMethodClient(new LenientRouterRpcClient(router),
                NullLogger<ChunkMethodClient>.Instance);
            var request = Json.SerializeToMemory(new EchoRequest { Value = "lenient" });

            var response = await client.CallMethodAsync("target", "Echo_V1",
                request, ContentMimeType.Json, null, CancellationToken.None);

            Json.Deserialize<EchoResponse>(response)!.Value.Should().Be("lenient");
        }

        private sealed class LenientRouterRpcClient : IRpcClient
        {
            public string Name => "lenient-test";
            public int MaxMethodPayloadSizeInBytes => 256 * 1024;

            public LenientRouterRpcClient(MethodRouter router)
            {
                _router = router;
            }

            public async ValueTask<ReadOnlySequence<byte>> CallAsync(string target,
                string method, ReadOnlySequence<byte> payload, string contentType,
                TimeSpan? timeout = null, CancellationToken ct = default)
            {
                var json = Encoding.UTF8.GetString(payload.ToArray());
                json = json.Replace("\"method\":", "\"METHOD\":",
                    StringComparison.Ordinal);
                json = Regex.Replace(json, "\"(length|acceptedSize)\":(\\d+)",
                    static match => $"\"{match.Groups[1].Value.ToUpperInvariant()}\":\"{match.Groups[2].Value}\"");
                return await _router.InvokeAsync(method,
                    new ReadOnlySequence<byte>(Encoding.UTF8.GetBytes(json)),
                    contentType, ct).ConfigureAwait(false);
            }

            private readonly MethodRouter _router;
        }
    }
}
