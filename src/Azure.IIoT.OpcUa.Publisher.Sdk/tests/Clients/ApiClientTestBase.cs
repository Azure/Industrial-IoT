// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Azure.IIoT.OpcUa.Publisher.Sdk.Tests.Clients
{
    using Azure.IIoT.OpcUa.Core;
    using Azure.IIoT.OpcUa.Core.Rpc;
    using Azure.IIoT.OpcUa.Core.Serialization;
    using Moq;
    using System;
    using System.Text.Json;
    using System.Threading;
    using System.Threading.Tasks;
    using Xunit;

    /// <summary>
    /// Harness for the SDK's API clients.
    /// </summary>
    /// <remarks>
    /// Every client method is the same shape: validate the arguments, serialize
    /// a payload, call one named method on the RPC transport, and deserialize
    /// what comes back. So the transport is recorded rather than mocked per
    /// test, and a test says which method should have been called and what the
    /// payload should have contained.
    /// <para>
    /// The payload is asserted as JSON rather than against a model type on
    /// purpose. Most of these payloads are anonymous objects whose property
    /// names <em>are</em> the wire contract, and a model-typed assertion would
    /// keep passing if a name changed on both sides at once.
    /// </para>
    /// </remarks>
    public abstract class ApiClientTestBase
    {
        protected const string Target = "publisher_module";

        protected ApiClientTestBase()
        {
            MethodClient = new Mock<IMethodClient>();
            MethodClient
                .Setup(c => c.CallMethodAsync(
                    It.IsAny<string>(), It.IsAny<string>(),
                    It.IsAny<ReadOnlyMemory<byte>>(), It.IsAny<string>(),
                    It.IsAny<TimeSpan?>(), It.IsAny<CancellationToken>()))
                .Returns((string target, string method, ReadOnlyMemory<byte> payload,
                    string contentType, TimeSpan? timeout, CancellationToken ct) =>
                {
                    LastCall = new RecordedCall(target, method, payload, contentType, timeout, ct);
                    return ValueTask.FromResult(_response);
                });
        }

        protected Mock<IMethodClient> MethodClient { get; }

        /// <summary>
        /// The call the client made, or null if it never reached the transport.
        /// </summary>
        protected RecordedCall? LastCall { get; private set; }

        /// <summary>
        /// Sets what the transport returns for the next call.
        /// </summary>
        protected void Returns<T>(T value)
        {
            _response = Json.SerializeToMemory(value);
        }

        /// <summary>
        /// Sets a raw transport response, for the malformed-response cases.
        /// </summary>
        protected void ReturnsRaw(string json)
        {
            _response = System.Text.Encoding.UTF8.GetBytes(json);
        }

        /// <summary>
        /// Asserts the transport was called once, for the named method, against
        /// the configured target and as JSON, and returns the payload parsed as
        /// JSON so the test can assert the wire property names.
        /// </summary>
        protected JsonElement AssertCalled(string method)
        {
            var call = LastCall;
            Assert.NotNull(call);
            Assert.Equal(Target, call!.Target);
            Assert.Equal(method, call.Method);
            Assert.Equal(ContentMimeType.Json, call.ContentType);
            MethodClient.Verify(c => c.CallMethodAsync(
                It.IsAny<string>(), It.IsAny<string>(),
                It.IsAny<ReadOnlyMemory<byte>>(), It.IsAny<string>(),
                It.IsAny<TimeSpan?>(), It.IsAny<CancellationToken>()), Times.Once);
            return JsonDocument.Parse(call.Payload).RootElement.Clone();
        }

        /// <summary>
        /// Asserts the transport was never reached, which is what argument
        /// validation is for - a rejected call must not travel.
        /// </summary>
        protected void AssertNotCalled()
        {
            Assert.Null(LastCall);
        }

        protected static string? StringOf(JsonElement payload, string property)
        {
            return payload.GetProperty(property).GetString();
        }

        private ReadOnlyMemory<byte> _response = Json.SerializeToMemory<object?>(null);

        protected sealed record RecordedCall(string Target, string Method,
            ReadOnlyMemory<byte> Payload, string ContentType, TimeSpan? Timeout,
            CancellationToken Ct);
    }
}
