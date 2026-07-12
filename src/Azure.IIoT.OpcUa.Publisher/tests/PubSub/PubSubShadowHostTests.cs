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
    using Opc.Ua.PubSub.Diagnostics;
    using Opc.Ua.PubSub.Encoding;
    using Opc.Ua.PubSub.Encoding.Json;
    using Opc.Ua.PubSub.Encoding.Uadp;
    using Opc.Ua.PubSub.MetaData;
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Linq;
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
            services.AddPubSubShadowHost();
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
            var store = new MemoryIdentityStore();
            var registry = new PubSubIdentityRegistry(store);
            var translator = new PubSubConfigurationTranslator();
            var writerGroup = CreateWriterGroup("group-a", "writer-a", MessageEncoding.Uadp);

            PubSubConfigurationDataType configuration;
            await using (var transaction = await registry.BeginAsync())
            {
                configuration = translator.Translate([writerGroup], transaction);
                await transaction.CommitAsync();
            }

            var nativeGroup = Assert.Single(configuration.Connections).WriterGroups.Single();
            var nativeWriter = Assert.Single(nativeGroup.DataSetWriters);
            var nativeDataSet = Assert.Single(configuration.PublishedDataSets);

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
                Assert.Single(reordered.Connections).WriterGroups.Single().WriterGroupId);
            Assert.Equal(nativeWriter.DataSetWriterId,
                Assert.Single(reordered.Connections).WriterGroups.Single()
                    .DataSetWriters.Single().DataSetWriterId);
        }

        [Fact]
        public async Task TranslatorUsesPositivePublisherPublishingIntervalAndValidMasksAsync()
        {
            var options = Options.Create(new PublisherOptions
            {
                BatchTriggerInterval = TimeSpan.FromSeconds(3)
            });
            var translator = new PubSubConfigurationTranslator(options);
            var registry = new PubSubIdentityRegistry(new MemoryIdentityStore());
            var group = CreateWriterGroup("group-a", "writer-a", MessageEncoding.Uadp);
            group.PublishingInterval = null;

            PubSubConfigurationDataType configuration;
            await using (var transaction = await registry.BeginAsync())
            {
                configuration = translator.Translate([group], transaction);
                await transaction.CommitAsync();
            }

            var nativeGroup = configuration.Connections.Single().WriterGroups.Single();
            Assert.Equal(TimeSpan.FromSeconds(3).TotalMilliseconds,
                nativeGroup.PublishingInterval);
            Assert.True(nativeGroup.PublishingInterval > 0);
            new PubSubConfigurationValidator([Profiles.PubSubUdpUadpTransport])
                .Validate(configuration)
                .ThrowIfInvalid();
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
            var registry = new PubSubIdentityRegistry(new MemoryIdentityStore());
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
                firstGroupId = initial.Connections.Single(connection =>
                    connection.Name == "shadow-group-a").WriterGroups.Single().WriterGroupId;
                secondGroupId = initial.Connections.Single(connection =>
                    connection.Name == "shadow-group-b").WriterGroups.Single().WriterGroupId;
                await transaction.CommitAsync();
            }

            await using (var transaction = await registry.BeginAsync())
            {
                var reorderedAndUpdated = translator.Translate(
                [
                    CreateWriterGroup("group-b", "writer-b", MessageEncoding.Json),
                    CreateWriterGroup("group-a", "writer-a", MessageEncoding.Uadp)
                ], transaction);
                Assert.Equal(firstGroupId, reorderedAndUpdated.Connections.Single(connection =>
                    connection.Name == "shadow-group-a").WriterGroups.Single().WriterGroupId);
                Assert.Equal(secondGroupId, reorderedAndUpdated.Connections.Single(connection =>
                    connection.Name == "shadow-group-b").WriterGroups.Single().WriterGroupId);
                await transaction.CommitAsync();
            }

            await using (var transaction = await registry.BeginAsync())
            {
                var removedAndAdded = translator.Translate(
                [
                    CreateWriterGroup("group-a", "writer-a", MessageEncoding.Uadp),
                    CreateWriterGroup("group-c", "writer-c", MessageEncoding.Json)
                ], transaction);
                Assert.Equal(firstGroupId, removedAndAdded.Connections.Single(connection =>
                    connection.Name == "shadow-group-a").WriterGroups.Single().WriterGroupId);
                Assert.NotEqual(firstGroupId, removedAndAdded.Connections.Single(connection =>
                    connection.Name == "shadow-group-c").WriterGroups.Single().WriterGroupId);
                await transaction.CommitAsync();
            }

            Assert.Equal(secondGroupId,
                await registry.TryGetIdAsync("writer-group", "group-b"));
        }

        [Fact]
        public async Task IdentityRegistryResolvesCollisionsWithinScopeAsync()
        {
            var registry = new PubSubIdentityRegistry(new MemoryIdentityStore(), _ => 0);

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
            var registry = new PubSubIdentityRegistry(new MemoryIdentityStore());
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
            var registry = new PubSubIdentityRegistry(new MemoryIdentityStore());
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
            services.AddPubSubShadowHost();
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
        public async Task ShadowEncodingRoundTripsThroughNativeDecodersAsync()
        {
            var captureSink = new InMemoryPubSubShadowCaptureSink();
            var state = new PubSubShadowRuntimeStateProvider();
            var bridge = new PubSubShadowEncodingBridge(captureSink, state);
            var context = CreateContext();

            await bridge.CaptureJsonAsync(new JsonNetworkMessage
            {
                PublisherId = PublisherId.FromUInt16(12),
                DataSetMessages =
                [
                    new JsonDataSetMessage
                    {
                        DataSetWriterId = 4,
                        Fields = [new DataSetField { Value = new Variant(42) }]
                    }
                ]
            });
            await bridge.CaptureUadpAsync(new UadpNetworkMessage
            {
                ContentMask = UadpNetworkMessageContentMask.PublisherId,
                PublisherId = PublisherId.FromUInt16(12),
                DataSetMessages =
                [
                    new UadpDataSetMessage
                    {
                        DataSetWriterId = 4,
                        Fields = [new DataSetField { Value = new Variant(42) }]
                    }
                ]
            });

            var captures = captureSink.Captures;
            Assert.Equal(2, captures.Count);
            Assert.IsType<JsonNetworkMessage>(await new JsonDecoder().TryDecodeAsync(
                captures.Single(capture => capture.Encoding == PubSubShadowEncoding.Json).Payload,
                context));
            Assert.IsType<UadpNetworkMessage>(await new UadpDecoder().TryDecodeAsync(
                captures.Single(capture => capture.Encoding == PubSubShadowEncoding.Uadp).Payload,
                context));
            Assert.Equal(2, state.State.CaptureCount);
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

                            private static async Task AssertMasksAsync(MessageEncoding encoding,
                                NetworkMessageContentFlags? networkMask,
                                DataSetMessageContentFlags? messageMask,
                                DataSetFieldContentFlags? fieldMask)
                            {
                                var registry = new PubSubIdentityRegistry(new MemoryIdentityStore());
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

                                var nativeGroup = configuration.Connections.Single().WriterGroups.Single();
                                var nativeWriter = nativeGroup.DataSetWriters.Single();
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
                        }
                    }
                ]
            };
        }

        private static PubSubNetworkMessageContext CreateContext()
        {
            return new PubSubNetworkMessageContext(
                ServiceMessageContext.CreateEmpty(null!),
                new DataSetMetaDataRegistry(),
                new PubSubDiagnostics(PubSubDiagnosticsLevel.Low),
                TimeProvider.System);
        }

        private sealed class MemoryIdentityStore : IPubSubIdentityRegistryStore
        {
            public ValueTask<PubSubIdentityRegistrySnapshot> LoadAsync(
                CancellationToken cancellationToken = default)
            {
                cancellationToken.ThrowIfCancellationRequested();
                return new ValueTask<PubSubIdentityRegistrySnapshot>(Clone(_snapshot));
            }

            public ValueTask SaveAsync(PubSubIdentityRegistrySnapshot snapshot,
                CancellationToken cancellationToken = default)
            {
                cancellationToken.ThrowIfCancellationRequested();
                _snapshot = Clone(snapshot);
                return default;
            }

            private static PubSubIdentityRegistrySnapshot Clone(
                PubSubIdentityRegistrySnapshot snapshot)
            {
                return new PubSubIdentityRegistrySnapshot
                {
                    Entries = snapshot.Entries.ConvertAll(entry =>
                        new PubSubIdentityRegistryEntry
                        {
                            Scope = entry.Scope,
                            Id = entry.Id,
                            Value = entry.Value
                        })
                };
            }

            private PubSubIdentityRegistrySnapshot _snapshot = new();
        }
    }
}
