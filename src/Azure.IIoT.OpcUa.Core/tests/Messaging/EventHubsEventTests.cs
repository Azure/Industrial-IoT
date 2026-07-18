// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

#nullable enable

namespace Azure.IIoT.OpcUa.Core.Messaging.Clients.EventHubs
{
    using System;
    using System.Buffers;
    using System.Globalization;
    using System.Runtime.CompilerServices;
    using Xunit;

    public sealed class EventHubsEventTests
    {
        [Fact]
        public void EventMapsContentTypeAndEncodingToAmqpProperties()
        {
            var client = (EventHubsClient)RuntimeHelpers.GetUninitializedObject(
                typeof(EventHubsClient));
            using var @event = Assert.IsType<EventHubsClient.EventHubsEvent>(
                client.CreateEvent());

            @event
                .SetContentType("application/octet-stream")
                .SetContentEncoding("gzip");

            var message = @event.CreateMessage(
                new ReadOnlySequence<byte>(new byte[] { 1 }));

            Assert.Equal("application/octet-stream", message.ContentType);
            Assert.Equal("gzip",
                message.GetRawAmqpMessage().Properties.ContentEncoding);
        }

        [Fact]
        public void EventMapsContentAndCloudEventsToAmqp()
        {
            var client = (EventHubsClient)RuntimeHelpers.GetUninitializedObject(
                typeof(EventHubsClient));
            using var @event = Assert.IsType<EventHubsClient.EventHubsEvent>(
                client.CreateEvent());
            var time = new DateTimeOffset(2026, 7, 16, 8, 9, 10, 123,
                TimeSpan.FromHours(-4));

            @event
                .SetTopic("factory-a")
                .SetContentType("application/octet-stream")
                .SetContentEncoding("gzip")
                .AddProperty("tenant", "north")
                .AsCloudEvent(new CloudEventHeader
                {
                    Id = "event-id",
                    Source = new Uri("urn:test"),
                    Type = "test.event",
                    Subject = "subject",
                    Time = time,
                    DataContentType = "application/json"
                });

            var message = @event.CreateMessage(
                new ReadOnlySequence<byte>(new byte[] { 1, 2, 3 }));

            Assert.Equal(new byte[] { 1, 2, 3 }, message.EventBody.ToArray());
            Assert.Equal("application/json", message.ContentType);
            Assert.Equal("gzip",
                message.GetRawAmqpMessage().Properties.ContentEncoding);
            Assert.Equal("factory-a", message.Properties["deviceId"]);
            Assert.Equal("north", message.Properties["tenant"]);
            Assert.Equal("1.0", message.Properties["cloudEvents:specversion"]);
            Assert.Equal("event-id", message.Properties["cloudEvents:id"]);
            Assert.Equal("urn:test", message.Properties["cloudEvents:source"]);
            Assert.Equal("test.event", message.Properties["cloudEvents:type"]);
            Assert.Equal("subject", message.Properties["cloudEvents:subject"]);
            Assert.Equal(time.ToString("O", CultureInfo.InvariantCulture),
                message.Properties["cloudEvents:time"]);
            Assert.DoesNotContain("cloudEvents:datacontenttype",
                message.Properties.Keys);
            Assert.DoesNotContain("datacontenttype", message.Properties.Keys);
        }
    }
}
