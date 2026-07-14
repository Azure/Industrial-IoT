// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Azure.IIoT.OpcUa.Publisher.Tests.PubSub
{
    using Azure.IIoT.OpcUa.Publisher.Models;
    using Azure.IIoT.OpcUa.Publisher.PubSub;
    using Azure.IIoT.OpcUa.Publisher.Stack;
    using Microsoft.Extensions.DependencyInjection;
    using Microsoft.Extensions.Hosting;
    using Microsoft.Extensions.Options;
    using Moq;
    using Opc.Ua;
    using Opc.Ua.PubSub.Application;
    using Opc.Ua.PubSub.Configuration;
    using Opc.Ua.PubSub.DataSets;
    using Opc.Ua.PubSub.Diagnostics;
    using Opc.Ua.PubSub.Encoding;
    using Opc.Ua.PubSub.Groups;
    using JsonDecoder = Opc.Ua.PubSub.Encoding.Json.JsonDecoder;
    using JsonNetworkMessage = Opc.Ua.PubSub.Encoding.Json.JsonNetworkMessage;
    using JsonWriterGroupMessageDataType = Opc.Ua.JsonWriterGroupMessageDataType;
    using Opc.Ua.PubSub.Encoding.Uadp;
    using Opc.Ua.PubSub.MetaData;
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.IO.Compression;
    using System.Linq;
    using System.Text;
    using System.Threading;
    using System.Threading.Tasks;
    using Xunit;

    /// <summary>
    /// Tests for the inert application-wide standard PubSub host.
    /// </summary>
    public sealed class PubSubShadowHostTests
    {
        [Fact]
        public async Task EmptyHostStartsAndStopsExactlyOnceAsync()
        {
            var services = new ServiceCollection();
            services.AddLogging();
            services.AddIsolatedPubSubShadowHost();
            await using var provider = services.BuildServiceProvider();

            var host = provider.GetRequiredService<IPubSubShadowHost>();
            var hostedServices = provider.GetServices<IHostedService>().ToList();
            var stateProvider = provider.GetRequiredService<IPubSubShadowRuntimeStateProvider>();

            Assert.Single(hostedServices);
            Assert.Same(host, hostedServices[0]);

            await hostedServices[0].StartAsync(default);
            await hostedServices[0].StartAsync(default);
            await hostedServices[0].StopAsync(default);
            await hostedServices[0].StopAsync(default);

            var state = stateProvider.State;
            Assert.False(state.IsRunning);
            Assert.Equal(1, state.StartCount);
            Assert.Equal(1, state.StopCount);
        }

        [Fact]
        public async Task TranslatorPreservesStableIdsAndStandardConfigurationAsync()
        {
            var store = new PubSubTestIdentityStore();
            var registry = new PubSubIdentityRegistry(store);
            var translator = new PubSubConfigurationTranslator();
            var writerGroup = CreateWriterGroup("group-a", "writer-a", MessageEncoding.Uadp);

            PubSubConfigurationDataType configuration;
            await using (var transaction = await registry.BeginAsync())
            {
                configuration = translator.Translate([writerGroup], transaction);
                await transaction.CommitAsync();
            }

            var nativeGroup = Single(configuration.Connections).WriterGroups[0];
            var nativeWriter = Single(nativeGroup.DataSetWriters);
            var nativeDataSet = Single(configuration.PublishedDataSets);

            Assert.NotEqual(0, nativeGroup.WriterGroupId);
            Assert.NotEqual(0, nativeWriter.DataSetWriterId);
            Assert.Equal("group-a", nativeGroup.Name);
            Assert.Equal("writer-a", nativeWriter.Name);
            Assert.Equal("dataset-writer-a", nativeWriter.DataSetName);
            Assert.Equal("dataset-writer-a", nativeDataSet.DataSetMetaData.Name);
            Assert.True(nativeGroup.MessageSettings.TryGetValue(
                out UadpWriterGroupMessageDataType? groupSettings));
            Assert.NotNull(groupSettings);
            Assert.True(nativeWriter.MessageSettings.TryGetValue(
                out UadpDataSetWriterMessageDataType? writerSettings));
            Assert.NotNull(writerSettings);

            var sameRegistry = new PubSubIdentityRegistry(store);
            await using var sameTransaction = await sameRegistry.BeginAsync();
            var reordered = translator.Translate([writerGroup], sameTransaction);
            Assert.Equal(nativeGroup.WriterGroupId,
                Single(reordered.Connections).WriterGroups[0].WriterGroupId);
            Assert.Equal(nativeWriter.DataSetWriterId,
                Single(reordered.Connections).WriterGroups[0]
                    .DataSetWriters[0].DataSetWriterId);
        }

        [Fact]
        public async Task TranslatorUsesPositivePublisherPublishingIntervalAndValidMasksAsync()
        {
            var options = Options.Create(new PublisherOptions
            {
                BatchTriggerInterval = TimeSpan.FromSeconds(3)
            });
            var translator = new PubSubConfigurationTranslator(options);
            var registry = new PubSubIdentityRegistry(new PubSubTestIdentityStore());
            var group = CreateWriterGroup("group-a", "writer-a", MessageEncoding.Uadp);
            group.PublishingInterval = null;

            PubSubConfigurationDataType configuration;
            await using (var transaction = await registry.BeginAsync())
            {
                configuration = translator.Translate([group], transaction);
                await transaction.CommitAsync();
            }

            var nativeGroup = Single(configuration.Connections).WriterGroups[0];
            Assert.Equal(TimeSpan.FromSeconds(3).TotalMilliseconds,
                nativeGroup.PublishingInterval);
            Assert.True(nativeGroup.PublishingInterval > 0);
            new PubSubConfigurationValidator([Profiles.PubSubUdpUadpTransport])
                .Validate(configuration)
                .ThrowIfInvalid();
        }

        [Fact]
        public async Task TranslatorNormalizesEveryNonPositivePublishingIntervalAsync()
        {
            foreach (var interval in new TimeSpan?[]
            {
                null,
                TimeSpan.Zero,
                TimeSpan.FromMilliseconds(-1),
                TimeSpan.FromMilliseconds(25)
            })
            {
                var translator = new PubSubConfigurationTranslator(Options.Create(
                    new PublisherOptions { BatchTriggerInterval = TimeSpan.Zero }));
                var registry = new PubSubIdentityRegistry(new PubSubTestIdentityStore());
                var group = CreateWriterGroup("group-" + interval, "writer-" + interval,
                    MessageEncoding.Uadp);
                group.PublishingInterval = interval;
                PubSubConfigurationDataType configuration;
                await using (var transaction = await registry.BeginAsync())
                {
                    configuration = translator.Translate([group], transaction);
                    await transaction.CommitAsync();
                }

                var nativeGroup = Single(configuration.Connections).WriterGroups[0];
                Assert.True(nativeGroup.PublishingInterval > 0);
                Assert.Equal(interval is { } value && value > TimeSpan.Zero
                    ? value.TotalMilliseconds
                    : PublisherConfig.BatchTriggerIntervalLLegacyDefaultMillis,
                    nativeGroup.PublishingInterval);
                new PubSubConfigurationValidator([Profiles.PubSubUdpUadpTransport])
                    .Validate(configuration)
                    .ThrowIfInvalid();
            }
        }

        [Fact]
        public async Task TranslatorUsesEncodingAwareMasksForEveryPublicFlagAsync()
        {
            foreach (var encoding in new[] { MessageEncoding.Json, MessageEncoding.Uadp })
            {
                foreach (var networkFlag in Enum.GetValues<NetworkMessageContentFlags>())
                {
                    await AssertMasksAsync(encoding, networkFlag, null, null);
                }
                foreach (var messageFlag in Enum.GetValues<DataSetMessageContentFlags>())
                {
                    await AssertMasksAsync(encoding, null, messageFlag, null);
                }
                foreach (var fieldFlag in Enum.GetValues<DataSetFieldContentFlags>())
                {
                    await AssertMasksAsync(encoding, null, null, fieldFlag);
                }
                await AssertMasksAsync(encoding, null, null, null);
            }
        }

        [Fact]
        public async Task IdentityRegistrySurvivesRestartAndRetainsRemovedIdentitiesAsync()
        {
            var path = ".pubsub-identity-registry-" + Guid.NewGuid().ToString("N") + ".json";
            try
            {
                var first = new PubSubIdentityRegistry(
                    new FilePubSubIdentityRegistryStore(path));
                ushort groupId;
                ushort writerId;
                await using (var transaction = await first.BeginAsync())
                {
                    groupId = transaction.GetOrAllocate("writer-group", "group-a");
                    writerId = transaction.GetOrAllocate("data-set-writer", "writer-a");
                    await transaction.CommitAsync();
                }

                var restarted = new PubSubIdentityRegistry(
                    new FilePubSubIdentityRegistryStore(path));
                await using (var transaction = await restarted.BeginAsync())
                {
                    Assert.Equal(writerId,
                        transaction.GetOrAllocate("data-set-writer", "writer-a"));
                    var addedWriter = transaction.GetOrAllocate("data-set-writer", "writer-b");
                    Assert.NotEqual(writerId, addedWriter);
                    await transaction.CommitAsync();
                }

                Assert.Equal(groupId,
                    await restarted.TryGetIdAsync("writer-group", "group-a"));
                Assert.Equal("group-a",
                    await restarted.TryGetPublicIdAsync("writer-group", groupId));
            }
            finally
            {
                if (File.Exists(path))
                {
                    File.Delete(path);
                }
            }
        }

        [Fact]
        public async Task IdentityRegistryStaysStableAcrossReorderUpdateAddAndRemoveAsync()
        {
            var registry = new PubSubIdentityRegistry(new PubSubTestIdentityStore());
            var translator = new PubSubConfigurationTranslator();
            ushort firstGroupId;
            ushort secondGroupId;
            await using (var transaction = await registry.BeginAsync())
            {
                var initial = translator.Translate(
                [
                    CreateWriterGroup("group-a", "writer-a", MessageEncoding.Json),
                    CreateWriterGroup("group-b", "writer-b", MessageEncoding.Uadp)
                ], transaction);
                firstGroupId = Find(initial.Connections,
                    connection => connection.Name == "shadow-group-a").WriterGroups[0].WriterGroupId;
                secondGroupId = Find(initial.Connections,
                    connection => connection.Name == "shadow-group-b").WriterGroups[0].WriterGroupId;
                await transaction.CommitAsync();
            }

            await using (var transaction = await registry.BeginAsync())
            {
                var reorderedAndUpdated = translator.Translate(
                [
                    CreateWriterGroup("group-b", "writer-b", MessageEncoding.Json),
                    CreateWriterGroup("group-a", "writer-a", MessageEncoding.Uadp)
                ], transaction);
                Assert.Equal(firstGroupId, Find(reorderedAndUpdated.Connections,
                    connection => connection.Name == "shadow-group-a").WriterGroups[0].WriterGroupId);
                Assert.Equal(secondGroupId, Find(reorderedAndUpdated.Connections,
                    connection => connection.Name == "shadow-group-b").WriterGroups[0].WriterGroupId);
                await transaction.CommitAsync();
            }

            await using (var transaction = await registry.BeginAsync())
            {
                var removedAndAdded = translator.Translate(
                [
                    CreateWriterGroup("group-a", "writer-a", MessageEncoding.Uadp),
                    CreateWriterGroup("group-c", "writer-c", MessageEncoding.Json)
                ], transaction);
                Assert.Equal(firstGroupId, Find(removedAndAdded.Connections,
                    connection => connection.Name == "shadow-group-a").WriterGroups[0].WriterGroupId);
                Assert.NotEqual(firstGroupId, Find(removedAndAdded.Connections,
                    connection => connection.Name == "shadow-group-c").WriterGroups[0].WriterGroupId);
                await transaction.CommitAsync();
            }

            Assert.Equal(secondGroupId,
                await registry.TryGetIdAsync("writer-group", "group-b"));
        }

        [Fact]
        public async Task IdentityRegistryResolvesCollisionsWithinScopeAsync()
        {
            var registry = new PubSubIdentityRegistry(new PubSubTestIdentityStore(), _ => 0);

            await using var transaction = await registry.BeginAsync();
            var first = transaction.GetOrAllocate("writer-group", "first");
            var second = transaction.GetOrAllocate("writer-group", "second");
            await transaction.CommitAsync();

            Assert.Equal((ushort)1, first);
            Assert.Equal((ushort)2, second);
        }

        [Fact]
        public async Task FailedHotReplacementRollsBackIdentityAllocationAsync()
        {
            var application = new Mock<IPubSubApplication>(MockBehavior.Strict);
            application.Setup(instance => instance.ReplaceConfigurationAsync(
                    It.IsAny<PubSubConfigurationDataType>(),
                    It.IsAny<CancellationToken>()))
                .Returns<PubSubConfigurationDataType, CancellationToken>((_, _) =>
                    ValueTask.FromException<ArrayOf<StatusCode>>(
                        new InvalidOperationException("replacement failed")));
            var registry = new PubSubIdentityRegistry(new PubSubTestIdentityStore());
            var state = new PubSubShadowRuntimeStateProvider();
            var host = new PubSubShadowHost(registry, new PubSubConfigurationTranslator(),
                state, application.Object, PubSubConfigurationTranslator.CreateEmpty());

            await Assert.ThrowsAsync<InvalidOperationException>(async () =>
                await host.ReplaceConfigurationAsync(
                    [CreateWriterGroup("group-a", "writer-a", MessageEncoding.Json)]));

            Assert.Null(await registry.TryGetIdAsync("writer-group", "group-a"));
            Assert.Equal("replacement failed", state.State.LastError);
        }

        [Fact]
        public async Task HotReplacementCommitsTranslatedIdentityMappingAsync()
        {
            var application = new Mock<IPubSubApplication>(MockBehavior.Strict);
            application.Setup(instance => instance.ReplaceConfigurationAsync(
                    It.IsAny<PubSubConfigurationDataType>(),
                    It.IsAny<CancellationToken>()))
                .Returns(new ValueTask<ArrayOf<StatusCode>>([StatusCodes.Good]));
            var registry = new PubSubIdentityRegistry(new PubSubTestIdentityStore());
            var state = new PubSubShadowRuntimeStateProvider();
            var host = new PubSubShadowHost(registry, new PubSubConfigurationTranslator(),
                state, application.Object, PubSubConfigurationTranslator.CreateEmpty());

            await host.ReplaceConfigurationAsync(
                [CreateWriterGroup("group-a", "writer-a", MessageEncoding.Json)]);

            Assert.NotNull(await registry.TryGetIdAsync("writer-group", "group-a"));
            Assert.NotNull(await registry.TryGetIdAsync("data-set-writer", "writer-a"));
            Assert.Equal(1, state.State.ConfigurationGeneration);
            Assert.Equal(1, state.State.WriterGroupCount);
            Assert.Equal(1, state.State.DataSetWriterCount);
            application.Verify(instance => instance.ReplaceConfigurationAsync(
                It.IsAny<PubSubConfigurationDataType>(), It.IsAny<CancellationToken>()),
                Times.Once);
        }

        [Fact]
        public async Task RunningStandardHostAcceptsDisabledShadowConfigurationAsync()
        {
            var services = new ServiceCollection();
            services.AddLogging();
            services.AddIsolatedPubSubShadowHost();
            await using var provider = services.BuildServiceProvider();
            var host = provider.GetRequiredService<IPubSubShadowHost>();
            var hosted = Assert.Single(provider.GetServices<IHostedService>());
            var state = provider.GetRequiredService<IPubSubShadowRuntimeStateProvider>();

            await hosted.StartAsync(default);
            await host.ReplaceConfigurationAsync(
                [CreateWriterGroup("group-a", "writer-a", MessageEncoding.Json)]);
            await hosted.StopAsync(default);

            Assert.Equal(1, state.State.ConfigurationGeneration);
            Assert.Equal(1, state.State.StartCount);
            Assert.Equal(1, state.State.StopCount);
        }

        [Fact]
        public async Task NotificationBufferPreservesIntermediateEventsAndOwnershipAsync()
        {
            IManagedPubSubNotificationBuffer buffer = new ManagedPubSubNotificationBuffer();
            var firstPayload = new byte[] { 1 };
            await buffer.EnqueueAsync(new ManagedPubSubNotification("data", "field",
                DateTimeOffset.UnixEpoch, firstPayload));
            firstPayload[0] = 9;
            await buffer.EnqueueAsync(new ManagedPubSubNotification("data", "field",
                DateTimeOffset.UnixEpoch.AddSeconds(1), [2]));

            await using var reader = buffer.ReadAllAsync().GetAsyncEnumerator();
            Assert.True(await reader.MoveNextAsync());
            Assert.Equal(new byte[] { 1 }, reader.Current.Payload.ToArray());
            Assert.True(await reader.MoveNextAsync());
            Assert.Equal(new byte[] { 2 }, reader.Current.Payload.ToArray());
        }

        [Fact]
        public async Task BoundedCaptureSinkEvictsOldestFramesAndCountsDropsAsync()
        {
            var sink = new InMemoryPubSubShadowCaptureSink(2);
            await sink.CaptureAsync(new PubSubShadowCapture(
                PubSubShadowEncoding.Json, DateTimeOffset.UnixEpoch, [1]));
            await sink.CaptureAsync(new PubSubShadowCapture(
                PubSubShadowEncoding.Json, DateTimeOffset.UnixEpoch, [2]));
            await sink.CaptureAsync(new PubSubShadowCapture(
                PubSubShadowEncoding.Json, DateTimeOffset.UnixEpoch, [3]));

            Assert.Equal(1, sink.DroppedCaptureCount);
            Assert.Equal(new byte[] { 2 }, sink.Captures[0].Payload.ToArray());
            Assert.Equal(new byte[] { 3 }, sink.Captures[1].Payload.ToArray());
        }

        [Fact]
        public async Task ActualJsonWriterGroupUsesCompactJsonNetworkMessageAsync()
        {
            var capture = await CaptureActualWriterGroupAsync(MessageEncoding.Json);
            var payload = Encoding.UTF8.GetString(capture.Payload.Span);

            Assert.Equal(PubSubShadowEncoding.Json, capture.Encoding);
            Assert.Equal(Profiles.PubSubMqttJsonTransport, capture.TransportProfileUri);
            Assert.Null(capture.ContentEncoding);
            Assert.Contains("\"Value\":42", payload, StringComparison.Ordinal);
            Assert.DoesNotContain("\"Type\":", payload, StringComparison.Ordinal);
            Assert.IsType<JsonNetworkMessage>(await new JsonDecoder().TryDecodeAsync(
                capture.Payload, CreateContext()));
        }

        [Fact]
        public async Task ActualJsonReversibleWriterGroupUsesVerboseJsonAsync()
        {
            var compact = await CaptureActualWriterGroupAsync(MessageEncoding.Json);
            var reversible = await CaptureActualWriterGroupAsync(MessageEncoding.JsonReversible);
            var compactPayload = Encoding.UTF8.GetString(compact.Payload.Span);
            var reversiblePayload = Encoding.UTF8.GetString(reversible.Payload.Span);

            Assert.Equal(PubSubShadowEncoding.JsonReversible, reversible.Encoding);
            Assert.Equal(Profiles.PubSubMqttJsonTransport, reversible.TransportProfileUri);
            Assert.NotEqual(compactPayload, reversiblePayload);
            Assert.Contains("\"Type\":", reversiblePayload, StringComparison.Ordinal);
            Assert.Contains("\"Body\":42", reversiblePayload, StringComparison.Ordinal);
            Assert.IsType<JsonNetworkMessage>(await new JsonDecoder().TryDecodeAsync(
                reversible.Payload, CreateContext()));
        }

        [Fact]
        public async Task ActualJsonGzipWriterGroupCompressesCompactJsonAsync()
        {
            var capture = await CaptureActualWriterGroupAsync(MessageEncoding.JsonGzip);
            var payload = Decompress(capture.Payload);

            Assert.Equal(PubSubShadowEncoding.JsonGzip, capture.Encoding);
            Assert.Equal("gzip", capture.ContentEncoding);
            Assert.Equal(Profiles.PubSubMqttJsonTransport, capture.TransportProfileUri);
            Assert.Equal((byte)0x1f, capture.Payload.Span[0]);
            Assert.Equal((byte)0x8b, capture.Payload.Span[1]);
            Assert.Contains("\"Value\":42", payload, StringComparison.Ordinal);
            Assert.DoesNotContain("\"Type\":", payload, StringComparison.Ordinal);
        }

        [Fact]
        public async Task ActualJsonReversibleGzipWriterGroupCompressesVerboseJsonAsync()
        {
            var capture = await CaptureActualWriterGroupAsync(MessageEncoding.JsonReversibleGzip);
            var payload = Decompress(capture.Payload);

            Assert.Equal(PubSubShadowEncoding.JsonReversibleGzip, capture.Encoding);
            Assert.Equal("gzip", capture.ContentEncoding);
            Assert.Equal(Profiles.PubSubMqttJsonTransport, capture.TransportProfileUri);
            Assert.Equal((byte)0x1f, capture.Payload.Span[0]);
            Assert.Equal((byte)0x8b, capture.Payload.Span[1]);
            Assert.Contains("\"Type\":", payload, StringComparison.Ordinal);
            Assert.Contains("\"Body\":42", payload, StringComparison.Ordinal);
        }

        [Fact]
        public async Task ActualUadpWriterGroupUsesUadpTransportAndEncoderAsync()
        {
            var capture = await CaptureActualWriterGroupAsync(MessageEncoding.Uadp);

            Assert.Equal(PubSubShadowEncoding.Uadp, capture.Encoding);
            Assert.Null(capture.ContentEncoding);
            Assert.Equal(Profiles.PubSubUdpUadpTransport, capture.TransportProfileUri);
            Assert.IsType<UadpNetworkMessage>(await new UadpDecoder().TryDecodeAsync(
                capture.Payload, CreateContext()));
        }

        [Fact]
        public async Task TranslatorRejectsCombinedAndUnknownMessageEncodingsAsync()
        {
            var translator = new PubSubConfigurationTranslator();
            var registry = new PubSubIdentityRegistry(new PubSubTestIdentityStore());

            await using var transaction = await registry.BeginAsync();
            Assert.Throws<ArgumentException>(() => translator.Translate(
                [CreateWriterGroup("group-a", "writer-a",
                    MessageEncoding.Json | MessageEncoding.Uadp)],
                transaction));
            Assert.Throws<ArgumentException>(() => translator.Translate(
                [CreateWriterGroup("group-b", "writer-b", (MessageEncoding)0x1000)],
                transaction));
        }

        [Fact]
        public async Task ReplacementKeepsInFlightEncodingGenerationConsistentAsync()
        {
            var services = new ServiceCollection();
            services.AddLogging();
            await using var provider = services.BuildServiceProvider();
            var identities = new PubSubIdentityRegistry(new PubSubTestIdentityStore());
            var translator = new PubSubConfigurationTranslator();
            var state = new PubSubShadowRuntimeStateProvider();
            var encodings = new PubSubShadowEncodingRegistry();
            var captures = new InMemoryPubSubShadowCaptureSink();
            var observer = new BlockingEncoderObserver();
            var removed = CreateWriterGroup("removed", "removed-writer", MessageEncoding.Json);
            var retained = CreateWriterGroup("retained", "retained-writer", MessageEncoding.Json);
            removed.PublishingInterval = TimeSpan.FromDays(1);
            retained.PublishingInterval = TimeSpan.FromDays(1);

            PubSubShadowConfigurationTranslation initial;
            await using (var transaction = await identities.BeginAsync())
            {
                initial = translator.TranslateWithEncodingRegistry([removed, retained], transaction);
                await transaction.CommitAsync();
            }
            encodings.Replace(initial.Encodings);
            var initialGeneration = encodings.ActiveGeneration.Id;

            await using var application = new PubSubApplicationBuilder(
                new ServiceProviderTelemetryContext(provider))
                .WithApplicationId("pubsub-shadow-generation-test")
                .UseConfiguration(initial.Configuration)
                .AddDataSetSource("dataset-removed-writer",
                    new ValueDataSetSource("dataset-removed-writer", 42))
                .AddDataSetSource("dataset-retained-writer",
                    new ValueDataSetSource("dataset-retained-writer", 43))
                .AddTransportFactory(new NoEgressPubSubTransportFactory(
                    Profiles.PubSubMqttJsonTransport, PubSubShadowEncoding.Json,
                    captures, state, encodings))
                .AddTransportFactory(new NoEgressPubSubTransportFactory(
                    Profiles.PubSubUdpUadpTransport, PubSubShadowEncoding.Uadp,
                    captures, state))
                .AddEncoder(new ShadowJsonEncoder(encodings, observer))
                .AddEncoder(new Opc.Ua.PubSub.Encoding.Uadp.UadpEncoder())
                .AddDecoder(new JsonDecoder())
                .AddDecoder(new UadpDecoder())
                .Build();
            await using var host = new PubSubShadowHost(identities, translator, state,
                encodings, application, initial.Configuration);
            await host.StartAsync(default);
            var captureCount = captures.Captures.Count;
            var oldConnection = application.Connections.Single(connection =>
                connection.Name == "shadow-removed");
            Assert.Equal(1, oldConnection.WriterGroups.Count);
            var oldGroup = Assert.IsType<WriterGroup>(oldConnection.WriterGroups[0]);

            observer.BlockNextEncode();
            var oldPublish = oldGroup.PublishOnceAsync().AsTask();
            await observer.WaitForBlockedEncodeAsync();
            var replacement = host.ReplaceConfigurationAsync(
                [CreateWriterGroup("retained", "retained-writer",
                    MessageEncoding.JsonReversibleGzip)]).AsTask();
            await WaitForGenerationChangeAsync(encodings, initialGeneration);
            observer.ReleaseEncode();
            await oldPublish;
            await replacement;

            Assert.Equal(initialGeneration, observer.BlockedGeneration);
            Assert.NotEqual(initialGeneration, encodings.ActiveGeneration.Id);
            var oldCapture = captures.Captures
                .Skip(captureCount)
                .Single(capture => capture.Encoding == PubSubShadowEncoding.Json);
            var oldPayload = Encoding.UTF8.GetString(oldCapture.Payload.Span);
            Assert.Null(oldCapture.ContentEncoding);
            Assert.Contains("\"Value\":42", oldPayload, StringComparison.Ordinal);
            Assert.DoesNotContain("\"Type\":", oldPayload, StringComparison.Ordinal);

            var newCaptureCount = captures.Captures.Count;
            var newConnection = application.Connections.Single(connection =>
                connection.Name == "shadow-retained");
            Assert.Equal(1, newConnection.WriterGroups.Count);
            var newGroup = Assert.IsType<WriterGroup>(newConnection.WriterGroups[0]);
            await newGroup.PublishOnceAsync();

            var newCapture = Assert.Single(captures.Captures.Skip(newCaptureCount));
            var newPayload = Decompress(newCapture.Payload);
            Assert.Equal(PubSubShadowEncoding.JsonReversibleGzip, newCapture.Encoding);
            Assert.Equal("gzip", newCapture.ContentEncoding);
            Assert.Contains("\"Type\":", newPayload, StringComparison.Ordinal);
            Assert.Contains("\"Body\":43", newPayload, StringComparison.Ordinal);
            Assert.Equal(0, captures.DroppedCaptureCount);
        }

        private static async Task<PubSubShadowCapture> CaptureActualWriterGroupAsync(
            MessageEncoding encoding)
        {
            var services = new ServiceCollection();
            services.AddLogging();
            await using var provider = services.BuildServiceProvider();
            var identities = new PubSubIdentityRegistry(new PubSubTestIdentityStore());
            var translator = new PubSubConfigurationTranslator();
            var writerGroup = CreateWriterGroup("group", "writer", encoding);
            writerGroup.PublishingInterval = TimeSpan.FromDays(1);
            PubSubShadowConfigurationTranslation translation;
            await using (var transaction = await identities.BeginAsync())
            {
                translation = translator.TranslateWithEncodingRegistry([writerGroup], transaction);
                await transaction.CommitAsync();
            }

            var encodings = new PubSubShadowEncodingRegistry();
            encodings.Replace(translation.Encodings);
            var captures = new InMemoryPubSubShadowCaptureSink();
            var state = new PubSubShadowRuntimeStateProvider();
            await using var application = new PubSubApplicationBuilder(
                new ServiceProviderTelemetryContext(provider))
                .WithApplicationId("pubsub-shadow-actual-test")
                .UseConfiguration(translation.Configuration)
                .AddDataSetSource("dataset-writer", new ValueDataSetSource())
                .AddTransportFactory(new NoEgressPubSubTransportFactory(
                    Profiles.PubSubMqttJsonTransport, PubSubShadowEncoding.Json,
                    captures, state, encodings))
                .AddTransportFactory(new NoEgressPubSubTransportFactory(
                    Profiles.PubSubUdpUadpTransport, PubSubShadowEncoding.Uadp,
                    captures, state))
                .AddEncoder(new ShadowJsonEncoder(encodings))
                .AddEncoder(new Opc.Ua.PubSub.Encoding.Uadp.UadpEncoder())
                .AddDecoder(new JsonDecoder())
                .AddDecoder(new UadpDecoder())
                .Build();

            await application.StartAsync();
            var connection = Assert.Single(application.Connections);
            Assert.Equal(1, connection.WriterGroups.Count);
            var group = Assert.IsType<WriterGroup>(connection.WriterGroups[0]);
            var initialCaptureCount = captures.Captures.Count;

            await group.PublishOnceAsync();

            return Assert.Single(captures.Captures.Skip(initialCaptureCount));
        }

        private static string Decompress(ReadOnlyMemory<byte> payload)
        {
            using var input = new MemoryStream(payload.ToArray());
            using var gzip = new GZipStream(input, CompressionMode.Decompress);
            using var reader = new StreamReader(gzip, Encoding.UTF8);
            return reader.ReadToEnd();
        }

        private static async Task WaitForGenerationChangeAsync(
            PubSubShadowEncodingRegistry encodings, long initialGeneration)
        {
            for (var attempt = 0; attempt < 100; attempt++)
            {
                if (encodings.ActiveGeneration.Id != initialGeneration)
                {
                    return;
                }
                await Task.Delay(10);
            }
            throw new Xunit.Sdk.XunitException(
                "The replacement did not activate a new encoding generation.");
        }

        private static WriterGroupModel CreateWriterGroup(string groupId,
            string writerId, MessageEncoding encoding)
        {
            return new WriterGroupModel
            {
                Id = groupId,
                MessageType = encoding,
                DataSetWriters =
                [
                    new DataSetWriterModel
                    {
                        Id = writerId,
                        DataSetWriterName = writerId,
                        DataSet = new PublishedDataSetModel
                        {
                            Name = "dataset-" + writerId,
                            DataSetMetaData = new DataSetMetaDataModel
                            {
                                Name = "dataset-" + writerId,
                                DataSetClassId = Guid.Empty,
                                MajorVersion = 1
                            }
                        }
                    }
                ]
            };
        }

        private static async Task AssertMasksAsync(MessageEncoding encoding,
            NetworkMessageContentFlags? networkMask,
            DataSetMessageContentFlags? messageMask,
            DataSetFieldContentFlags? fieldMask)
        {
            var registry = new PubSubIdentityRegistry(new PubSubTestIdentityStore());
            var group = CreateWriterGroup("group", "writer", encoding);
            group.MessageSettings = new WriterGroupMessageSettingsModel
            {
                NetworkMessageContentMask = networkMask
            };
            var writer = Assert.Single(group.DataSetWriters!);
            writer.DataSetFieldContentMask = fieldMask;
            writer.MessageSettings = new DataSetWriterMessageSettingsModel
            {
                DataSetMessageContentMask = messageMask
            };
            PubSubConfigurationDataType configuration;
            await using (var transaction = await registry.BeginAsync())
            {
                configuration = new PubSubConfigurationTranslator().Translate([group], transaction);
                await transaction.CommitAsync();
            }

            var nativeGroup = Single(configuration.Connections).WriterGroups[0];
            var nativeWriter = Single(nativeGroup.DataSetWriters);
            if (encoding == MessageEncoding.Json)
            {
                Assert.True(nativeGroup.MessageSettings.TryGetValue(
                    out JsonWriterGroupMessageDataType? jsonGroup));
                Assert.True(nativeWriter.MessageSettings.TryGetValue(
                    out JsonDataSetWriterMessageDataType? jsonWriter));
                Assert.Equal(networkMask.ToStackType(encoding),
                    jsonGroup!.NetworkMessageContentMask);
                Assert.Equal(messageMask.ToStackType(fieldMask, encoding),
                    jsonWriter!.DataSetMessageContentMask);
            }
            else
            {
                Assert.True(nativeGroup.MessageSettings.TryGetValue(
                    out UadpWriterGroupMessageDataType? uadpGroup));
                Assert.True(nativeWriter.MessageSettings.TryGetValue(
                    out UadpDataSetWriterMessageDataType? uadpWriter));
                Assert.Equal(networkMask.ToStackType(encoding),
                    uadpGroup!.NetworkMessageContentMask);
                Assert.Equal(messageMask.ToStackType(fieldMask, encoding),
                    uadpWriter!.DataSetMessageContentMask);
            }
            Assert.Equal((uint)fieldMask.ToStackType(), nativeWriter.DataSetFieldContentMask);
        }

        private static T Single<T>(ArrayOf<T> values)
        {
            Assert.Equal(1, values.Count);
            return values[0];
        }

        private static T Find<T>(ArrayOf<T> values, Func<T, bool> predicate)
        {
            for (var index = 0; index < values.Count; index++)
            {
                if (predicate(values[index]))
                {
                    return values[index];
                }
            }
            throw new Xunit.Sdk.XunitException("No matching value was found.");
        }

        private static PubSubNetworkMessageContext CreateContext()
        {
            return new PubSubNetworkMessageContext(
                ServiceMessageContext.CreateEmpty(null!),
                new DataSetMetaDataRegistry(),
                new PubSubDiagnostics(PubSubDiagnosticsLevel.Low),
                TimeProvider.System);
        }

        private sealed class ValueDataSetSource : IPublishedDataSetSource
        {
            public ValueDataSetSource(string name = "dataset-writer", int value = 42)
            {
                _name = name;
                _value = value;
            }

            public DataSetMetaDataType BuildMetaData()
            {
                return new DataSetMetaDataType
                {
                    Name = _name,
                    Fields =
                    [
                        new FieldMetaData
                        {
                            Name = "Value",
                            BuiltInType = (byte)DataTypes.Int32,
                            DataType = DataTypeIds.Int32,
                            ValueRank = ValueRanks.Scalar
                        }
                    ],
                    ConfigurationVersion = new ConfigurationVersionDataType
                    {
                        MajorVersion = 1,
                        MinorVersion = 0
                    }
                };
            }

            public ValueTask<PublishedDataSetSnapshot> SampleAsync(
                DataSetMetaDataType metaData,
                CancellationToken cancellationToken = default)
            {
                cancellationToken.ThrowIfCancellationRequested();
                return new ValueTask<PublishedDataSetSnapshot>(
                    new PublishedDataSetSnapshot(metaData.ConfigurationVersion,
                        [new DataSetField
                        {
                            Name = "Value",
                            Value = new Variant(_value)
                        }],
                        DateTimeUtc.From(DateTimeOffset.UnixEpoch)));
            }

            private readonly string _name;
            private readonly int _value;
        }

        private sealed class BlockingEncoderObserver : IPubSubShadowEncodingObserver
        {
            public void BlockNextEncode()
            {
                _blocked = new TaskCompletionSource<bool>(
                    TaskCreationOptions.RunContinuationsAsynchronously);
                _release = new TaskCompletionSource<bool>(
                    TaskCreationOptions.RunContinuationsAsynchronously);
                Interlocked.Exchange(ref _blockNextEncode, 1);
            }

            public async ValueTask BeforeEncodeAsync(PubSubShadowEncodingMarker marker,
                PubSubNetworkMessage networkMessage,
                CancellationToken cancellationToken = default)
            {
                if (Interlocked.Exchange(ref _blockNextEncode, 0) == 0)
                {
                    return;
                }
                if (marker.Generation is null)
                {
                    throw new Xunit.Sdk.XunitException(
                        "The JSON encoder did not capture an encoding generation.");
                }
                BlockedGeneration = marker.Generation.Id;
                _blocked.TrySetResult(true);
                await _release.Task.WaitAsync(cancellationToken).ConfigureAwait(false);
            }

            public Task WaitForBlockedEncodeAsync()
            {
                return _blocked.Task;
            }

            public void ReleaseEncode()
            {
                _release.TrySetResult(true);
            }

            public long BlockedGeneration { get; private set; }

            private TaskCompletionSource<bool> _blocked = new(
                TaskCreationOptions.RunContinuationsAsynchronously);
            private TaskCompletionSource<bool> _release = new(
                TaskCreationOptions.RunContinuationsAsynchronously);
            private int _blockNextEncode;
        }

    }
}
