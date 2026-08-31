// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Azure.IIoT.OpcUa.Core.Messaging.Clients
{
    using Azure.IIoT.OpcUa.Core.Messaging;
    using System;
    using System.Buffers;
    using System.Net;
    using System.Threading.Tasks;
    using Xunit;

    public sealed class NullEventClientTests
    {
        [Fact]
        public void PropertiesDescribeNullClient()
        {
            var client = new NullEventClient();

            Assert.Equal("NULL", client.Name);
            Assert.Equal(int.MaxValue, client.MaxEventPayloadSizeInBytes);
            Assert.Equal(Dns.GetHostName(), client.Identity);
            Assert.Equal(0, (int)client.Capabilities);
        }

        [Fact]
        public void CreateEventReturnsClientItself()
        {
            var client = new NullEventClient();

            var @event = client.CreateEvent();

            Assert.Same(client, @event);
        }

        [Fact]
        public async Task FluentEventMethodsAreNoOpsReturningSameInstanceAsync()
        {
            var client = new NullEventClient();
            var header = new CloudEventHeader
            {
                Id = "id",
                Source = new Uri("urn:test"),
                Type = "type"
            };
            var buffers = new[]
            {
                new ReadOnlySequence<byte>(new byte[] { 1, 2, 3 })
            };

            Assert.Same(client, client.SetTopic(null));
            Assert.Same(client, client.SetTimestamp(DateTimeOffset.UnixEpoch));
            Assert.Same(client, client.SetContentType(null));
            Assert.Same(client, client.SetContentEncoding(null));
            Assert.Same(client, client.AsCloudEvent(header));
            Assert.Same(client, client.SetSchema(new TestSchema()));
            Assert.Same(client, client.AddProperty("name", null));
            Assert.Same(client, client.SetRetain(true));
            Assert.Same(client, client.SetQoS(QoS.AtLeastOnce));
            Assert.Same(client, client.SetTtl(TimeSpan.FromMinutes(1)));
            Assert.Same(client, client.AddBuffers(buffers));

            await client.SendAsync(default);
            client.Dispose();
        }

        private sealed class TestSchema : IEventSchema
        {
            public string Type => "application/schema+json";
            public string Name => "schema";
            public ulong Version => 1;
            public string Schema => "{}";
            public string Id => "schema:1";
        }
    }
}
