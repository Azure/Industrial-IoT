// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Azure.IIoT.OpcUa.Publisher.Tests.PubSub
{
    using Azure.IIoT.OpcUa.Core.Messaging;
    using Azure.IIoT.OpcUa.Publisher.Models;
    using Azure.IIoT.OpcUa.Publisher.PubSub;
    using Microsoft.Extensions.DependencyInjection;
    using Microsoft.Extensions.Hosting;
    using Microsoft.Extensions.Logging;
    using Opc.Ua;
    using Opc.Ua.PubSub.Diagnostics;
    using Opc.Ua.PubSub.Transports;
    using System;
    using System.Buffers;
    using System.Collections.Generic;
    using System.Collections.ObjectModel;
    using System.Linq;
    using System.Text;
    using System.Threading;
    using System.Threading.Tasks;
    using Xunit;

    public sealed class EventClientPubSubTransportTests
    {
        [Fact]
        public async Task EgressPreservesEventClientHeadersAndChunksAsync()
        {
            var client = new RecordingEventClient { MaxPayload = 2 };
            await using var transport = CreateTransport(client, new PubSubShadowEgressSettings
            {
                ConnectionName = "shadow-group",
                Topic = "configured/topic",
                ContentType = "application/json",
                ContentEncoding = "gzip",
                QualityOfService = QoS.AtLeastOnce,
                Retain = true,
                TimeToLive = TimeSpan.FromMinutes(1),
                UseCloudEvents = true,
                CloudEventSource = new Uri("urn:test:publisher"),
                CloudEventType = "test.pubsub",
                CloudEventSubject = "group",
                Schema = new PubSubShadowEventSchema("group", PubSubShadowEncoding.Json),
                Properties = new ReadOnlyDictionary<string, string?>(
                    new Dictionary<string, string?> { ["custom"] = "property" })
            });
            await transport.OpenAsync();

            await transport.SendAsync(new byte[] { 1, 2, 3, 4, 5 }, "writer/topic");
            await transport.CloseAsync();

            var events = client.Events;
            Assert.Equal(3, events.Count);
            Assert.All(events, captured =>
            {
                Assert.Equal("writer/topic", captured.Topic);
                Assert.Equal(QoS.AtLeastOnce, captured.QualityOfService);
                Assert.True(captured.Retain);
                Assert.Equal(TimeSpan.FromMinutes(1), captured.TimeToLive);
                Assert.Equal("application/json", captured.ContentType);
                Assert.Equal("gzip", captured.ContentEncoding);
                Assert.Equal("property", captured.Properties["custom"]);
                Assert.NotNull(captured.CloudEvent);
                Assert.NotNull(captured.Schema);
            });
            Assert.Equal(new byte[] { 1, 2 }, events[0].Payload);
            Assert.Equal(new byte[] { 3, 4 }, events[1].Payload);
            Assert.Equal(new byte[] { 5 }, events[2].Payload);
            Assert.Equal(3, transport.Metrics.ChunkCount);
        }

        [Fact]
        public async Task EgressRetriesTransientFailuresWithoutReorderingAsync()
        {
            var client = new RecordingEventClient { FailuresRemaining = 2 };
            await using var transport = CreateTransport(client);
            await transport.OpenAsync();

            var first = transport.SendAsync(new byte[] { 1 }, "topic").AsTask();
            var second = transport.SendAsync(new byte[] { 2 }, "topic").AsTask();
            await Task.WhenAll(first, second);
            await transport.CloseAsync();

            Assert.Equal(new byte[] { 1 }, client.Events[0].Payload);
            Assert.Equal(new byte[] { 2 }, client.Events[1].Payload);
            Assert.Equal(2, transport.Metrics.RetryCount);
            Assert.Equal(2, transport.Metrics.SentCount);
        }

        [Fact]
        public async Task EgressCancelsAContinuouslyFailingSendAsync()
        {
            var client = new RecordingEventClient { FailuresRemaining = int.MaxValue };
            await using var transport = CreateTransport(client);
            await transport.OpenAsync();
            using var cts = new CancellationTokenSource();

            var sending = transport.SendAsync(new byte[] { 1 }, "topic", cts.Token).AsTask();
            await client.SendStarted.Task.WaitAsync(TimeSpan.FromSeconds(5));
            cts.Cancel();

            await Assert.ThrowsAnyAsync<OperationCanceledException>(() => sending);
            await transport.CloseAsync();
            Assert.NotEqual(0, transport.Metrics.RetryCount);
        }

        [Fact]
        public async Task EgressAppliesBoundedQueueBackpressureAndRejectPolicyAsync()
        {
            var client = new RecordingEventClient();
            client.BlockSuccessfulSends();
            await using var waitTransport = CreateTransport(client, options =>
            {
                options.QueueCapacity = 1;
            });
            await waitTransport.OpenAsync();

            var first = waitTransport.SendAsync(new byte[] { 1 }, "topic").AsTask();
            await client.SendStarted.Task.WaitAsync(TimeSpan.FromSeconds(5));
            var second = waitTransport.SendAsync(new byte[] { 2 }, "topic").AsTask();
            var third = waitTransport.SendAsync(new byte[] { 3 }, "topic").AsTask();
            await WaitUntilAsync(() => waitTransport.Metrics.BackpressureCount == 1);
            client.ReleaseSuccessfulSends();
            await Task.WhenAll(first, second, third);
            await waitTransport.CloseAsync();

            Assert.Equal(new byte[] { 1, 2, 3 },
                client.Events.SelectMany(captured => captured.Payload).ToArray());

            var rejectingClient = new RecordingEventClient();
            rejectingClient.BlockSuccessfulSends();
            await using var rejectTransport = CreateTransport(rejectingClient, options =>
            {
                options.QueueCapacity = 1;
                options.OverflowPolicy = PubSubShadowEgressOverflowPolicy.Reject;
            });
            await rejectTransport.OpenAsync();
            var blocked = rejectTransport.SendAsync(new byte[] { 1 }, "topic").AsTask();
            await rejectingClient.SendStarted.Task.WaitAsync(TimeSpan.FromSeconds(5));
            var queued = rejectTransport.SendAsync(new byte[] { 2 }, "topic").AsTask();
            await Assert.ThrowsAsync<InvalidOperationException>(async () =>
                await rejectTransport.SendAsync(new byte[] { 3 }, "topic"));
            rejectingClient.ReleaseSuccessfulSends();
            await Task.WhenAll(blocked, queued);
            await rejectTransport.CloseAsync();
            Assert.Equal(1, rejectTransport.Metrics.OverflowCount);
        }

        [Fact]
        public void EgressRejectsUndeclaredAndUnsupportedCapabilities()
        {
            var undeclared = new UndeclaredEventClient();
            var required = EventClientCapabilities.Payload
                | EventClientCapabilities.Topic
                | EventClientCapabilities.CloudEvents;
            var missingContract = Assert.Throws<InvalidOperationException>(() =>
                EventClientPubSubTransportFactory.ValidateCapabilities(undeclared, required));
            Assert.Contains("IEventClientCapabilities", missingContract.Message,
                StringComparison.Ordinal);

            var declared = new RecordingEventClient
            {
                Capabilities = EventClientCapabilities.Payload | EventClientCapabilities.Topic
            };
            var unsupported = Assert.Throws<NotSupportedException>(() =>
                EventClientPubSubTransportFactory.ValidateCapabilities(declared, required));
            Assert.Contains(nameof(EventClientCapabilities.CloudEvents), unsupported.Message,
                StringComparison.Ordinal);
        }

        [Fact]
        public async Task ManagedSourceRetainsEveryBurstNotificationAfterOwnershipTransferAsync()
        {
            var buffer = new ManagedPubSubNotificationBuffer(8);
            await using var provider = new ManagedPubSubNotificationDataSourceProvider(buffer);
            var model = new PublishedDataSetModel { Name = "data" };
            var managed = Assert.IsAssignableFrom<IManagedPubSubDataSource>(
                await provider.CreateAsync(model));
            await using var source = new ManagedPubSubDataSetSource("data", managed);
            source.Start();
            var metadata = source.BuildMetaData();

            var first = new byte[] { 1 };
            await buffer.EnqueueAsync(new ManagedPubSubNotification(
                "data", "event", DateTimeOffset.UnixEpoch, first));
            first[0] = 9;
            await buffer.EnqueueAsync(new ManagedPubSubNotification(
                "data", "event", DateTimeOffset.UnixEpoch.AddSeconds(1), [2]));
            await buffer.EnqueueAsync(new ManagedPubSubNotification(
                "data", "condition", DateTimeOffset.UnixEpoch.AddSeconds(2), [3]));

            Assert.Equal(new byte[] { 1 }, await ReadPayloadAsync(source, metadata));
            Assert.Equal(new byte[] { 2 }, await ReadPayloadAsync(source, metadata));
            Assert.Equal(new byte[] { 3 }, await ReadPayloadAsync(source, metadata));
            Assert.Equal(0, ((IManagedPubSubNotificationBufferDiagnostics)buffer).QueueDepth);
        }

        [Fact]
        public async Task DiagnosticsBridgeMapsNativeAndEgressCountersAsync()
        {
            var client = new RecordingEventClient();
            await using var transport = CreateTransport(client);
            await transport.OpenAsync();
            await transport.SendAsync(new byte[] { 1 }, "topic");
            await transport.CloseAsync();
            var native = new PubSubDiagnostics(PubSubDiagnosticsLevel.Medium);
            native.Increment(PubSubDiagnosticsCounterKind.SentNetworkMessages, 4);
            native.Increment(PubSubDiagnosticsCounterKind.SentDataSetMessages, 5);

            var diagnostic = PubSubShadowDiagnosticsBridge.Apply(
                new WriterGroupDiagnosticModel(), native, transport);

            Assert.Equal(1, diagnostic.OutgressIoTMessageCount);
            Assert.Equal(4, diagnostic.EncoderIoTMessagesProcessed);
            Assert.Equal(5, diagnostic.EncoderNotificationsProcessed);
        }

        [Fact]
        public async Task TestOnlyEgressHostUsesManagedSourceAndNativeForcedKeyframeAsync()
        {
            var client = new RecordingEventClient();
            var services = new ServiceCollection();
            services.AddLogging();
            services.AddPubSubShadowEgressHost(client, options =>
            {
                options.IncludeSchema = false;
            });
            await using var provider = services.BuildServiceProvider();
            var host = provider.GetRequiredService<IPubSubShadowHost>();
            var hosted = Assert.Single(provider.GetServices<IHostedService>());
            var buffer = provider.GetRequiredService<IManagedPubSubNotificationBuffer>();

            await hosted.StartAsync(default);
            await host.ReplaceConfigurationAsync([CreateManagedWriterGroup()]);
            await buffer.EnqueueAsync(new ManagedPubSubNotification("data", "payload",
                DateTimeOffset.UnixEpoch, [42]));
            await WaitUntilAsync(() => ((IManagedPubSubNotificationBufferDiagnostics)buffer)
                .QueueDepth == 0);
            var sources = provider.GetRequiredService<ManagedPubSubDataSetSourceRegistry>();
            Assert.True(sources.TryGetSource("data", out var nativeSource));
            var managedSource = Assert.IsType<ManagedPubSubDataSetSource>(nativeSource);
            await WaitUntilAsync(() => managedSource.PendingCount == 1);
            var priorEvents = client.Events.Count;

            await host.ForceKeyFrameAsync("group", "writer");
            Assert.Equal(0, managedSource.PendingCount);
            await WaitUntilAsync(() => client.Events.Count > priorEvents);
            await hosted.StopAsync(default);

            var published = client.Events.Skip(priorEvents).ToList();
            Assert.True(published.Any(captured => captured.Topic == "shadow/group"),
                string.Join(", ", published.Select(captured =>
                    captured.Topic + ":" + Encoding.UTF8.GetString(captured.Payload))));
            var publication = published.Single(captured => captured.Topic == "shadow/group");
            Assert.Contains("\"payload\"", Encoding.UTF8.GetString(publication.Payload),
                StringComparison.Ordinal);
            Assert.Equal(1, ((IPubSubShadowRuntimeStateProvider)provider.GetRequiredService<
                IPubSubShadowRuntimeStateProvider>()).State.StartCount);
        }

        private static EventClientPubSubTransport CreateTransport(
            RecordingEventClient client, Action<PubSubShadowEgressOptions>? configure = null)
        {
            return CreateTransport(client, CreateSettings(), configure);
        }

        private static EventClientPubSubTransport CreateTransport(
            RecordingEventClient client, PubSubShadowEgressSettings settings,
            Action<PubSubShadowEgressOptions>? configure = null)
        {
            var options = new PubSubShadowEgressOptions
            {
                InitialRetryDelay = TimeSpan.FromMilliseconds(1),
                MaximumRetryDelay = TimeSpan.FromMilliseconds(5)
            };
            configure?.Invoke(options);
            return new EventClientPubSubTransport(Profiles.PubSubMqttJsonTransport,
                PubSubTransportDirection.Send, client, settings, options, TimeProvider.System);
        }

        private static PubSubShadowEgressSettings CreateSettings()
        {
            return new PubSubShadowEgressSettings
            {
                ConnectionName = "shadow-group",
                Topic = "configured/topic",
                ContentType = "application/json",
                ContentEncoding = null,
                QualityOfService = QoS.AtLeastOnce,
                Retain = false,
                TimeToLive = null,
                UseCloudEvents = false,
                CloudEventSource = new Uri("urn:test:publisher"),
                CloudEventType = "test.pubsub",
                CloudEventSubject = "group",
                Schema = null,
                Properties = new ReadOnlyDictionary<string, string?>(
                    new Dictionary<string, string?>())
            };
        }

        private static WriterGroupModel CreateManagedWriterGroup()
        {
            return new WriterGroupModel
            {
                Id = "group",
                MessageType = MessageEncoding.Json,
                PublishingInterval = TimeSpan.FromDays(1),
                DataSetWriters =
                [
                    new DataSetWriterModel
                    {
                        Id = "writer",
                        DataSetWriterName = "writer",
                        DataSet = new PublishedDataSetModel
                        {
                            Name = "data",
                            DataSetMetaData = new DataSetMetaDataModel
                            {
                                Name = "data",
                                DataSetClassId = Guid.Empty,
                                MajorVersion = 1
                            }
                        }
                    }
                ]
            };
        }

        private static async Task<byte[]> ReadPayloadAsync(ManagedPubSubDataSetSource source,
            DataSetMetaDataType metadata)
        {
            for (var attempt = 0; attempt < 100; attempt++)
            {
                var snapshot = await source.SampleAsync(metadata);
                if (snapshot.Fields.Count != 0)
                {
                    return Assert.IsType<byte[]>(snapshot.Fields[0].Value.Value);
                }
                await Task.Delay(10);
            }
            throw new Xunit.Sdk.XunitException("The managed source did not receive its notification.");
        }

        private static async Task WaitUntilAsync(Func<bool> condition)
        {
            for (var attempt = 0; attempt < 100; attempt++)
            {
                if (condition())
                {
                    return;
                }
                await Task.Delay(10);
            }
            throw new Xunit.Sdk.XunitException("The expected condition was not reached.");
        }

        private sealed class UndeclaredEventClient : IEventClient
        {
            public string Name => "undeclared";
            public int MaxEventPayloadSizeInBytes => 1024;
            public string Identity => "undeclared";
            public IEvent CreateEvent() => throw new NotSupportedException();
        }

        private sealed class RecordingEventClient : IEventClient, IEventClientCapabilities
        {
            public string Name => "recording";
            public int MaxPayload { get; set; } = 1024;
            public int MaxEventPayloadSizeInBytes => MaxPayload;
            public string Identity => "recording";
            public EventClientCapabilities Capabilities { get; set; } =
                EventClientCapabilities.Payload
                | EventClientCapabilities.Topic
                | EventClientCapabilities.QualityOfService
                | EventClientCapabilities.Retain
                | EventClientCapabilities.TimeToLive
                | EventClientCapabilities.ContentType
                | EventClientCapabilities.ContentEncoding
                | EventClientCapabilities.CustomProperties
                | EventClientCapabilities.CloudEvents
                | EventClientCapabilities.Schema;
            public int FailuresRemaining
            {
                get => Volatile.Read(ref _failuresRemaining);
                set => Volatile.Write(ref _failuresRemaining, value);
            }
            public TaskCompletionSource SendStarted { get; } = new(
                TaskCreationOptions.RunContinuationsAsynchronously);

            public IReadOnlyList<CapturedEvent> Events
            {
                get
                {
                    lock (_gate)
                    {
                        return _events.ToArray();
                    }
                }
            }

            public IEvent CreateEvent()
            {
                return new RecordingEvent(this);
            }

            public void BlockSuccessfulSends()
            {
                _release = new TaskCompletionSource<bool>(
                    TaskCreationOptions.RunContinuationsAsynchronously);
            }

            public void ReleaseSuccessfulSends()
            {
                _release?.TrySetResult(true);
            }

            private async ValueTask SendAsync(CapturedEvent captured,
                CancellationToken cancellationToken)
            {
                SendStarted.TrySetResult();
                if (Interlocked.Decrement(ref _failuresRemaining) >= 0)
                {
                    throw new InvalidOperationException("transient");
                }
                if (_release is not null)
                {
                    await _release.Task.WaitAsync(cancellationToken).ConfigureAwait(false);
                }
                lock (_gate)
                {
                    _events.Add(captured);
                }
            }

            private sealed class RecordingEvent : IEvent
            {
                public RecordingEvent(RecordingEventClient owner)
                {
                    _owner = owner;
                }

                public IEvent SetTopic(string? value)
                {
                    _topic = value;
                    return this;
                }

                public IEvent SetTimestamp(DateTimeOffset value)
                {
                    _timestamp = value;
                    return this;
                }

                public IEvent SetContentType(string? value)
                {
                    _contentType = value;
                    return this;
                }

                public IEvent SetContentEncoding(string? value)
                {
                    _contentEncoding = value;
                    return this;
                }

                public IEvent AsCloudEvent(CloudEventHeader header)
                {
                    _cloudEvent = header;
                    return this;
                }

                public IEvent SetSchema(IEventSchema schema)
                {
                    _schema = schema;
                    return this;
                }

                public IEvent AddProperty(string name, string? value)
                {
                    _properties[name] = value;
                    return this;
                }

                public IEvent SetRetain(bool value)
                {
                    _retain = value;
                    return this;
                }

                public IEvent SetQoS(QoS value)
                {
                    _qualityOfService = value;
                    return this;
                }

                public IEvent SetTtl(TimeSpan value)
                {
                    _timeToLive = value;
                    return this;
                }

                public IEvent AddBuffers(IEnumerable<ReadOnlySequence<byte>> value)
                {
                    _payload = value.SelectMany(sequence => sequence.ToArray()).ToArray();
                    return this;
                }

                public ValueTask SendAsync(CancellationToken ct = default)
                {
                    return _owner.SendAsync(new CapturedEvent(
                        _topic, _timestamp, _contentType, _contentEncoding,
                        _qualityOfService, _retain, _timeToLive,
                        new Dictionary<string, string?>(_properties),
                        _cloudEvent, _schema, _payload), ct);
                }

                public void Dispose()
                {
                }

                private readonly RecordingEventClient _owner;
                private readonly Dictionary<string, string?> _properties =
                    new(StringComparer.Ordinal);
                private string? _topic;
                private DateTimeOffset _timestamp;
                private string? _contentType;
                private string? _contentEncoding;
                private QoS _qualityOfService;
                private bool _retain;
                private TimeSpan? _timeToLive;
                private CloudEventHeader? _cloudEvent;
                private IEventSchema? _schema;
                private byte[] _payload = [];
            }

            private readonly Lock _gate = new();
            private readonly List<CapturedEvent> _events = [];
            private TaskCompletionSource<bool>? _release;
            private int _failuresRemaining;
        }

        private sealed record CapturedEvent(string? Topic, DateTimeOffset Timestamp,
            string? ContentType, string? ContentEncoding, QoS QualityOfService, bool Retain,
            TimeSpan? TimeToLive, IReadOnlyDictionary<string, string?> Properties,
            CloudEventHeader? CloudEvent, IEventSchema? Schema, byte[] Payload);
    }
}
