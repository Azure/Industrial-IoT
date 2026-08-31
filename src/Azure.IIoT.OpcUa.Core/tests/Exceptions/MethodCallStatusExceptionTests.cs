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

        [Fact]
        public void ConstructorsPopulateStatusMessageAndInnerException()
        {
            var inner = new InvalidOperationException("inner");

            var defaultStatus = new MethodCallStatusException("message");
            var withInner = new MethodCallStatusException("message", inner);
            var explicitStatus = new MethodCallStatusException(503, inner,
                "unavailable", "Unavailable", "urn:unavailable");

            Assert.Equal(500, defaultStatus.Status);
            Assert.Equal("message", defaultStatus.Details.Detail);
            Assert.Same(inner, withInner.InnerException);
            Assert.Equal(500, withInner.Status);
            Assert.Equal(503, explicitStatus.Status);
            Assert.Same(inner, explicitStatus.InnerException);
            Assert.Equal("Unavailable", explicitStatus.Details.Title);
            Assert.Equal("urn:unavailable", explicitStatus.Details.Type);
        }

        [Fact]
        public void DetailsConstructorUsesProblemDetailsAsMessage()
        {
            var details = new ErrorDetails
            {
                Status = 451,
                Detail = "legal",
                Title = "Unavailable"
            };

            var exception = new MethodCallStatusException(details);

            Assert.Same(details, exception.Details);
            Assert.Equal(451, exception.Status);
            Assert.Contains("\"status\":451", exception.Message);
            Assert.Equal(exception.Message, exception.ToString());
        }

        [Fact]
        public void DetailsConstructorPreservesInnerException()
        {
            var inner = new InvalidOperationException("inner");
            var details = new ErrorDetails { Status = 400, Detail = "bad" };

            var exception = new MethodCallStatusException(details, inner);

            Assert.Same(details, exception.Details);
            Assert.Same(inner, exception.InnerException);
        }

        [Fact]
        public void DeserializePlainTextPayloadUsesPayloadAsDetail()
        {
            var payload = Encoding.UTF8.GetBytes("plain error");

            var restored = MethodCallStatusException.Deserialize(payload, 502);

            Assert.Equal(502, restored.Status);
            Assert.Equal("plain error", restored.Details.Detail);
        }

        [Fact]
        public void ThrowInvalidJsonPayloadWrapsParserException()
        {
            var payload = Encoding.UTF8.GetBytes("{");

            var ex = Assert.Throws<MethodCallStatusException>(() =>
                MethodCallStatusException.Throw(payload, 500));

            Assert.Equal(500, ex.Status);
            Assert.NotNull(ex.InnerException);
            Assert.Equal(ex.InnerException.Message, ex.Details.Detail);
        }

        [Fact]
        public void ThrowPayloadStartingWithZeroUsesEmptyDetail()
        {
            var ex = Assert.Throws<MethodCallStatusException>(() =>
                MethodCallStatusException.Throw(new byte[] { 0 }, 503));

            Assert.Equal(503, ex.Status);
            Assert.Equal(string.Empty, ex.Details.Detail);
        }
    }
}
