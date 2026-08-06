// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

#nullable enable

namespace Azure.IIoT.OpcUa.Core.Messaging.Clients.Dapr
{
    using Azure.IIoT.OpcUa.Core.Messaging;
    using Microsoft.Extensions.Options;
    using System;
    using System.Buffers;
    using System.Threading.Tasks;
    using Xunit;

    public sealed class DaprPubSubClientTests
    {
        [Fact]
        public void ConstructorRequiresOptions()
        {
            Assert.Throws<ArgumentNullException>(() => new DaprPubSubClient(null!));
        }

        [Fact]
        public void ConstructorUsesConfiguredMaximumPayloadSize()
        {
            using var client = new DaprPubSubClient(Options.Create(new DaprOptions
            {
                MessageMaxBytes = 1234
            }));

            Assert.Equal(1234, client.MaxEventPayloadSizeInBytes);
        }

        [Fact]
        public void ConstructorUsesDefaultMaximumPayloadSize()
        {
            using var client = new DaprPubSubClient(Options.Create(new DaprOptions()));

            Assert.Equal(512 * 1024 * 1024, client.MaxEventPayloadSizeInBytes);
        }

        [Fact]
        public async Task EmptyEventDoesNotRequireTopicAsync()
        {
            using var client = new DaprPubSubClient(Options.Create(new DaprOptions()));
            using var @event = client.CreateEvent();

            await @event.SendAsync();
        }

        [Fact]
        public async Task SendRequiresTopicWhenPayloadIsPresentAsync()
        {
            using var client = new DaprPubSubClient(Options.Create(new DaprOptions()));
            using var @event = client.CreateEvent();

            await Assert.ThrowsAsync<InvalidOperationException>(async () =>
                await @event.AddBuffers(
                    [new ReadOnlySequence<byte>(new byte[] { 1 })])
                    .SendAsync().ConfigureAwait(false));
        }

        [Theory]
        [InlineData("topic")]
        [InlineData("/topic")]
        public async Task SendRequiresComponentInTopicWhenComponentIsNotConfiguredAsync(
            string topic)
        {
            using var client = new DaprPubSubClient(Options.Create(new DaprOptions()));
            using var @event = client.CreateEvent();

            await Assert.ThrowsAsync<InvalidOperationException>(async () =>
                await @event
                    .SetTopic(topic)
                    .AddBuffers([new ReadOnlySequence<byte>(new byte[] { 1 })])
                    .SendAsync().ConfigureAwait(false));
        }

        [Fact]
        public void Identity_ReturnsValidGuidString()
        {
            using var client = new DaprPubSubClient(Options.Create(new DaprOptions()));
            Assert.True(Guid.TryParse(client.Identity, out _));
        }

        [Fact]
        public void Capabilities_ContainsExpectedFlags()
        {
            using var client = new DaprPubSubClient(Options.Create(new DaprOptions()));
            Assert.True(client.Capabilities.HasFlag(EventClientCapabilities.Payload));
            Assert.True(client.Capabilities.HasFlag(EventClientCapabilities.Topic));
        }

        [Fact]
        public void EventBuilderMethods_ReturnThis_ForFluentChaining()
        {
            using var client = new DaprPubSubClient(Options.Create(new DaprOptions()));
            using var evt = client.CreateEvent();

            var fluent = evt
                .SetTopic("t/v")
                .SetContentType("application/json")
                .SetContentEncoding("utf-8")
                .SetQoS(QoS.AtLeastOnce)
                .SetTimestamp(DateTimeOffset.UtcNow)
                .SetRetain(false)
                .SetTtl(TimeSpan.FromSeconds(30))
                .AddProperty("key", "value")
                .AddProperty("key", null)     // removes the entry
                .SetSchema(new StubSchema());

            Assert.Same(evt, fluent);
        }

        private sealed class StubSchema : IEventSchema
        {
            public string Type => "application/json";
            public string Name => "test";
            public ulong Version => 1;
            public string Schema => "{}";
            public string? Id => "schema:1";
        }
    }
}
