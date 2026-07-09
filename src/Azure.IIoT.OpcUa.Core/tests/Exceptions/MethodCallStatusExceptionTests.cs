// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Azure.IIoT.OpcUa.Core.Exceptions
{
    using FluentAssertions;
    using System;
    using System.Text;
    using System.Text.Json;
    using Xunit;

    /// <summary>
    /// Verifies the RFC 7807 wire shape and round-trip behavior of the
    /// source-generated (AOT/trim safe) method-call error envelope.
    /// </summary>
    public sealed class MethodCallStatusExceptionTests
    {
        [Fact]
        public void ErrorDetailsSerializesWithRfc7807PropertyNamesAndOrder()
        {
            var ex = new MethodCallStatusException(404, "not there", "Not Found");

            var json = Encoding.UTF8.GetString(ex.Serialize().Span);

            // Property order: type(-5), title(-4), status(-3), detail(-2).
            json.Should().Be(
                "{\"title\":\"Not Found\",\"status\":404,\"detail\":\"not there\"}");
        }

        [Fact]
        public void NullValuedMembersAreOmitted()
        {
            var ex = new MethodCallStatusException(500, (string?)null);

            var json = Encoding.UTF8.GetString(ex.Serialize().Span);

            json.Should().Be("{\"status\":500}");
        }

        [Fact]
        public void DeserializeRoundTripsStatusAndDetail()
        {
            var original = new MethodCallStatusException(409, "conflict", "Conflict",
                "https://example/conflict");

            var restored = MethodCallStatusException.Deserialize(original.Serialize());

            restored.Status.Should().Be(409);
            restored.Details.Detail.Should().Be("conflict");
            restored.Details.Title.Should().Be("Conflict");
            restored.Details.Type.Should().Be("https://example/conflict");
        }

        [Fact]
        public void DeserializeAppliesOuterStatusWhenPayloadHasNone()
        {
            var payload = Encoding.UTF8.GetBytes("{\"detail\":\"boom\"}");

            var restored = MethodCallStatusException.Deserialize(payload, outerStatus: 502);

            restored.Status.Should().Be(502);
            restored.Details.Detail.Should().Be("boom");
        }

        [Fact]
        public void DeserializeEmptyPayloadYieldsOuterStatus()
        {
            var restored = MethodCallStatusException.Deserialize(
                ReadOnlyMemory<byte>.Empty, outerStatus: 500);

            restored.Status.Should().Be(500);
        }

        [Fact]
        public void ThrowRaisesTheDeserializedException()
        {
            var payload = new MethodCallStatusException(418, "teapot", "I'm a teapot")
                .Serialize();

            var act = () => MethodCallStatusException.Throw(payload);

            act.Should().Throw<MethodCallStatusException>()
                .Which.Status.Should().Be(418);
        }

        [Fact]
        public void UnknownMembersRoundTripThroughExtensionData()
        {
            var payload = Encoding.UTF8.GetBytes(
                "{\"status\":400,\"detail\":\"bad\",\"traceId\":\"abc123\"}");

            var restored = MethodCallStatusException.Deserialize(payload);

            restored.Details.Extensions.Should().ContainKey("traceId");
            restored.Details.Extensions["traceId"].GetString().Should().Be("abc123");
        }
    }
}
