// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

#nullable enable

namespace Azure.IIoT.OpcUa.Core.Messaging.Clients.Mqtt
{
    using System;
    using System.Buffers;
    using System.Collections.Generic;
    using System.Globalization;
    using System.Linq;
    using System.Threading;
    using System.Threading.Tasks;
    using Xunit;

    public sealed class MqttEventTests
    {
        [Fact]
        public async Task MqttV5PreservesEventWireContractAsync()
        {
            var publisher = new CapturingPublisher();
            var schema = new TestEventSchema();
            using var cancellation = new CancellationTokenSource();
            using var @event = new MqttEvent(MqttVersion.v5, QoS.AtMostOnce, publisher);
            var time = new DateTimeOffset(2026, 7, 16, 8, 9, 10, 123,
                TimeSpan.FromHours(5.5));
            var header = new CloudEventHeader
            {
                Id = "event-id",
                Source = new Uri("urn:test"),
                Type = "test.event",
                Subject = "subject",
                Time = time,
                DataContentType = "application/json"
            };

            await @event
                .SetTopic("telemetry/topic")
                .SetQoS(QoS.ExactlyOnce)
                .SetRetain(true)
                .SetTtl(TimeSpan.FromSeconds(42))
                .SetContentType("application/json")
                .SetContentEncoding("gzip")
                .AddProperty("tenant", "factory-a")
                .AsCloudEvent(header)
                .SetSchema(schema)
                .AddBuffers([new ReadOnlySequence<byte>(new byte[] { 1, 2, 3 })])
                .SendAsync(cancellation.Token);

            var published = Assert.Single(publisher.Messages);
            Assert.Equal("telemetry/topic", published.Message.Topic);
            Assert.Equal(new byte[] { 1, 2, 3 }, published.Message.Payload.ToArray());
            Assert.Equal(QoS.ExactlyOnce, published.Message.QoS);
            Assert.True(published.Message.Retain);
            Assert.Equal((uint)42, published.Message.MessageExpiryIntervalSeconds);
            Assert.Equal("application/json", published.Message.ContentType);
            Assert.Same(schema, published.Schema);
            Assert.Equal(cancellation.Token, published.CancellationToken);

            var properties = Assert.IsType<List<KeyValuePair<string, string>>>(
                published.Message.UserProperties).ToLookup(property => property.Key,
                    property => property.Value);
            Assert.Equal("gzip", Assert.Single(properties["ContentEncoding"]));
            Assert.Equal("factory-a", Assert.Single(properties["tenant"]));
            Assert.Equal("1.0", Assert.Single(properties["specversion"]));
            Assert.Equal("event-id", Assert.Single(properties["id"]));
            Assert.Equal("urn:test", Assert.Single(properties["source"]));
            Assert.Equal("test.event", Assert.Single(properties["type"]));
            Assert.Equal("subject", Assert.Single(properties["subject"]));
            var timeProperty = Assert.Single(properties["time"]);
            Assert.Equal(time.ToString("O", CultureInfo.InvariantCulture), timeProperty);
            Assert.Equal(time, DateTimeOffset.Parse(timeProperty,
                CultureInfo.InvariantCulture, DateTimeStyles.RoundtripKind));
            Assert.Equal("application/json",
                Assert.Single(properties["datacontenttype"]));
        }

        [Fact]
        public async Task MqttV311DropsV5OnlyMetadataAsync()
        {
            var publisher = new CapturingPublisher();
            using var @event = new MqttEvent(MqttVersion.v311, QoS.AtMostOnce, publisher);

            await @event
                .SetTopic("telemetry/topic")
                .SetQoS(QoS.ExactlyOnce)
                .SetRetain(true)
                .SetTtl(TimeSpan.FromSeconds(42))
                .SetContentType("application/json")
                .SetContentEncoding("gzip")
                .AddProperty("tenant", "factory-a")
                .AsCloudEvent(new CloudEventHeader
                {
                    Id = "event-id",
                    Source = new Uri("urn:test"),
                    Type = "test.event"
                })
                .AddBuffers([new ReadOnlySequence<byte>(new byte[] { 1, 2, 3 })])
                .SendAsync();

            var published = Assert.Single(publisher.Messages).Message;
            Assert.Equal("telemetry/topic", published.Topic);
            Assert.Equal(new byte[] { 1, 2, 3 }, published.Payload.ToArray());
            Assert.Equal(QoS.ExactlyOnce, published.QoS);
            Assert.True(published.Retain);
            Assert.Null(published.MessageExpiryIntervalSeconds);
            Assert.Null(published.ContentType);
            Assert.Null(published.UserProperties);
        }

        [Theory]
        [InlineData(MqttVersion.v5)]
        [InlineData(MqttVersion.v311)]
        public async Task EmptyRetainedMqttEventPublishesTombstoneAsync(
            MqttVersion version)
        {
            var publisher = new CapturingPublisher();
            using var @event = new MqttEvent(version, QoS.AtLeastOnce, publisher);

            await @event.SetTopic("metadata/topic").SetRetain(true).SendAsync();

            var published = Assert.Single(publisher.Messages).Message;
            Assert.Equal("metadata/topic", published.Topic);
            Assert.True(published.Retain);
            Assert.True(published.Payload.IsEmpty);
        }

        private sealed class CapturingPublisher : IMqttPublisher
        {
            public List<PublishedMessage> Messages { get; } = [];

            public ValueTask PublishAsync(MqttPublishMessage message, IEventSchema? schema,
                CancellationToken ct)
            {
                Messages.Add(new PublishedMessage(message, schema, ct));
                return ValueTask.CompletedTask;
            }
        }

        private sealed record class PublishedMessage(MqttPublishMessage Message,
            IEventSchema? Schema, CancellationToken CancellationToken);

        private sealed class TestEventSchema : IEventSchema
        {
            public string Type => "application/schema+json";
            public string Name => "test";
            public ulong Version => 1;
            public string Schema => "{}";
            public string Id => "urn:test:schema";
        }
    }
}
