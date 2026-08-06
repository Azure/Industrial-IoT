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
    using Microsoft.Extensions.Logging.Abstractions;
    using Microsoft.Extensions.Options;
    using Opc.Ua;
    using Opc.Ua.PubSub.DataSets;
    using Opc.Ua.PubSub.Diagnostics;
    using Opc.Ua.PubSub.Encoding;
    using Opc.Ua.PubSub.Transports;
    using System;
    using System.Buffers;
    using System.Collections.Generic;
    using System.Collections.Concurrent;
    using System.Collections.ObjectModel;
    using System.IO;
    using System.IO.Compression;
    using System.Linq;
    using System.Security.Authentication;
    using System.Text;
    using System.Text.Json;
    using System.Threading;
    using System.Threading.Tasks;
    using Moq;
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
                EventClient = client,
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
            await transport.SendAsync(payload, "writer/topic").AsTask()
                .WaitAsync(TimeSpan.FromSeconds(10));
            await transport.CloseAsync().AsTask().WaitAsync(TimeSpan.FromSeconds(10));

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
        public async Task EgressCanReopenAfterCloseAsync()
        {
            var client = new RecordingEventClient();
            await using var transport = CreateTransport(client);

            await transport.OpenAsync();
            await transport.SendAsync(new byte[] { 1 }, "topic");
            await transport.CloseAsync();
            Assert.False(transport.IsConnected);

            await transport.OpenAsync();
            await transport.SendAsync(new byte[] { 2 }, "topic");
            await transport.CloseAsync();

            Assert.Equal(2, client.Events.Count);
            Assert.Equal(new byte[] { 1 }, client.Events[0].Payload);
            Assert.Equal(new byte[] { 2 }, client.Events[1].Payload);
            Assert.Equal(2, transport.Metrics.SentCount);
            Assert.Equal(2, transport.Metrics.ChunkCount);
            Assert.Equal(0, transport.Metrics.QueueDepth);
        }

        [Fact]
        public async Task ConcurrentLifecycleCallsRemainIdempotentAsync()
        {
            var client = new RecordingEventClient();
            await using var transport = CreateTransport(client);

            var openStart = new TaskCompletionSource(
                TaskCreationOptions.RunContinuationsAsynchronously);
            var opens = Enumerable.Range(0, 8).Select(async _ =>
            {
                await openStart.Task;
                await transport.OpenAsync();
            }).ToArray();
            openStart.TrySetResult();
            await Task.WhenAll(opens);
            Assert.True(transport.IsConnected);
            await transport.SendAsync(new byte[] { 1 }, "topic");

            var closeStart = new TaskCompletionSource(
                TaskCreationOptions.RunContinuationsAsynchronously);
            var closes = Enumerable.Range(0, 8).Select(async _ =>
            {
                await closeStart.Task;
                await transport.CloseAsync();
            }).ToArray();
            closeStart.TrySetResult();
            await Task.WhenAll(closes);
            Assert.False(transport.IsConnected);

            await transport.OpenAsync();
            await transport.SendAsync(new byte[] { 2 }, "topic");
            await transport.CloseAsync();
            Assert.Equal(new byte[] { 1, 2 },
                client.Events.SelectMany(captured => captured.Payload).ToArray());
        }

        [Fact]
        public async Task CloseAndReopenIsolatePendingGenerationAsync()
        {
            var client = new RecordingEventClient();
            client.BlockSuccessfulSends();
            await using var transport = CreateTransport(client);
            var states = new ConcurrentQueue<bool>();
            transport.StateChanged += (_, args) => states.Enqueue(args.IsConnected);
            await transport.OpenAsync();
            var stale = transport.SendAsync(new byte[] { 1 }, "topic").AsTask();
            await client.SendStarted.Task.WaitAsync(TimeSpan.FromSeconds(5));

            var closing = transport.CloseAsync().AsTask();
            var reopening = transport.OpenAsync().AsTask();
            await closing.WaitAsync(TimeSpan.FromSeconds(5));
            await reopening.WaitAsync(TimeSpan.FromSeconds(5));
            await Assert.ThrowsAnyAsync<OperationCanceledException>(() => stale);

            client.ReleaseSuccessfulSends();
            await transport.SendAsync(new byte[] { 2 }, "topic");
            await transport.CloseAsync();

            var sent = Assert.Single(client.Events);
            Assert.Equal(new byte[] { 2 }, sent.Payload);
            Assert.Equal(0, transport.Metrics.QueueDepth);
            Assert.Equal(new[] { true, false, true, false }, states);
        }

        [Fact]
        public async Task CloseCancelsActiveQueuedAndBackpressuredGenerationAsync()
        {
            var client = new RecordingEventClient();
            client.BlockSuccessfulSends();
            await using var transport = CreateTransport(client, options =>
            {
                options.QueueCapacity = 1;
            });
            await transport.OpenAsync();
            var active = transport.SendAsync(new byte[] { 1 }, "topic").AsTask();
            await client.SendStarted.Task.WaitAsync(TimeSpan.FromSeconds(5));
            var queued = transport.SendAsync(new byte[] { 2 }, "topic").AsTask();
            var backpressured = transport.SendAsync(new byte[] { 3 }, "topic").AsTask();
            await WaitUntilAsync(() => transport.Metrics.BackpressureCount == 1);

            var closing = transport.CloseAsync().AsTask();
            var reopening = transport.OpenAsync().AsTask();
            await closing.WaitAsync(TimeSpan.FromSeconds(5));
            await reopening.WaitAsync(TimeSpan.FromSeconds(5));

            await Assert.ThrowsAnyAsync<OperationCanceledException>(() => active);
            await Assert.ThrowsAnyAsync<OperationCanceledException>(() => queued);
            await Assert.ThrowsAnyAsync<OperationCanceledException>(() => backpressured);
            client.ReleaseSuccessfulSends();
            await transport.SendAsync(new byte[] { 4 }, "topic");
            await transport.CloseAsync();

            var sent = Assert.Single(client.Events);
            Assert.Equal(new byte[] { 4 }, sent.Payload);
            Assert.Equal(0, transport.Metrics.QueueDepth);
        }

        [Fact]
        public async Task CloseCancellationDoesNotPermitOverlappingGenerationAsync()
        {
            var client = new RecordingEventClient { IgnoreCancellation = true };
            client.BlockSuccessfulSends();
            await using var transport = CreateTransport(client);
            await transport.OpenAsync();
            var sending = transport.SendAsync(new byte[] { 1 }, "topic").AsTask();
            await client.SendStarted.Task.WaitAsync(TimeSpan.FromSeconds(5));
            using var cts = new CancellationTokenSource(TimeSpan.FromMilliseconds(25));

            await Assert.ThrowsAnyAsync<OperationCanceledException>(async () =>
                await transport.CloseAsync(cts.Token));
            var reopening = transport.OpenAsync().AsTask();
            await Task.Delay(25);
            Assert.False(reopening.IsCompleted);

            client.ReleaseSuccessfulSends();
            await reopening.WaitAsync(TimeSpan.FromSeconds(5));
            await sending.WaitAsync(TimeSpan.FromSeconds(5));
            await transport.SendAsync(new byte[] { 2 }, "topic");
            await transport.CloseAsync();

            Assert.Equal(new byte[] { 1, 2 },
                client.Events.SelectMany(captured => captured.Payload).ToArray());
        }

        [Fact]
        public async Task DisposeIsTerminalAndCancelsActiveGenerationAsync()
        {
            var client = new RecordingEventClient { FailuresRemaining = int.MaxValue };
            var transport = CreateTransport(client);
            await transport.OpenAsync();
            var sending = transport.SendAsync(new byte[] { 1 }, "topic").AsTask();
            await client.SendStarted.Task.WaitAsync(TimeSpan.FromSeconds(5));

            await transport.DisposeAsync();

            await Assert.ThrowsAnyAsync<OperationCanceledException>(() => sending);
            await Assert.ThrowsAsync<ObjectDisposedException>(async () =>
                await transport.OpenAsync());
            await Assert.ThrowsAsync<InvalidOperationException>(async () =>
                await transport.SendAsync(new byte[] { 2 }, "topic"));
            await transport.DisposeAsync();
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
        public void EgressDropsASchemaTheTransportCannotCarryButKeepsDeliverySemantics()
        {
            //
            // A schema describes the message, not its delivery, so a transport
            // that cannot carry one still publishes every byte of telemetry
            // under the requested guarantees. Refusing to start there would
            // leave IoT Hub users unable to enable schema publishing at all.
            //
            var withSchema = CreateSettings() with
            {
                Schema = new PubSubShadowEventSchema("group", PubSubShadowEncoding.Json)
            };
            var client = new RecordingEventClient
            {
                Capabilities = EventClientCapabilities.Payload
                    | EventClientCapabilities.Topic
                    | EventClientCapabilities.ContentType
                    | EventClientCapabilities.QualityOfService
            };

            var degraded = EventClientPubSubTransportFactory.DegradeUnsupportedCapabilities(
                client, withSchema, NullLogger.Instance);

            Assert.Null(degraded.Schema);
            Assert.Equal(withSchema.Topic, degraded.Topic);
            Assert.Equal(withSchema.QualityOfService, degraded.QualityOfService);
            EventClientPubSubTransportFactory.ValidateCapabilities(client,
                degraded.RequiredCapabilities);

            //
            // Delivery guarantees the user asked for are never dropped.
            //
            var retained = withSchema with { Retain = true };
            var refused = EventClientPubSubTransportFactory.DegradeUnsupportedCapabilities(
                client, retained, NullLogger.Instance);
            Assert.Throws<NotSupportedException>(() =>
                EventClientPubSubTransportFactory.ValidateCapabilities(client,
                    refused.RequiredCapabilities));
        }

        [Fact]
        public void EgressPublishesOverATransportWithoutContentTypeOrProperties()
        {
            //
            // MQTT 3.1.1 has no field for a content type or user properties.
            // They annotate the message rather than delivering it, and the
            // writer path publishes over 3.1.1 regardless, so requiring them
            // would refuse a transport the Publisher has always supported.
            //
            var settings = CreateSettings() with
            {
                Properties = new Dictionary<string, string?>(StringComparer.Ordinal)
                {
                    ["writerGroupId"] = "group"
                }
            };
            var client = new RecordingEventClient
            {
                Capabilities = EventClientCapabilities.Payload
                    | EventClientCapabilities.Topic
                    | EventClientCapabilities.QualityOfService
                    | EventClientCapabilities.Retain
            };

            var degraded = EventClientPubSubTransportFactory.DegradeUnsupportedCapabilities(
                client, settings, NullLogger.Instance);

            EventClientPubSubTransportFactory.ValidateCapabilities(client,
                degraded.RequiredCapabilities);

            //
            // The values are still offered to the client, which drops what its
            // protocol has no field for.
            //
            Assert.Equal(settings.ContentType, degraded.ContentType);
            Assert.Equal(settings.Properties, degraded.Properties);

            //
            // A time to live is a delivery semantic and is still refused.
            //
            var withTtl = settings with { TimeToLive = TimeSpan.FromMinutes(1) };
            var stillRefused = EventClientPubSubTransportFactory.DegradeUnsupportedCapabilities(
                client, withTtl, NullLogger.Instance);
            Assert.Throws<NotSupportedException>(() =>
                EventClientPubSubTransportFactory.ValidateCapabilities(client,
                    stillRefused.RequiredCapabilities));
        }

        [Fact]
        public void EgressKeepsASchemaTheTransportCanCarry()
        {
            var withSchema = CreateSettings() with
            {
                Schema = new PubSubShadowEventSchema("group", PubSubShadowEncoding.Json)
            };
            var client = new RecordingEventClient
            {
                Capabilities = withSchema.RequiredCapabilities
            };

            Assert.Same(withSchema,
                EventClientPubSubTransportFactory.DegradeUnsupportedCapabilities(
                    client, withSchema, NullLogger.Instance));
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
            services.AddIsolatedPubSubShadowEgressHost(client,
                options => options.IncludeSchema = false);
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
            services.AddIsolatedPubSubShadowEgressHost(client,
                options => options.IncludeSchema = false);
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
            services.AddIsolatedPubSubShadowEgressHost(client, options =>
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
        public async Task TombstoneJournalDoesNotBlockReplaceOrStopWhenCapacityIsExceededAsync()
        {
            var client = new RecordingEventClient();
            var services = new ServiceCollection();
            services.AddLogging();
            services.AddIsolatedPubSubShadowEgressHost(client, options =>
            {
                options.IncludeSchema = false;
                options.QueueCapacity = 1;
                options.InitialRetryDelay = TimeSpan.FromMilliseconds(1);
                options.MaximumRetryDelay = TimeSpan.FromMilliseconds(5);
            });
            await using var provider = services.BuildServiceProvider();
            var host = provider.GetRequiredService<IPubSubShadowHost>();
            var hosted = Assert.Single(provider.GetServices<IHostedService>());
            var tombstones = provider.GetRequiredService<PubSubShadowEgressRegistration>()
                .Tombstones;
            var groups = Enumerable.Range(0, 3).Select(index =>
            {
                var group = CreateManagedWriterGroup(groupId: "group-" + index,
                    writerId: "writer-" + index, dataSetName: "data-" + index);
                group.DataSetWriters![0].MetaData = new PublishingQueueSettingsModel
                {
                    QueueName = "metadata/deadlock/" + index,
                    Retain = true
                };
                return group;
            }).ToArray();

            await hosted.StartAsync(default);
            await host.ReplaceConfigurationAsync(groups);
            client.FailuresRemaining = int.MaxValue;
            var replacing = host.ReplaceConfigurationAsync([]).AsTask();
            var stopping = hosted.StopAsync(default);
            await Task.WhenAll(replacing, stopping).WaitAsync(TimeSpan.FromSeconds(30));
            Assert.Equal(groups.Length, tombstones.PendingCount);

            client.FailuresRemaining = 0;
            await hosted.StartAsync(default);
            await WaitUntilAsync(() => tombstones.PendingCount == 0);
            await hosted.StopAsync(default);
        }

        [Fact]
        public async Task ReactivationCancelsAnOlderInFlightTombstoneAsync()
        {
            var client = new RecordingEventClient();
            var options = new PubSubShadowEgressOptions
            {
                InitialRetryDelay = TimeSpan.FromMilliseconds(1),
                MaximumRetryDelay = TimeSpan.FromMilliseconds(5)
            };
            await using var tombstones = new PubSubShadowTombstoneQueue(options);
            var settings = CreateSettings(retain: true, eventClient: client);
            const string topic = "metadata/reactivated";
            client.BlockSuccessfulSends();
            var removedGeneration = tombstones.NextGeneration();
            tombstones.Persist(settings, topic, removedGeneration);
            await client.SendStarted.Task.WaitAsync(TimeSpan.FromSeconds(5));

            await tombstones.ReactivateAsync(topic, tombstones.NextGeneration());
            Assert.Equal(0, tombstones.PendingCount);
            client.ReleaseSuccessfulSends();

            await using var transport = CreateTransport(client, settings);
            await transport.OpenAsync();
            await transport.SendAsync(new byte[] { 7 }, topic);
            await transport.CloseAsync();
            await Task.Delay(25);

            var retained = Assert.Single(client.Events);
            Assert.Equal(new byte[] { 7 }, retained.Payload);
            Assert.True(retained.Retain);
        }

        [Fact]
        public async Task ReactivationRestoreRequeuesCleanupAfterReplacementRollbackAsync()
        {
            var client = new RecordingEventClient();
            var options = new PubSubShadowEgressOptions
            {
                InitialRetryDelay = TimeSpan.FromMilliseconds(1),
                MaximumRetryDelay = TimeSpan.FromMilliseconds(5)
            };
            await using var tombstones = new PubSubShadowTombstoneQueue(options);
            var settings = CreateSettings(retain: true, eventClient: client);
            const string topic = "metadata/rollback";
            client.BlockSuccessfulSends();
            var removedGeneration = tombstones.NextGeneration();
            tombstones.Persist(settings, topic, removedGeneration);
            await client.SendStarted.Task.WaitAsync(TimeSpan.FromSeconds(5));

            var reactivation = await tombstones.ReactivateAsync(
                topic, tombstones.NextGeneration());
            Assert.NotNull(reactivation);
            tombstones.Restore(reactivation!);
            client.ReleaseSuccessfulSends();
            await WaitUntilAsync(() => tombstones.PendingCount == 0);

            var tombstone = Assert.Single(client.Events);
            Assert.Equal(topic, tombstone.Topic);
            Assert.Empty(tombstone.Payload);
        }

        [Fact]
        public async Task ReintroducedRetainedMetadataInvalidatesPendingTombstoneAsync()
        {
            var client = new RecordingEventClient();
            var services = new ServiceCollection();
            services.AddLogging();
            services.AddIsolatedPubSubShadowEgressHost(client, options =>
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
                QueueName = "metadata/reintroduced",
                Retain = true
            };

            await hosted.StartAsync(default);
            await host.ReplaceConfigurationAsync([group]);
            client.FailuresRemaining = int.MaxValue;
            await host.ReplaceConfigurationAsync([]);
            await WaitUntilAsync(() => tombstones.PendingCount == 1
                && tombstones.RetryCount != 0);

            client.FailuresRemaining = 0;
            await host.ReplaceConfigurationAsync([group]);
            var afterReactivation = client.Events.Count;
            await WaitUntilAsync(() => tombstones.PendingCount == 0);
            await Task.Delay(25);
            await hosted.StopAsync(default);

            Assert.DoesNotContain(client.Events.Skip(afterReactivation), captured =>
                captured.Topic == "metadata/reintroduced"
                && captured.Retain && captured.Payload.Length == 0);
        }

        [Fact]
        public async Task ConfigurationRemovalFailsWhenClientCannotTombstoneAsync()
        {
            var client = new RecordingEventClient { SupportsRetainedTombstones = false };
            var services = new ServiceCollection();
            services.AddLogging();
            services.AddIsolatedPubSubShadowEgressHost(client,
                options => options.IncludeSchema = false);
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
                await provider.CreateAsync("data", model));
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
                await provider.CreateAsync("data", new PublishedDataSetModel { Name = "data" }));
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
                await provider.CreateAsync("data", new PublishedDataSetModel { Name = "data" }));
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
        public async Task KeyframeBoundaryIncludesOrRetainsADataUpdateExactlyOnceAsync()
        {
            var buffer = new ManagedPubSubNotificationBuffer(8);
            await using var provider = new ManagedPubSubNotificationDataSourceProvider(buffer);
            var managed = Assert.IsAssignableFrom<IManagedPubSubDataSource>(
                await provider.CreateAsync("data", new PublishedDataSetModel { Name = "data" }));
            var observer = new BlockingDataPublicationObserver();
            await using var source = new ManagedPubSubDataSetSource("data", managed,
                observer: observer);
            source.Start();
            var metadata = source.BuildMetaData();
            await buffer.EnqueueAsync(new ManagedPubSubNotification(
                "data", "value", DateTimeOffset.UnixEpoch, [4]));
            await observer.Allocated.Task.WaitAsync(TimeSpan.FromSeconds(5));

            source.RequestKeyFrame();
            var keyframeTask = Task.Run(async () => await source.SampleAsync(metadata));
            observer.Release();
            var keyframe = await keyframeTask.WaitAsync(TimeSpan.FromSeconds(5));
            await WaitUntilAsync(() => source.PendingCount == 1);

            Assert.Equal(1, keyframe.Fields.Count);
            Assert.Equal(new byte[] { 4 },
                Assert.IsType<byte[]>(keyframe.Fields[0].Value.Value));
            Assert.Equal(0, (await source.SampleAsync(metadata)).Fields.Count);
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
        public async Task QueueDepthNeverGoesNegativeUnderConcurrentEnqueueAndDequeueAsync()
        {
            const int kCount = 128;
            var client = new RecordingEventClient();
            await using var transport = CreateTransport(client, options =>
            {
                options.QueueCapacity = 8;
            });
            await transport.OpenAsync();
            client.BlockSuccessfulSends();
            var transportDepths = new ConcurrentBag<int>();
            using var monitorStop = new CancellationTokenSource();
            var monitor = Task.Run(async () =>
            {
                while (!monitorStop.IsCancellationRequested)
                {
                    transportDepths.Add(transport.Metrics.QueueDepth);
                    await Task.Delay(1);
                }
            });
            await Task.Delay(10);
            var sends = Enumerable.Range(0, kCount).Select(index =>
                transport.SendAsync(new byte[] { (byte)index }, "topic").AsTask()).ToArray();
            await WaitUntilAsync(() => transport.Metrics.BackpressureCount != 0);
            client.ReleaseSuccessfulSends();
            await Task.WhenAll(sends);
            monitorStop.Cancel();
            await monitor;
            await transport.CloseAsync();

            Assert.NotEmpty(transportDepths);
            Assert.All(transportDepths, depth => Assert.InRange(depth, 0, 8));
            Assert.Equal(0, transport.Metrics.QueueDepth);

            var buffer = new ManagedPubSubNotificationBuffer(8);
            var bufferDepths = new ConcurrentBag<int>();
            using var bufferMonitorStop = new CancellationTokenSource();
            var bufferMonitor = Task.Run(async () =>
            {
                while (!bufferMonitorStop.IsCancellationRequested)
                {
                    bufferDepths.Add(buffer.QueueDepth);
                    await Task.Delay(1);
                }
            });
            await Task.Delay(10);
            var writes = Enumerable.Range(0, kCount).Select(index =>
                buffer.EnqueueAsync(new ManagedPubSubNotification("data", "value",
                    DateTimeOffset.UnixEpoch.AddMilliseconds(index), [(byte)index])).AsTask())
                .ToArray();
            await WaitUntilAsync(() => buffer.BackpressureCount != 0);
            var reader = Task.Run(async () =>
            {
                var read = 0;
                await using var enumerator = buffer.ReadAllAsync().GetAsyncEnumerator();
                while (read < kCount && await enumerator.MoveNextAsync())
                {
                    read++;
                }
            });
            await Task.WhenAll(writes);
            await reader;
            bufferMonitorStop.Cancel();
            await bufferMonitor;

            Assert.NotEmpty(bufferDepths);
            Assert.All(bufferDepths, depth => Assert.InRange(depth, 0, 8));
            Assert.Equal(0, buffer.QueueDepth);
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
            services.AddIsolatedPubSubShadowEgressHost(client, options =>
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
            var expectedTopic = ExpectedTelemetryTopic(new PublisherOptions(),
                CreateManagedWriterGroup(MessageEncoding.JsonGzip));
            Assert.True(published.Any(captured => captured.Topic == expectedTopic),
                string.Join(", ", published.Select(captured => captured.Topic)));
            var publication = published.Single(captured => captured.Topic == expectedTopic);
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
            IReadOnlyList<PubSubShadowMetadataWriterSettings>? metadataWriters = null,
            bool retain = false,
            IEventClient? eventClient = null)
        {
            return new PubSubShadowEgressSettings
            {
                ConnectionName = "shadow-group",
                EventClient = eventClient ?? new RecordingEventClient(),
                Encoding = encoding,
                Topic = "configured/topic",
                ContentType = "application/json",
                ContentEncoding = contentEncoding,
                QualityOfService = QoS.AtLeastOnce,
                Retain = retain,
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
            MessageEncoding encoding = MessageEncoding.Json, string groupId = "group",
            string writerId = "writer", string dataSetName = "data")
        {
            return new WriterGroupModel
            {
                Id = groupId,
                MessageType = encoding,
                PublishingInterval = TimeSpan.FromDays(1),
                DataSetWriters =
                [
                    new DataSetWriterModel
                    {
                        Id = writerId,
                        DataSetWriterName = writerId,
                        DataSet = new PublishedDataSetModel
                        {
                            Name = dataSetName,
                            DataSetMetaData = new DataSetMetaDataModel
                            {
                                Name = dataSetName,
                                DataSetClassId = Guid.Empty,
                                MajorVersion = 1
                            }
                        }
                    }
                ]
            };
        }

        // ── PubSubShadowTombstoneQueue validation ──────────────────────────────

        [Fact]
        public async Task TombstoneQueue_Persist_NullSettings_ThrowsArgumentNullExceptionAsync()
        {
            await using var queue = new PubSubShadowTombstoneQueue(new PubSubShadowEgressOptions());

            Assert.Throws<ArgumentNullException>(() =>
                queue.Persist(null!, "topic", 1));
        }

        [Theory]
        [InlineData(null)]
        [InlineData("")]
        [InlineData("   ")]
        public async Task TombstoneQueue_Persist_NullOrEmptyTopic_ThrowsArgumentExceptionAsync(string? topic)
        {
            await using var queue = new PubSubShadowTombstoneQueue(new PubSubShadowEgressOptions());
            var settings = CreateSettings();

            Assert.Throws<ArgumentException>(() =>
                queue.Persist(settings, topic!, 1));
        }

        [Fact]
        public async Task TombstoneQueue_Restore_NullReactivation_ThrowsArgumentNullExceptionAsync()
        {
            await using var queue = new PubSubShadowTombstoneQueue(new PubSubShadowEgressOptions());

            Assert.Throws<ArgumentNullException>(() =>
                queue.Restore(null!));
        }

        [Fact]
        public async Task TombstoneQueue_PendingCount_IsZeroOnFreshQueueAsync()
        {
            await using var queue = new PubSubShadowTombstoneQueue(new PubSubShadowEgressOptions());

            Assert.Equal(0, queue.PendingCount);
        }

        [Fact]
        public async Task TombstoneQueue_RetryCount_IsZeroOnFreshQueueAsync()
        {
            await using var queue = new PubSubShadowTombstoneQueue(new PubSubShadowEgressOptions());

            Assert.Equal(0, queue.RetryCount);
        }

        [Fact]
        public async Task TombstoneQueue_NextGeneration_ReturnsMonotonicallyIncreasingValuesAsync()
        {
            await using var queue = new PubSubShadowTombstoneQueue(new PubSubShadowEgressOptions());

            var gen1 = queue.NextGeneration();
            var gen2 = queue.NextGeneration();
            var gen3 = queue.NextGeneration();

            Assert.Equal(1L, gen1);
            Assert.Equal(2L, gen2);
            Assert.Equal(3L, gen3);
        }

        [Fact]
        public async Task TombstoneQueue_ReactivateAsync_UnknownTopic_ReturnsNullAsync()
        {
            await using var queue = new PubSubShadowTombstoneQueue(new PubSubShadowEgressOptions());

            var reactivation = await queue.ReactivateAsync("no-such-topic", 1);

            Assert.Null(reactivation);
        }

        // ── PubSubShadowEgressRegistration ─────────────────────────────────────

        [Fact]
        public void EgressRegistration_Constructor_InitializesPropertiesCorrectly()
        {
            var client = Mock.Of<IEventClient>();
            var selector = new PubSubShadowSingleEventClientSelector(client);
            var options = new PubSubShadowEgressOptions();

            var registration = new PubSubShadowEgressRegistration(selector, options);

            Assert.Same(selector, registration.EventClients);
            Assert.Same(options, registration.Options);
            Assert.NotNull(registration.Settings);
            Assert.NotNull(registration.Tombstones);
            registration.Dispose();
        }

        [Fact]
        public async Task EgressRegistration_DisposeAsync_CompletesCleanlyAsync()
        {
            var client = Mock.Of<IEventClient>();
            var selector = new PubSubShadowSingleEventClientSelector(client);
            var registration = new PubSubShadowEgressRegistration(selector,
                new PubSubShadowEgressOptions());

            var ex = await Record.ExceptionAsync(async () =>
                await registration.DisposeAsync());

            Assert.Null(ex);
        }

        [Fact]
        public void EgressRegistration_Dispose_CompletesCleanly()
        {
            var client = Mock.Of<IEventClient>();
            var selector = new PubSubShadowSingleEventClientSelector(client);
            var registration = new PubSubShadowEgressRegistration(selector,
                new PubSubShadowEgressOptions());

            var ex = Record.Exception(() => registration.Dispose());

            Assert.Null(ex);
        }

        [Fact]
        public void EgressRegistration_NullEventClients_ThrowsArgumentNullException()
        {
            Assert.Throws<ArgumentNullException>(() =>
                new PubSubShadowEgressRegistration(null!, new PubSubShadowEgressOptions()));
        }

        [Fact]
        public void EgressRegistration_NullOptions_ThrowsArgumentNullException()
        {
            var selector = new PubSubShadowSingleEventClientSelector(Mock.Of<IEventClient>());

            Assert.Throws<ArgumentNullException>(() =>
                new PubSubShadowEgressRegistration(selector, null!));
        }

        // ── ValidateTombstoneCapability ─────────────────────────────────────────

        [Fact]
        public void ValidateTombstoneCapability_ClientWithoutInterface_ThrowsNotSupportedException()
        {
            var client = Mock.Of<IEventClient>();

            Assert.Throws<NotSupportedException>(() =>
                EventClientPubSubTransportFactory.ValidateTombstoneCapability(client));
        }

        [Fact]
        public void ValidateTombstoneCapability_ClientReturnsFalse_ThrowsNotSupportedException()
        {
            var client = new RecordingEventClient { SupportsRetainedTombstones = false };

            Assert.Throws<NotSupportedException>(() =>
                EventClientPubSubTransportFactory.ValidateTombstoneCapability(client));
        }

        [Fact]
        public void ValidateTombstoneCapability_ClientReturnsTrue_DoesNotThrow()
        {
            var client = new RecordingEventClient { SupportsRetainedTombstones = true };

            var ex = Record.Exception(() =>
                EventClientPubSubTransportFactory.ValidateTombstoneCapability(client));

            Assert.Null(ex);
        }

        [Fact]
        public void EgressPublishesToTheSameTopicTheWriterPathComputes()
        {
            //
            // The native runtime must publish where the custom path publishes,
            // otherwise no consumer receives the telemetry. The custom path
            // resolves the writer group topic through the Publisher topic
            // templates, so both must agree for the same configuration.
            //
            foreach (var (publisherId, groupPublisherId, groupName) in
                new (string?, string?, string?)[]
                {
                    (null, null, null),
                    ("publisher", null, null),
                    ("publisher", null, "the group"),
                    ("publisher", "group-owned", "the group")
                })
            {
                var options = new PublisherOptions { PublisherId = publisherId };
                var writerGroup = CreateManagedWriterGroup();
                writerGroup.PublisherId = groupPublisherId;
                writerGroup.Name = groupName;

                var registry = new PubSubShadowEgressSettingsRegistry(
                    new PubSubShadowSingleEventClientSelector(new RecordingEventClient()));
                registry.Replace([writerGroup], options, new PubSubShadowEgressOptions());
                var settings = Assert.Single(registry.Snapshot().Values);

                var expected = ExpectedTelemetryTopic(options, writerGroup);

                Assert.Equal(expected, settings.Topic);
                Assert.DoesNotContain("shadow/", settings.Topic, StringComparison.Ordinal);
            }
        }

        /// <summary>
        /// Computes the topic the custom writer path resolves for a writer
        /// group, so the native egress can be asserted against it rather than
        /// against a hard-coded literal.
        /// </summary>
        /// <param name="options"></param>
        /// <param name="writerGroup"></param>
        private static string ExpectedTelemetryTopic(PublisherOptions options,
            WriterGroupModel writerGroup)
        {
            var writerGroupName = TopicFilter.Escape(writerGroup.Name
                ?? Constants.DefaultWriterGroupName);
            return new TopicBuilder(options, writerGroup.MessageType,
                new TopicTemplatesOptions(),
                new Dictionary<string, string>
                {
                    [PublisherConfig.PublisherIdKey] = TopicFilter.Escape(
                        writerGroup.PublisherId ?? options.PublisherId
                            ?? Constants.DefaultPublisherId),
                    [PublisherConfig.WriterGroupIdVariableName] = writerGroup.Id,
                    [PublisherConfig.DataSetWriterGroupVariableName] = writerGroupName,
                    [PublisherConfig.WriterGroupVariableName] = writerGroupName
                }).TelemetryTopic;
        }

        [Fact]
        public void EgressPrefersAnExplicitlyConfiguredQueueName()
        {
            var writerGroup = CreateManagedWriterGroup();
            writerGroup.Publishing = new PublishingQueueSettingsModel
            {
                QueueName = "explicit/topic"
            };
            var registry = new PubSubShadowEgressSettingsRegistry(
                new PubSubShadowSingleEventClientSelector(new RecordingEventClient()));
            registry.Replace([writerGroup], new PublisherOptions(),
                new PubSubShadowEgressOptions());

            Assert.Equal("explicit/topic", Assert.Single(registry.Snapshot().Values).Topic);
        }

        [Fact]
        public void EgressPublishesOverATransportWithoutAQualityOfServiceKnob()
        {
            //
            // The QualityOfService capability means the client exposes a
            // per-message delivery setting, not that it can deliver reliably.
            // IoT Hub is queued and acknowledged with no per-message knob, so
            // demanding the capability refused the transport the module is
            // normally deployed with, for a guarantee it already provides.
            // Retain and time to live are still refused, because a message
            // that is not retained or that outlives its deadline is a real
            // loss of function.
            //
            var client = new RecordingEventClient
            {
                Capabilities = EventClientCapabilities.Payload
                    | EventClientCapabilities.Topic
                    | EventClientCapabilities.ContentType
            };
            var settings = CreateSettings() with { QualityOfService = QoS.AtLeastOnce };

            var degraded = EventClientPubSubTransportFactory.DegradeUnsupportedCapabilities(
                client, settings, NullLogger.Instance);
            EventClientPubSubTransportFactory.ValidateCapabilities(client,
                degraded.RequiredCapabilities);

            //
            // The delivery guarantee is still carried to the client, which
            // applies whatever its protocol supports.
            //
            Assert.Equal(QoS.AtLeastOnce, degraded.QualityOfService);

            var retained = settings with { Retain = true };
            var refused = EventClientPubSubTransportFactory.DegradeUnsupportedCapabilities(
                client, retained, NullLogger.Instance);
            Assert.Throws<NotSupportedException>(() =>
                EventClientPubSubTransportFactory.ValidateCapabilities(client,
                    refused.RequiredCapabilities));
        }

        [Fact]
        public void EgressResolvesTheMetaDataTopicTheWriterPathComputes()
        {
            //
            // The writer path applies the metadata topic template, so a
            // configuration naming one through --mdt must reach the same topic
            // here. Falling back to a synthetic topic silently stops every
            // consumer of the metadata stream.
            //
            var writerGroup = CreateManagedWriterGroup();
            var options = new PublisherOptions
            {
                PublisherId = "publisher"
            };
            options.TopicTemplates.DataSetMetaData = "{TelemetryTopic}/metadatamessage";
            var registry = new PubSubShadowEgressSettingsRegistry(
                new PubSubShadowSingleEventClientSelector(new RecordingEventClient()));

            registry.Replace([writerGroup], options, new PubSubShadowEgressOptions());

            var settings = Assert.Single(registry.Snapshot().Values);
            var metadata = Assert.Single(settings.MetadataWriters);
            Assert.Equal(settings.Topic + "/metadatamessage", metadata.Publishing?.QueueName);
        }

        [Fact]
        public void EgressSuppressesMetaDataWhenTheConfigurationDisablesIt()
        {
            //
            // The writer path suppresses the announcement when metadata is
            // disabled, but the native runtime announces on its own schedule
            // and has no per-writer switch, so the egress has to drop what the
            // configuration asked not to publish.
            //
            var writerGroup = CreateManagedWriterGroup();
            var registry = new PubSubShadowEgressSettingsRegistry(
                new PubSubShadowSingleEventClientSelector(new RecordingEventClient()));

            registry.Replace([writerGroup], new PublisherOptions
            {
                DisableDataSetMetaData = true
            }, new PubSubShadowEgressOptions());

            var metadata = Assert.Single(
                Assert.Single(registry.Snapshot().Values).MetadataWriters);
            Assert.False(metadata.Enabled);
        }

        [Fact]
        public void EgressPrefersAnExplicitlyConfiguredMetaDataQueueName()
        {
            var writerGroup = CreateManagedWriterGroup();
            writerGroup.DataSetWriters![0].MetaData = new PublishingQueueSettingsModel
            {
                QueueName = "explicit/metadata"
            };
            var options = new PublisherOptions();
            options.TopicTemplates.DataSetMetaData = "{TelemetryTopic}/metadatamessage";
            var registry = new PubSubShadowEgressSettingsRegistry(
                new PubSubShadowSingleEventClientSelector(new RecordingEventClient()));

            registry.Replace([writerGroup], options, new PubSubShadowEgressOptions());

            var metadata = Assert.Single(
                Assert.Single(registry.Snapshot().Values).MetadataWriters);
            Assert.Equal("explicit/metadata", metadata.Publishing?.QueueName);
        }

        [Fact]
        public void EgressResolvesTheTransportPerWriterGroup()
        {
            //
            // Writer groups may name their own transport, so two groups must be
            // able to publish through two different clients. Sharing one
            // application-wide client would silently route a group to the wrong
            // transport.
            //
            var mqtt = new RecordingEventClient();
            var hub = new RecordingEventClient();
            var registry = new PubSubShadowEgressSettingsRegistry(
                new PerGroupEventClientSelector(new Dictionary<string, IEventClient>
                {
                    ["group-a"] = mqtt,
                    ["group-b"] = hub
                }));

            registry.Replace(
                [
                    CreateManagedWriterGroup(groupId: "group-a", writerId: "writer-a",
                        dataSetName: "data-a"),
                    CreateManagedWriterGroup(groupId: "group-b", writerId: "writer-b",
                        dataSetName: "data-b")
                ],
                new PublisherOptions(), new PubSubShadowEgressOptions());

            var snapshot = registry.Snapshot();
            Assert.Same(mqtt, snapshot["shadow-group-a"].EventClient);
            Assert.Same(hub, snapshot["shadow-group-b"].EventClient);
        }

        private sealed class PerGroupEventClientSelector : IPubSubShadowEventClientSelector
        {
            public PerGroupEventClientSelector(Dictionary<string, IEventClient> clients)
            {
                _clients = clients;
            }

            public IEventClient Select(WriterGroupModel writerGroup)
            {
                return _clients[writerGroup.Id!];
            }

            private readonly Dictionary<string, IEventClient> _clients;
        }

        [Fact]
        public async Task ManagedSourcePublishesTypedValuesAndDerivedMetaDataAsync()
        {
            var buffer = new ManagedPubSubNotificationBuffer(8);
            await using var provider = new ManagedPubSubNotificationDataSourceProvider(buffer);
            var managed = Assert.IsAssignableFrom<IManagedPubSubDataSource>(
                await provider.CreateAsync("data", new PublishedDataSetModel { Name = "data" }));
            await using var source = new ManagedPubSubDataSetSource("data", managed);
            source.Start();
            var metadata = source.BuildMetaData();

            await buffer.EnqueueAsync(new ManagedPubSubNotification("data", "counter",
                DateTimeOffset.UnixEpoch, new DataValue(new Variant(42),
                    StatusCodes.Good, DateTimeUtc.From(DateTimeOffset.UnixEpoch))));
            await buffer.EnqueueAsync(new ManagedPubSubNotification("data", "label",
                DateTimeOffset.UnixEpoch.AddSeconds(1), new DataValue(new Variant("ok"),
                    StatusCodes.Good, DateTimeUtc.From(DateTimeOffset.UnixEpoch))));

            var first = await ReadFieldAsync(source, metadata);
            Assert.Equal("counter", first.Name);
            Assert.Equal(42, Assert.IsType<int>(first.Value.Value));

            var second = await ReadFieldAsync(source, metadata);
            Assert.Equal("label", second.Name);
            Assert.Equal("ok", Assert.IsType<string>(second.Value.Value));

            // Metadata must describe the real field types, not opaque byte strings.
            metadata = source.BuildMetaData();
            var counter = Assert.Single(metadata.Fields.AsEnumerable(),
                field => field.Name == "counter");
            Assert.Equal((byte)BuiltInType.Int32, counter.BuiltInType);
            Assert.Equal(ValueRanks.Scalar, counter.ValueRank);
            var label = Assert.Single(metadata.Fields.AsEnumerable(),
                field => field.Name == "label");
            Assert.Equal((byte)BuiltInType.String, label.BuiltInType);
        }

        [Fact]
        public async Task ManagedSourcePreservesBadStatusOnPublishedFieldAsync()
        {
            var buffer = new ManagedPubSubNotificationBuffer(8);
            await using var provider = new ManagedPubSubNotificationDataSourceProvider(buffer);
            var managed = Assert.IsAssignableFrom<IManagedPubSubDataSource>(
                await provider.CreateAsync("data", new PublishedDataSetModel { Name = "data" }));
            await using var source = new ManagedPubSubDataSetSource("data", managed);
            source.Start();
            var metadata = source.BuildMetaData();

            await buffer.EnqueueAsync(new ManagedPubSubNotification("data", "faulted",
                DateTimeOffset.UnixEpoch, new DataValue(default,
                    StatusCodes.BadNotConnected, DateTimeUtc.From(DateTimeOffset.UnixEpoch))));

            var field = await ReadFieldAsync(source, metadata);
            Assert.Equal("faulted", field.Name);
            Assert.Equal(StatusCodes.BadNotConnected, field.StatusCode.Code);
        }

        [Fact]
        public async Task ManagedSourcePrefersMetaDataDeclaredBySourceAsync()
        {
            var declared = new DataSetMetaDataType
            {
                Name = "declared",
                Fields =
                [
                    new FieldMetaData
                    {
                        Name = "temperature",
                        BuiltInType = (byte)BuiltInType.Double,
                        DataType = DataTypeIds.Double,
                        ValueRank = ValueRanks.Scalar
                    }
                ],
                ConfigurationVersion = new ConfigurationVersionDataType { MajorVersion = 7 }
            };
            var buffer = new ManagedPubSubNotificationBuffer(8);
            await using var source = new ManagedPubSubDataSetSource("data",
                new DeclaredMetaDataSource(buffer, declared));
            source.Start();

            var metadata = source.BuildMetaData();

            Assert.Equal("declared", metadata.Name);
            Assert.Equal(7u, metadata.ConfigurationVersion.MajorVersion);
            var field = Assert.Single(metadata.Fields.AsEnumerable());
            Assert.Equal("temperature", field.Name);
            Assert.Equal((byte)BuiltInType.Double, field.BuiltInType);
        }

        private sealed class DeclaredMetaDataSource : IManagedPubSubDataSource
        {
            public DeclaredMetaDataSource(ManagedPubSubNotificationBuffer buffer,
                DataSetMetaDataType metaData)
            {
                _buffer = buffer;
                MetaData = metaData;
            }

            public DataSetMetaDataType? MetaData { get; }

            public IAsyncEnumerable<ManagedPubSubNotification> ReadNotificationsAsync(
                CancellationToken cancellationToken = default)
            {
                return _buffer.ReadAllAsync(cancellationToken);
            }

            private readonly ManagedPubSubNotificationBuffer _buffer;
        }

        private static async Task<DataSetField> ReadFieldAsync(
            ManagedPubSubDataSetSource source, DataSetMetaDataType metadata)
        {
            for (var attempt = 0; attempt < 100; attempt++)
            {
                var snapshot = await source.SampleAsync(metadata);
                if (snapshot.Fields.Count != 0)
                {
                    return snapshot.Fields[0];
                }
                await Task.Delay(10);
            }
            throw new Xunit.Sdk.XunitException("The managed source did not receive its notification.");
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

        private sealed class BlockingDataPublicationObserver :
            IManagedPubSubDataPublicationObserver
        {
            public TaskCompletionSource Allocated { get; } = new(
                TaskCreationOptions.RunContinuationsAsynchronously);

            public void AfterSequenceAllocated(long sequence)
            {
                Allocated.TrySetResult();
                _release.Task.GetAwaiter().GetResult();
            }

            public void Release()
            {
                _release.TrySetResult();
            }

            private readonly TaskCompletionSource _release = new(
                TaskCreationOptions.RunContinuationsAsynchronously);
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
            public bool IgnoreCancellation { get; set; }
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
                    if (IgnoreCancellation)
                    {
                        await _release.Task.ConfigureAwait(false);
                    }
                    else
                    {
                        await _release.Task.WaitAsync(cancellationToken).ConfigureAwait(false);
                    }
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
