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
    using Microsoft.Extensions.Options;
    using Opc.Ua;
    using Opc.Ua.PubSub.Diagnostics;
    using Opc.Ua.PubSub.Transports;
    using System;
    using System.Buffers;
    using System.Collections.Generic;
    using System.Collections.ObjectModel;
    using System.IO;
    using System.IO.Compression;
    using System.Linq;
    using System.Security.Authentication;
    using System.Text;
    using System.Text.Json;
    using System.Threading;
    using System.Threading.Tasks;
    using Xunit;

    public sealed class EventClientPubSubTransportTests
    {
        [Fact]
        public async Task EgressPreservesEventClientHeadersWithoutByteSlicingAsync()
        {
            var client = new RecordingEventClient { MaxPayload = 1024 };
            await using var transport = CreateTransport(client, new PubSubShadowEgressSettings
            {
                ConnectionName = "shadow-group",
                Encoding = PubSubShadowEncoding.Json,
                Topic = "configured/topic",
                ContentType = "application/json",
                ContentEncoding = null,
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

            var payload = Encoding.UTF8.GetBytes("""{"MessageType":"ua-data"}""");
            await transport.SendAsync(payload, "writer/topic");
            await transport.CloseAsync();

            var events = client.Events;
            var captured = Assert.Single(events);
            Assert.All(events, sent =>
            {
                Assert.Equal("writer/topic", sent.Topic);
                Assert.Equal(QoS.AtLeastOnce, sent.QualityOfService);
                Assert.True(sent.Retain);
                Assert.Equal(TimeSpan.FromMinutes(1), sent.TimeToLive);
                Assert.Equal("application/json", sent.ContentType);
                Assert.Null(sent.ContentEncoding);
                Assert.Equal("property", sent.Properties["custom"]);
                Assert.NotNull(sent.CloudEvent);
                Assert.NotNull(sent.Schema);
            });
            Assert.Equal(payload, captured.Payload);
            using var _ = JsonDocument.Parse(captured.Payload);
            Assert.Equal(1, transport.Metrics.ChunkCount);
        }

        [Theory]
        [InlineData(PubSubShadowEncoding.JsonGzip)]
        [InlineData(PubSubShadowEncoding.JsonReversibleGzip)]
        public async Task EgressCompressesGzipPayloadBeforeSendingAsync(
            PubSubShadowEncoding encoding)
        {
            var client = new RecordingEventClient { MaxPayload = 128 };
            var settings = CreateSettings(encoding, "gzip");
            await using var transport = CreateTransport(client, settings);
            await transport.OpenAsync();
            var payload = Encoding.UTF8.GetBytes(
                "{\"MessageType\":\"" + new string('x', 512) + "\"}");

            await transport.SendAsync(payload, "topic");
            await transport.CloseAsync();

            var captured = Assert.Single(client.Events);
            Assert.Equal("gzip", captured.ContentEncoding);
            Assert.Equal(payload, Decompress(captured.Payload));
            using var _ = JsonDocument.Parse(Decompress(captured.Payload));
        }

        [Fact]
        public async Task EgressRejectsOversizeFramesWithoutCreatingInvalidFragmentsAsync()
        {
            var client = new RecordingEventClient { MaxPayload = 3 };
            await using var transport = CreateTransport(client);
            await transport.OpenAsync();

            var exception = await Assert.ThrowsAsync<PubSubShadowPayloadTooLargeException>(
                async () => await transport.SendAsync(new byte[] { 1, 2, 3, 4 }, "topic"));
            await transport.CloseAsync();

            Assert.Equal(4, exception.PayloadSize);
            Assert.Empty(client.Events);
            Assert.Equal(1, transport.Metrics.FailedCount);
            Assert.Equal(0, transport.Metrics.ChunkCount);
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
            Assert.Equal(0, transport.Metrics.FailedCount);
            var diagnostic = PubSubShadowDiagnosticsBridge.Apply(
                new WriterGroupDiagnosticModel(),
                new PubSubDiagnostics(PubSubDiagnosticsLevel.Medium), transport);
            Assert.Equal(0, diagnostic.OutgressIoTMessageFailedCount);
            Assert.Equal(2, diagnostic.ConnectionRetries);
        }

        [Fact]
        public async Task TerminalFailureReleasesFifoHeadForFollowingFramesAsync()
        {
            var client = new RecordingEventClient
            {
                PermanentFailure = new AuthenticationException("permanent"),
                TerminalFailuresRemaining = 1
            };
            await using var transport = CreateTransport(client);
            await transport.OpenAsync();

            var first = transport.SendAsync(new byte[] { 1 }, "topic").AsTask();
            var second = transport.SendAsync(new byte[] { 2 }, "topic").AsTask();
            await Assert.ThrowsAsync<PubSubShadowTerminalEgressException>(() => first);
            await second;
            await transport.CloseAsync();

            var sent = Assert.Single(client.Events);
            Assert.Equal(new byte[] { 2 }, sent.Payload);
            Assert.Equal(1, transport.Metrics.FailedCount);
            Assert.Equal(0, transport.Metrics.RetryCount);
        }

        [Fact]
        public async Task RetryExhaustionReleasesFifoHeadForFollowingFramesAsync()
        {
            var client = new RecordingEventClient { FailuresRemaining = 3 };
            await using var transport = CreateTransport(client, options =>
            {
                options.MaxSendAttempts = 3;
            });
            await transport.OpenAsync();

            var first = transport.SendAsync(new byte[] { 1 }, "topic").AsTask();
            var second = transport.SendAsync(new byte[] { 2 }, "topic").AsTask();
            var exception = await Assert.ThrowsAsync<PubSubShadowRetryLimitExceededException>(
                () => first);
            await second;
            await transport.CloseAsync();

            Assert.Equal(3, exception.Attempts);
            var sent = Assert.Single(client.Events);
            Assert.Equal(new byte[] { 2 }, sent.Payload);
            Assert.Equal(2, transport.Metrics.RetryCount);
            Assert.Equal(1, transport.Metrics.FailedCount);
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
        public async Task EgressCloseCancelsActiveRetriesAndCompletesPendingFramesAsync()
        {
            var client = new RecordingEventClient { FailuresRemaining = int.MaxValue };
            await using var transport = CreateTransport(client);
            await transport.OpenAsync();
            var sending = transport.SendAsync(new byte[] { 1 }, "topic").AsTask();
            await client.SendStarted.Task.WaitAsync(TimeSpan.FromSeconds(5));

            await transport.CloseAsync().AsTask().WaitAsync(TimeSpan.FromSeconds(2));
            await Assert.ThrowsAnyAsync<OperationCanceledException>(() => sending);
            Assert.Equal(0, transport.Metrics.QueueDepth);
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
        public async Task EgressUsesPerWriterMetadataPublishingSettingsAsync()
        {
            var client = new RecordingEventClient();
            var settings = CreateSettings(metadataWriters:
            [
                new PubSubShadowMetadataWriterSettings
                {
                    WriterName = "writer",
                    Publishing = new PublishingQueueSettingsModel
                    {
                        QueueName = "metadata/topic",
                        RequestedDeliveryGuarantee = QoS.AtMostOnce,
                        Retain = false,
                        Ttl = TimeSpan.FromSeconds(30)
                    }
                }
            ]);
            var connection = new PubSubConnectionDataType
            {
                Name = settings.ConnectionName,
                WriterGroups =
                [
                    new WriterGroupDataType
                    {
                        WriterGroupId = 3,
                        DataSetWriters =
                        [
                            new DataSetWriterDataType
                            {
                                Name = "writer",
                                DataSetWriterId = 4
                            }
                        ]
                    }
                ]
            };
            var routing = EventClientPubSubTransportFactory.CreateMetadataRouting(
                connection, settings);
            await using var transport = new EventClientPubSubTransport(
                Profiles.PubSubMqttJsonTransport, PubSubTransportDirection.Send,
                client, settings, routing, new PubSubShadowEgressOptions
                {
                    InitialRetryDelay = TimeSpan.FromMilliseconds(1),
                    MaximumRetryDelay = TimeSpan.FromMilliseconds(5)
                }, TimeProvider.System);
            await transport.OpenAsync();

            var topic = transport.BuildMetaDataTopic(default, 3, 4);
            await transport.SendAsync(new byte[] { 1 }, topic);
            await transport.CloseAsync();

            var sent = Assert.Single(client.Events);
            Assert.Equal("metadata/topic", sent.Topic);
            Assert.Equal(QoS.AtMostOnce, sent.QualityOfService);
            Assert.False(sent.Retain);
            Assert.Equal(TimeSpan.FromSeconds(30), sent.TimeToLive);
        }

        [Fact]
        public async Task ConfigurationRemovalPublishesRetainedMetadataTombstoneAsync()
        {
            var client = new RecordingEventClient();
            var services = new ServiceCollection();
            services.AddLogging();
            services.AddPubSubShadowEgressHost(client, options => options.IncludeSchema = false);
            await using var provider = services.BuildServiceProvider();
            var host = provider.GetRequiredService<IPubSubShadowHost>();
            var hosted = Assert.Single(provider.GetServices<IHostedService>());
            var tombstones = provider.GetRequiredService<PubSubShadowEgressRegistration>()
                .Tombstones;
            var group = CreateManagedWriterGroup();
            group.DataSetWriters![0].MetaData = new PublishingQueueSettingsModel
            {
                QueueName = "metadata/removed",
                Retain = true
            };

            await hosted.StartAsync(default);
            await host.ReplaceConfigurationAsync([group]);
            var before = client.Events.Count;
            await host.ReplaceConfigurationAsync([]);
            await WaitUntilAsync(() => tombstones.PendingCount == 0);
            await hosted.StopAsync(default);

            var tombstone = client.Events.Skip(before).Single(captured =>
                captured.Topic == "metadata/removed");
            Assert.True(tombstone.Retain);
            Assert.Empty(tombstone.Payload);
        }

        [Fact]
        public async Task ConfigurationRetentionChangeClearsPreviousMetadataAsync()
        {
            var client = new RecordingEventClient();
            var services = new ServiceCollection();
            services.AddLogging();
            services.AddPubSubShadowEgressHost(client, options => options.IncludeSchema = false);
            await using var provider = services.BuildServiceProvider();
            var host = provider.GetRequiredService<IPubSubShadowHost>();
            var hosted = Assert.Single(provider.GetServices<IHostedService>());
            var tombstones = provider.GetRequiredService<PubSubShadowEgressRegistration>()
                .Tombstones;
            var retained = CreateManagedWriterGroup();
            retained.DataSetWriters![0].MetaData = new PublishingQueueSettingsModel
            {
                QueueName = "metadata/retention-change",
                Retain = true
            };
            var transient = CreateManagedWriterGroup();
            transient.DataSetWriters![0].MetaData = new PublishingQueueSettingsModel
            {
                QueueName = "metadata/retention-change",
                Retain = false
            };

            await hosted.StartAsync(default);
            await host.ReplaceConfigurationAsync([retained]);
            var before = client.Events.Count;
            await host.ReplaceConfigurationAsync([transient]);
            await WaitUntilAsync(() => tombstones.PendingCount == 0);
            await hosted.StopAsync(default);

            Assert.Contains(client.Events.Skip(before), captured =>
                captured.Topic == "metadata/retention-change"
                && captured.Retain && captured.Payload.Length == 0);
        }

        [Fact]
        public async Task TombstoneQueueRetriesAndSurvivesHostRestartAsync()
        {
            var client = new RecordingEventClient();
            var services = new ServiceCollection();
            services.AddLogging();
            services.AddPubSubShadowEgressHost(client, options =>
            {
                options.IncludeSchema = false;
                options.InitialRetryDelay = TimeSpan.FromMilliseconds(1);
                options.MaximumRetryDelay = TimeSpan.FromMilliseconds(5);
            });
            await using var provider = services.BuildServiceProvider();
            var host = provider.GetRequiredService<IPubSubShadowHost>();
            var hosted = Assert.Single(provider.GetServices<IHostedService>());
            var tombstones = provider.GetRequiredService<PubSubShadowEgressRegistration>()
                .Tombstones;
            var group = CreateManagedWriterGroup();
            group.DataSetWriters![0].MetaData = new PublishingQueueSettingsModel
            {
                QueueName = "metadata/retry",
                Retain = true
            };

            await hosted.StartAsync(default);
            await host.ReplaceConfigurationAsync([group]);
            client.BlockSuccessfulSends();
            client.FailuresRemaining = 1;
            await host.ReplaceConfigurationAsync([]);
            await WaitUntilAsync(() => tombstones.RetryCount != 0
                && tombstones.PendingCount == 1);

            await hosted.StopAsync(default);
            await hosted.StartAsync(default);
            Assert.Equal(1, tombstones.PendingCount);
            client.ReleaseSuccessfulSends();
            await WaitUntilAsync(() => tombstones.PendingCount == 0);
            await hosted.StopAsync(default);

            Assert.Contains(client.Events, captured => captured.Topic == "metadata/retry"
                && captured.Retain && captured.Payload.Length == 0);
        }

        [Fact]
        public async Task ConfigurationRemovalFailsWhenClientCannotTombstoneAsync()
        {
            var client = new RecordingEventClient { SupportsRetainedTombstones = false };
            var services = new ServiceCollection();
            services.AddLogging();
            services.AddPubSubShadowEgressHost(client, options => options.IncludeSchema = false);
            await using var provider = services.BuildServiceProvider();
            var host = provider.GetRequiredService<IPubSubShadowHost>();
            var hosted = Assert.Single(provider.GetServices<IHostedService>());
            var group = CreateManagedWriterGroup();
            group.DataSetWriters![0].MetaData = new PublishingQueueSettingsModel
            {
                QueueName = "metadata/removed",
                Retain = true
            };

            await hosted.StartAsync(default);
            await host.ReplaceConfigurationAsync([group]);
            await Assert.ThrowsAsync<NotSupportedException>(async () =>
                await host.ReplaceConfigurationAsync([]));
            await hosted.StopAsync(default);
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
        public async Task ManagedSourceUsesCurrentStateForKeyframesAndKeepsEventsInFifoAsync()
        {
            var buffer = new ManagedPubSubNotificationBuffer(8);
            await using var provider = new ManagedPubSubNotificationDataSourceProvider(buffer);
            var managed = Assert.IsAssignableFrom<IManagedPubSubDataSource>(
                await provider.CreateAsync(new PublishedDataSetModel { Name = "data" }));
            await using var source = new ManagedPubSubDataSetSource("data", managed);
            source.Start();
            var metadata = source.BuildMetaData();
            await buffer.EnqueueAsync(new ManagedPubSubNotification(
                "data", "first", DateTimeOffset.UnixEpoch, [1]));
            await buffer.EnqueueAsync(new ManagedPubSubNotification(
                "data", "second", DateTimeOffset.UnixEpoch.AddSeconds(1), [2]));

            Assert.Equal(new byte[] { 1 }, await ReadPayloadAsync(source, metadata));
            Assert.Equal(new byte[] { 2 }, await ReadPayloadAsync(source, metadata));

            metadata = source.BuildMetaData();
            Assert.Equal(2, metadata.Fields.Count);
            source.RequestKeyFrame();
            var keyframe = await source.SampleAsync(metadata);
            Assert.Equal(2, keyframe.Fields.Count);
            Assert.Equal("first", keyframe.Fields[0].Name);
            Assert.Equal("second", keyframe.Fields[1].Name);

            await buffer.EnqueueAsync(new ManagedPubSubNotification(
                "data", "event", DateTimeOffset.UnixEpoch.AddSeconds(2), [3],
                ManagedPubSubNotificationKind.Event));
            Assert.Equal(new byte[] { 3 }, await ReadPayloadAsync(source, metadata));
        }

        [Fact]
        public async Task KeyframeWatermarkSuppressesObsoleteDataButPreservesQueuedEventsAsync()
        {
            var buffer = new ManagedPubSubNotificationBuffer(8);
            await using var provider = new ManagedPubSubNotificationDataSourceProvider(buffer);
            var managed = Assert.IsAssignableFrom<IManagedPubSubDataSource>(
                await provider.CreateAsync(new PublishedDataSetModel { Name = "data" }));
            await using var source = new ManagedPubSubDataSetSource("data", managed);
            source.Start();
            await buffer.EnqueueAsync(new ManagedPubSubNotification(
                "data", "value", DateTimeOffset.UnixEpoch, [1]));
            await buffer.EnqueueAsync(new ManagedPubSubNotification(
                "data", "alarm", DateTimeOffset.UnixEpoch.AddMilliseconds(1), [9],
                ManagedPubSubNotificationKind.Event));
            await buffer.EnqueueAsync(new ManagedPubSubNotification(
                "data", "value", DateTimeOffset.UnixEpoch.AddMilliseconds(2), [2]));
            await WaitUntilAsync(() => source.PendingCount == 3);

            var metadata = source.BuildMetaData();
            source.RequestKeyFrame();
            var keyframe = await source.SampleAsync(metadata);
            Assert.Equal(2, keyframe.Fields.Count);
            var value = keyframe.Fields[0].Name == "value"
                ? keyframe.Fields[0]
                : keyframe.Fields[1];
            Assert.Equal(new byte[] { 2 }, Assert.IsType<byte[]>(value.Value.Value));

            var next = await source.SampleAsync(metadata);
            Assert.Equal(1, next.Fields.Count);
            Assert.Equal("alarm", next.Fields[0].Name);
            Assert.Equal(new byte[] { 9 },
                Assert.IsType<byte[]>(next.Fields[0].Value.Value));
            Assert.Equal(0, (await source.SampleAsync(metadata)).Fields.Count);

            await buffer.EnqueueAsync(new ManagedPubSubNotification(
                "data", "value", DateTimeOffset.UnixEpoch.AddMilliseconds(3), [3]));
            Assert.Equal(new byte[] { 3 }, await ReadPayloadAsync(source, metadata));
        }

        [Fact]
        public async Task ManagedRoutesPropagateBackpressureAndDoNotReplayAfterRemovalAsync()
        {
            var options = Options.Create(new ManagedPubSubNotificationBufferOptions
            {
                Capacity = 1
            });
            var buffer = new ManagedPubSubNotificationBuffer(1);
            await using var provider = new ManagedPubSubNotificationDataSourceProvider(buffer,
                options);
            await using var registry = new ManagedPubSubDataSetSourceRegistry([provider], options);
            var group = CreateManagedWriterGroup();
            await using (var transaction = await registry.PrepareAsync([group]))
            {
                transaction.Install();
                await transaction.CommitAsync();
            }
            Assert.True(registry.TryGetSource("data", out var first));
            var source = Assert.IsType<ManagedPubSubDataSetSource>(first);
            await buffer.EnqueueAsync(new ManagedPubSubNotification(
                "data", "value", DateTimeOffset.UnixEpoch, [1]));
            await WaitUntilAsync(() => source.PendingCount == 1);
            var producers = Enumerable.Range(2, 10).Select(value =>
                buffer.EnqueueAsync(new ManagedPubSubNotification(
                    "data", "value", DateTimeOffset.UnixEpoch.AddSeconds(value), [(byte)value]))
                    .AsTask()).ToArray();
            await Task.Delay(50);
            Assert.Contains(producers, producer => !producer.IsCompleted);

            var metadata = source.BuildMetaData();
            for (var attempt = 0; attempt < 100 && !Task.WhenAll(producers).IsCompleted; attempt++)
            {
                _ = await source.SampleAsync(metadata);
                await Task.Delay(10);
            }
            await Task.WhenAll(producers).WaitAsync(TimeSpan.FromSeconds(5));

            await using (var transaction = await registry.PrepareAsync([]))
            {
                transaction.Install();
                await transaction.CommitAsync();
            }
            await using (var transaction = await registry.PrepareAsync([group]))
            {
                transaction.Install();
                await transaction.CommitAsync();
            }
            Assert.True(registry.TryGetSource("data", out var readded));
            var replacement = Assert.IsType<ManagedPubSubDataSetSource>(readded);
            Assert.NotSame(source, replacement);
            await Task.Delay(50);
            Assert.Equal(0, replacement.PendingCount);
        }

        [Fact]
        public async Task FailedSourceTransactionDoesNotLeaveAStagedRouteBlockingDispatchAsync()
        {
            var buffer = new ManagedPubSubNotificationBuffer(4);
            await using var provider = new ManagedPubSubNotificationDataSourceProvider(buffer);
            await using var registry = new ManagedPubSubDataSetSourceRegistry([provider]);
            var activeGroup = CreateManagedWriterGroup();
            await using (var transaction = await registry.PrepareAsync([activeGroup]))
            {
                transaction.Install();
                await transaction.CommitAsync();
            }
            Assert.True(registry.TryGetSource("data", out var active));
            var activeSource = Assert.IsType<ManagedPubSubDataSetSource>(active);
            var stagedGroup = new WriterGroupModel
            {
                Id = "staged",
                DataSetWriters =
                [
                    new DataSetWriterModel
                    {
                        Id = "staged-writer",
                        DataSet = new PublishedDataSetModel { Name = "staged-data" }
                    }
                ]
            };

            await using (var failed = await registry.PrepareAsync([activeGroup, stagedGroup]))
            {
                failed.Install();
            }
            await buffer.EnqueueAsync(new ManagedPubSubNotification(
                "staged-data", "value", DateTimeOffset.UnixEpoch, [1]));
            await buffer.EnqueueAsync(new ManagedPubSubNotification(
                "data", "value", DateTimeOffset.UnixEpoch, [2]));

            await WaitUntilAsync(() => activeSource.PendingCount == 1);
            var sample = await activeSource.SampleAsync(activeSource.BuildMetaData());
            Assert.Equal(1, sample.Fields.Count);
            Assert.Equal(new byte[] { 2 },
                Assert.IsType<byte[]>(sample.Fields[0].Value.Value));
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
            Assert.Equal(0, diagnostic.OutgressIoTMessageFailedCount);
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
            await host.ReplaceConfigurationAsync([CreateManagedWriterGroup(MessageEncoding.JsonGzip)]);
            await buffer.EnqueueAsync(new ManagedPubSubNotification("data", "payload",
                DateTimeOffset.UnixEpoch, [42]));
            await WaitUntilAsync(() => ((IManagedPubSubNotificationBufferDiagnostics)buffer)
                .QueueDepth == 0);
            var sources = provider.GetRequiredService<ManagedPubSubDataSetSourceRegistry>();
            Assert.True(sources.TryGetSource("data", out var nativeSource));
            var managedSource = Assert.IsType<ManagedPubSubDataSetSource>(nativeSource);
            await WaitUntilAsync(() => managedSource.PendingCount == 1);
            var priorEvents = client.Events.Count;

            await provider.GetRequiredService<IPubSubKeyFrameControl>()
                .ForceKeyFrameAsync("group", "writer");
            Assert.Equal(1, managedSource.PendingCount);
            await WaitUntilAsync(() => client.Events.Count > priorEvents);
            await hosted.StopAsync(default);

            var published = client.Events.Skip(priorEvents).ToList();
            Assert.True(published.Any(captured => captured.Topic == "shadow/group"),
                string.Join(", ", published.Select(captured =>
                    captured.Topic + ":" + Encoding.UTF8.GetString(captured.Payload))));
            var publication = published.Single(captured => captured.Topic == "shadow/group");
            Assert.Equal("gzip", publication.ContentEncoding);
            var decoded = Decompress(publication.Payload);
            Assert.Contains("\"payload\"", Encoding.UTF8.GetString(decoded),
                StringComparison.Ordinal);
            using var _ = JsonDocument.Parse(decoded);
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

        private static PubSubShadowEgressSettings CreateSettings(
            PubSubShadowEncoding encoding = PubSubShadowEncoding.Json,
            string? contentEncoding = null,
            IReadOnlyList<PubSubShadowMetadataWriterSettings>? metadataWriters = null)
        {
            return new PubSubShadowEgressSettings
            {
                ConnectionName = "shadow-group",
                Encoding = encoding,
                Topic = "configured/topic",
                ContentType = "application/json",
                ContentEncoding = contentEncoding,
                QualityOfService = QoS.AtLeastOnce,
                Retain = false,
                TimeToLive = null,
                UseCloudEvents = false,
                CloudEventSource = new Uri("urn:test:publisher"),
                CloudEventType = "test.pubsub",
                CloudEventSubject = "group",
                Schema = null,
                Properties = new ReadOnlyDictionary<string, string?>(
                    new Dictionary<string, string?>()),
                MetadataWriters = metadataWriters ?? []
            };
        }

        private static byte[] Decompress(byte[] payload)
        {
            using var input = new MemoryStream(payload);
            using var gzip = new GZipStream(input, CompressionMode.Decompress);
            using var output = new MemoryStream();
            gzip.CopyTo(output);
            return output.ToArray();
        }

        private static WriterGroupModel CreateManagedWriterGroup(
            MessageEncoding encoding = MessageEncoding.Json)
        {
            return new WriterGroupModel
            {
                Id = "group",
                MessageType = encoding,
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

        private sealed class RecordingEventClient : IEventClient, IEventClientCapabilities,
            IEventClientRetainedTombstoneCapabilities
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
            public bool SupportsRetainedTombstones { get; set; } = true;
            public Exception? PermanentFailure { get; set; }
            public int TerminalFailuresRemaining
            {
                get => Volatile.Read(ref _terminalFailuresRemaining);
                set => Volatile.Write(ref _terminalFailuresRemaining, value);
            }
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
                if (PermanentFailure is not null
                    && Interlocked.Decrement(ref _terminalFailuresRemaining) >= 0)
                {
                    throw PermanentFailure;
                }
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
            private int _terminalFailuresRemaining;
        }

        private sealed record CapturedEvent(string? Topic, DateTimeOffset Timestamp,
            string? ContentType, string? ContentEncoding, QoS QualityOfService, bool Retain,
            TimeSpan? TimeToLive, IReadOnlyDictionary<string, string?> Properties,
            CloudEventHeader? CloudEvent, IEventSchema? Schema, byte[] Payload);
    }
}
