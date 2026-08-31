// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

#nullable enable

namespace Azure.IIoT.OpcUa.Publisher.Tests.Discovery
{
    using Azure.IIoT.OpcUa.Core.Messaging;
    using Azure.IIoT.OpcUa.Encoders;
    using Azure.IIoT.OpcUa.Publisher.Discovery;
    using Azure.IIoT.OpcUa.Publisher.Models;
    using Microsoft.Extensions.Logging;
    using Microsoft.Extensions.Logging.Abstractions;
    using Microsoft.Extensions.Options;
    using System;
    using System.Buffers;
    using System.Collections.Generic;
    using System.Linq;
    using System.Text;
    using System.Threading;
    using System.Threading.Tasks;
    using Xunit;

    /// <summary>
    /// Covers the discovery progress publisher. Every test drives the publisher
    /// through the inherited progress API and then disposes it, because Dispose
    /// completes the channel and waits for the background sender to drain it -
    /// that is the only deterministic point at which delivery has happened, and
    /// polling for it would make these tests timing dependent.
    /// </summary>
    public sealed class ProgressPublisherTests
    {
        [Fact]
        public void SendsProgressThroughEveryConfiguredEventClient()
        {
            var mqtt = new RecordingEventClient("Mqtt", "mqtt-identity");
            var hub = new RecordingEventClient("IoTHub", "hub-identity");

            using (var sut = CreateSut([mqtt, hub]))
            {
                sut.OnDiscoveryStarted(CreateRequest());
            }

            var mqttEvent = Assert.Single(mqtt.Events);
            var hubEvent = Assert.Single(hub.Events);
            Assert.True(mqttEvent.WasSent);
            Assert.True(hubEvent.WasSent);
            Assert.Equal("application/json", mqttEvent.ContentType);
            Assert.Equal(Encoding.UTF8.WebName, mqttEvent.ContentEncoding);
            // The default events template is {RootTopic}/{EventSource}/{EventName},
            // and the publisher supplies "discovery" as the source.
            Assert.EndsWith("/discovery/" + DiscoveryProgressType.Started,
                mqttEvent.Topic, StringComparison.Ordinal);
            Assert.NotEmpty(mqttEvent.Buffers);
        }

        [Fact]
        public void StampsEachMessageWithTheIdentityOfTheClientThatSendsIt()
        {
            var mqtt = new RecordingEventClient("Mqtt", "mqtt-identity");
            var hub = new RecordingEventClient("IoTHub", "hub-identity");

            using (var sut = CreateSut([mqtt, hub]))
            {
                sut.OnDiscoveryStarted(CreateRequest());
            }

            // The DiscovererId is per client, so the same progress record must
            // not be sent with one shared identity.
            Assert.Contains("mqtt-identity", PayloadOf(mqtt));
            Assert.DoesNotContain("hub-identity", PayloadOf(mqtt));
            Assert.Contains("hub-identity", PayloadOf(hub));
            Assert.DoesNotContain("mqtt-identity", PayloadOf(hub));
        }

        [Fact]
        public void RestrictsSendingToTheAllowedTransports()
        {
            var mqtt = new RecordingEventClient("Mqtt", "mqtt-identity");
            var hub = new RecordingEventClient("IoTHub", "hub-identity");
            var options = CreateOptions();
            options.Value.AllowedEventAndDiagnosticsTransports.Add(
                WriterGroupTransport.Mqtt);

            using (var sut = CreateSut([mqtt, hub], options))
            {
                sut.OnDiscoveryStarted(CreateRequest());
            }

            Assert.Single(mqtt.Events);
            Assert.Empty(hub.Events);
        }

        [Fact]
        public void MatchesAllowedTransportNamesWithoutRegardToCase()
        {
            var mqtt = new RecordingEventClient("mqtt", "mqtt-identity");
            var options = CreateOptions();
            options.Value.AllowedEventAndDiagnosticsTransports.Add(
                WriterGroupTransport.Mqtt);

            using (var sut = CreateSut([mqtt], options))
            {
                sut.OnDiscoveryStarted(CreateRequest());
            }

            Assert.Single(mqtt.Events);
        }

        [Fact]
        public void SendsThroughEveryClientWhenNoTransportsAreRestricted()
        {
            var mqtt = new RecordingEventClient("Mqtt", "mqtt-identity");
            var hub = new RecordingEventClient("IoTHub", "hub-identity");

            using (var sut = CreateSut([mqtt, hub], CreateOptions()))
            {
                sut.OnDiscoveryStarted(CreateRequest());
            }

            Assert.Single(mqtt.Events);
            Assert.Single(hub.Events);
        }

        [Fact]
        public void TagsMessagesWithTheDiscoverySchemaWhenCloudEventsAreOff()
        {
            var mqtt = new RecordingEventClient("Mqtt", "mqtt-identity");
            var options = CreateOptions();
            options.Value.EnableCloudEvents = false;

            using (var sut = CreateSut([mqtt], options))
            {
                sut.OnDiscoveryStarted(CreateRequest());
            }

            var sent = Assert.Single(mqtt.Events);
            Assert.False(sent.IsCloudEvent);
            var property = Assert.Single(sent.Properties);
            Assert.Equal(MessageSchemaTypes.DiscoveryMessage, property.Value);
        }

        [Fact]
        public void WrapsMessagesAsCloudEventsWhenEnabled()
        {
            var mqtt = new RecordingEventClient("Mqtt", "mqtt-identity");
            var options = CreateOptions();
            options.Value.EnableCloudEvents = true;

            using (var sut = CreateSut([mqtt], options))
            {
                sut.OnDiscoveryStarted(CreateRequest());
            }

            var sent = Assert.Single(mqtt.Events);
            Assert.True(sent.IsCloudEvent);
            // The schema property is the non cloud event alternative, so it
            // must not also be present.
            Assert.Empty(sent.Properties);
            var header = sent.CloudEventHeader!;
            Assert.Equal(MessageSchemaTypes.DiscoveryMessage, header.Type);
            Assert.Equal(DiscoveryProgressType.Started.ToString(), header.Subject);
            Assert.Equal(new Uri("urn:publisher1"), header.Source);
            Assert.NotEmpty(header.Id!);
        }

        [Fact]
        public void KeepsPublishingAfterOneClientFails()
        {
            var failing = new RecordingEventClient("Mqtt", "mqtt-identity")
            {
                SendException = new InvalidOperationException("broker gone")
            };
            var healthy = new RecordingEventClient("IoTHub", "hub-identity");
            var logger = new CapturingLogger();

            using (var sut = CreateSut([failing, healthy], logger: logger))
            {
                sut.OnDiscoveryStarted(CreateRequest());
                sut.OnDiscoveryFinished(CreateRequest());
            }

            // A failing transport must not stop the others, and must not stop
            // the sender loop for subsequent progress either.
            Assert.Equal(2, healthy.Events.Count);
            Assert.All(healthy.Events, e => Assert.True(e.WasSent));
            Assert.Equal(2, logger.Errors.Count(id => id == 62));
        }

        [Fact]
        public void ReusesOneTopicForRepeatedProgressOfTheSameKind()
        {
            var mqtt = new RecordingEventClient("Mqtt", "mqtt-identity");
            var request = CreateRequest();

            using (var sut = CreateSut([mqtt]))
            {
                sut.OnDiscoveryStarted(request);
                sut.OnDiscoveryStarted(request);
            }

            Assert.Equal(2, mqtt.Events.Count);
            Assert.Equal(mqtt.Events[0].Topic, mqtt.Events[1].Topic);
        }

        [Fact]
        public void BuildsADifferentTopicForADifferentProgressKind()
        {
            var mqtt = new RecordingEventClient("Mqtt", "mqtt-identity");
            var request = CreateRequest();

            using (var sut = CreateSut([mqtt]))
            {
                sut.OnDiscoveryStarted(request);
                sut.OnDiscoveryFinished(request);
            }

            Assert.Equal(2, mqtt.Events.Count);
            Assert.NotEqual(mqtt.Events[0].Topic, mqtt.Events[1].Topic);
        }

        [Fact]
        public void RefusesToAcceptProgressOnceDisposed()
        {
            var mqtt = new RecordingEventClient("Mqtt", "mqtt-identity");
            var logger = new CapturingLogger();
            var sut = CreateSut([mqtt], logger: logger);

            sut.Dispose();

            Assert.Throws<ObjectDisposedException>(
                () => sut.OnDiscoveryStarted(CreateRequest()));
            Assert.Contains(61, logger.Errors);
        }

        [Fact]
        public void CanBeDisposedRepeatedly()
        {
            var mqtt = new RecordingEventClient("Mqtt", "mqtt-identity");
            var sut = CreateSut([mqtt]);

            sut.Dispose();

            Assert.Null(Record.Exception(sut.Dispose));
        }

        [Fact]
        public void DeliversEverythingQueuedBeforeDisposeReturns()
        {
            var mqtt = new RecordingEventClient("Mqtt", "mqtt-identity");
            var request = CreateRequest();

            var sut = CreateSut([mqtt]);
            for (var i = 0; i < 25; i++)
            {
                sut.OnDiscoveryStarted(request);
            }
            sut.Dispose();

            // Dispose drains the channel, so nothing may be lost on shutdown.
            Assert.Equal(25, mqtt.Events.Count);
        }

        private static string PayloadOf(RecordingEventClient client)
        {
            var buffers = Assert.Single(client.Events).Buffers;
            return string.Concat(buffers.Select(
                b => Encoding.UTF8.GetString(b.ToArray())));
        }

        private static ProgressPublisher CreateSut(IEnumerable<IEventClient> clients,
            IOptions<PublisherOptions>? options = null, ILogger? logger = null)
        {
            return new ProgressPublisher(clients, options ?? CreateOptions(),
                new TypedLogger(logger ?? NullLogger.Instance));
        }

        private static IOptions<PublisherOptions> CreateOptions()
        {
            var options = new PublisherOptions
            {
                PublisherId = "publisher1",
                SiteId = "site1"
            };
            // A bare PublisherOptions has no topic templates - the defaults are
            // applied by PublisherConfig.Configure, not by the record itself -
            // so without these every topic formats to the empty string and the
            // topic assertions below would pass vacuously.
            options.TopicTemplates.Root = PublisherConfig.RootTopicTemplateDefault;
            options.TopicTemplates.Events = PublisherConfig.EventsTopicTemplateDefault;
            return Options.Create(options);
        }

        private static DiscoveryRequestModel CreateRequest()
        {
            return new DiscoveryRequestModel
            {
                Id = "request1",
                Discovery = DiscoveryMode.Fast
            };
        }

        private sealed class RecordingEventClient : IEventClient
        {
            public RecordingEventClient(string name, string identity)
            {
                Name = name;
                Identity = identity;
            }

            public string Name { get; }

            public string Identity { get; }

            public int MaxEventPayloadSizeInBytes => 256 * 1024;

            public Exception? SendException { get; init; }

            public List<RecordingEvent> Events { get; } = [];

            public IEvent CreateEvent()
            {
                var e = new RecordingEvent(SendException);
                lock (Events)
                {
                    Events.Add(e);
                }
                return e;
            }
        }

        private sealed class RecordingEvent : IEvent
        {
            public RecordingEvent(Exception? sendException)
            {
                _sendException = sendException;
            }

            public string? Topic { get; private set; }

            public string? ContentType { get; private set; }

            public string? ContentEncoding { get; private set; }

            public bool IsCloudEvent { get; private set; }

            public CloudEventHeader? CloudEventHeader { get; private set; }

            public bool WasSent { get; private set; }

            public List<ReadOnlySequence<byte>> Buffers { get; } = [];

            public List<(string Name, string? Value)> Properties { get; } = [];

            public IEvent SetTopic(string? value)
            {
                Topic = value;
                return this;
            }

            public IEvent SetTimestamp(DateTimeOffset value)
            {
                return this;
            }

            public IEvent SetContentType(string? value)
            {
                ContentType = value;
                return this;
            }

            public IEvent SetContentEncoding(string? value)
            {
                ContentEncoding = value;
                return this;
            }

            public IEvent AsCloudEvent(CloudEventHeader header)
            {
                IsCloudEvent = true;
                CloudEventHeader = header;
                return this;
            }

            public IEvent SetSchema(IEventSchema schema)
            {
                return this;
            }

            public IEvent AddProperty(string name, string? value)
            {
                Properties.Add((name, value));
                return this;
            }

            public IEvent SetRetain(bool value)
            {
                return this;
            }

            public IEvent SetQoS(QoS value)
            {
                return this;
            }

            public IEvent SetTtl(TimeSpan value)
            {
                return this;
            }

            public IEvent AddBuffers(IEnumerable<ReadOnlySequence<byte>> value)
            {
                Buffers.AddRange(value);
                return this;
            }

            public ValueTask SendAsync(CancellationToken ct = default)
            {
                if (_sendException != null)
                {
                    return ValueTask.FromException(_sendException);
                }
                WasSent = true;
                return ValueTask.CompletedTask;
            }

            public void Dispose()
            {
            }

            private readonly Exception? _sendException;
        }

        private sealed class TypedLogger : ILogger<ProgressPublisher>
        {
            public TypedLogger(ILogger inner)
            {
                _inner = inner;
            }

            public IDisposable? BeginScope<TState>(TState state)
                where TState : notnull
            {
                return _inner.BeginScope(state);
            }

            public bool IsEnabled(LogLevel logLevel)
            {
                return _inner.IsEnabled(logLevel);
            }

            public void Log<TState>(LogLevel logLevel, EventId eventId, TState state,
                Exception? exception, Func<TState, Exception?, string> formatter)
            {
                _inner.Log(logLevel, eventId, state, exception, formatter);
            }

            private readonly ILogger _inner;
        }

        private sealed class CapturingLogger : ILogger
        {
            public List<int> Errors { get; } = [];

            public IDisposable? BeginScope<TState>(TState state)
                where TState : notnull
            {
                return null;
            }

            public bool IsEnabled(LogLevel logLevel)
            {
                return true;
            }

            public void Log<TState>(LogLevel logLevel, EventId eventId, TState state,
                Exception? exception, Func<TState, Exception?, string> formatter)
            {
                if (logLevel == LogLevel.Error)
                {
                    lock (Errors)
                    {
                        Errors.Add(eventId.Id);
                    }
                }
            }
        }
    }
}
