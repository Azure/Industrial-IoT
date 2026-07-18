// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

#nullable enable

namespace Azure.IIoT.OpcUa.Publisher.Tests.Compatibility
{
    using Azure.IIoT.OpcUa.Core.Logging;
    using Azure.IIoT.OpcUa.Core.Serialization;
    using Azure.IIoT.OpcUa.Publisher.Models;
    using Azure.IIoT.OpcUa.Publisher.PubSub;
    using Azure.IIoT.OpcUa.Publisher.Storage;
    using Microsoft.Extensions.Configuration;
    using Microsoft.Extensions.Options;
    using Opc.Ua;
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Linq;
    using System.Text.Json;
    using System.Text.Json.Serialization;
    using System.Threading;
    using System.Threading.Tasks;
    using Xunit;

    /// <summary>
    /// Characterizes durable 2.x inputs and the native PubSub identity sidecar
    /// without starting managed sessions or the native PubSub runtime.
    /// </summary>
    [Trait("Compatibility", "Authoritative")]
    public sealed class DurableUpgradeCompatibilityTests
    {
        [Fact]
        public async Task TwoXPublishedNodesFixtureMatchesFrozenContractAfterRestartAndReorderAsync()
        {
            var converter = CreateConverter();
            var entries = converter.Read(await ReadFixtureAsync(
                kPublishedNodesFixture)).ToList();
            AssertTwoXInputEntries(entries, expectedVersion: null);
            var reordered = entries
                .AsEnumerable()
                .Reverse()
                .Select(entry => entry with
                {
                    OpcNodes = entry.OpcNodes?.Reverse().ToList()
                })
                .ToList();

            var persisted = converter.ToPublishedNodes(30, kLastChanged,
                converter.ToWriterGroups(reordered)).ToList();
            var json = Json.SerializeToString(persisted,
                Json.GetTypeInfo<List<PublishedNodesEntryModel>>());
            var restartedConverter = CreateConverter();
            var restartedEntries = restartedConverter.Read(json).ToList();

            AssertCurrentRoundTripEntries(restartedEntries);
            AssertWriterGroupsMatchFrozenContract(
                restartedConverter.ToWriterGroups(restartedEntries));
        }

        [Fact]
        public async Task TwoXConfigurationAndCredentialFixturesMatchFrozenContractAfterReorderAsync()
        {
            var configuration = Json.Deserialize<SetConfiguredEndpointsRequestModel>(
                await ReadFixtureAsync(kConfigurationFixture));
            var credential = Json.Deserialize<CredentialModel>(
                await ReadFixtureAsync(kCredentialFixture));
            var endpoints = Assert.IsAssignableFrom<IEnumerable<PublishedNodesEntryModel>>(
                configuration?.Endpoints).ToList();
            AssertTwoXInputEntries(endpoints, expectedVersion: 29);
            Assert.Equal(CredentialType.UserName, credential?.Type);
            Assert.Equal(kFixtureUser, credential?.Value?.User);
            Assert.Equal(kFixturePassword, credential?.Value?.Password);
            var reordered = configuration! with
            {
                Endpoints = endpoints
                    .AsEnumerable()
                    .Reverse()
                    .Select(endpoint => endpoint with
                    {
                        OpcNodes = endpoint.OpcNodes?.Reverse().ToList()
                    })
                    .ToList()
            };

            var json = Json.SerializeToString(reordered,
                Json.GetTypeInfo<SetConfiguredEndpointsRequestModel>());
            var restarted = Json.Deserialize<SetConfiguredEndpointsRequestModel>(json);
            var restartedEndpoints = Assert.IsAssignableFrom<
                IEnumerable<PublishedNodesEntryModel>>(restarted?.Endpoints).ToList();
            AssertTwoXInputEntries(restartedEndpoints, expectedVersion: 29);
            var configuredCredential = CreateConverter()
                .ToWriterGroups(restartedEndpoints)
                .SelectMany(group => group.DataSetWriters ?? [])
                .Select(writer => writer.DataSet?.DataSetSource?.Connection?.User)
                .Single(user => user?.Type == CredentialType.UserName);

            Assert.Equal(credential?.Type, configuredCredential?.Type);
            Assert.Equal(credential?.Value?.User, configuredCredential?.Value?.User);
            Assert.Equal(credential?.Value?.Password,
                configuredCredential?.Value?.Password);
        }

        [Fact]
        public async Task NativeIdentityFixtureUsesPublishedNodesRuntimeKeysAsync()
        {
            var converter = CreateConverter();
            var groups = converter.ToWriterGroups(converter.Read(
                await ReadFixtureAsync(kPublishedNodesFixture))).ToList();
            AssertWriterGroupsMatchFrozenContract(groups);

            using var workspace = new TestWorkspace();
            var path = workspace.CopyFixture(kIdentityFixture,
                "pubsub-identities.json");
            var store = new FilePubSubIdentityRegistryStore(path);
            var registry = new PubSubIdentityRegistry(store);

            await using (var transaction = await registry.BeginAsync())
            {
                var native = new PubSubConfigurationTranslator().Translate(
                    groups, transaction);
                var connectionA = Find(native.Connections,
                    connection => connection.Name == "shadow-" + kWriterGroupAIdentity);
                var connectionB = Find(native.Connections,
                    connection => connection.Name == "shadow-" + kWriterGroupBIdentity);
                var groupA = Single(connectionA.WriterGroups);
                var groupB = Single(connectionB.WriterGroups);

                Assert.Equal((ushort)101, groupA.WriterGroupId);
                Assert.Equal((ushort)102, groupB.WriterGroupId);
                Assert.Equal((ushort)101,
                    Single(groupA.DataSetWriters).DataSetWriterId);
                Assert.Equal((ushort)102,
                    Single(groupB.DataSetWriters).DataSetWriterId);
                await transaction.CommitAsync();
            }

            var saved = await store.LoadAsync();
            Assert.Equal(4, saved.Entries.Count);
        }

        [Fact]
        public async Task NativeIdentityFixtureSurvivesRestartAndCanonicalizesReorderAsync()
        {
            using var workspace = new TestWorkspace();
            var path = workspace.GetPath("pubsub-identities.json");
            var snapshot = await ReadIdentityFixtureAsync();
            var fixtureKeys = snapshot.Entries
                .Select(entry => entry.Scope + "/" + entry.Id)
                .ToArray();
            Assert.Equal(fixtureKeys.Order(StringComparer.Ordinal).ToArray(),
                fixtureKeys);
            snapshot.Entries.Reverse();
            await WriteSnapshotAsync(path, snapshot);
            var store = new FilePubSubIdentityRegistryStore(path);
            var registry = new PubSubIdentityRegistry(store);

            await using (var transaction = await registry.BeginAsync())
            {
                Assert.Equal((ushort)101,
                    transaction.GetOrAllocate("writer-group", kWriterGroupAIdentity));
                Assert.NotEqual(0,
                    transaction.GetOrAllocate("data-set-writer", "added-writer"));
                await transaction.CommitAsync();
            }

            var saved = await store.LoadAsync();
            var keys = saved.Entries
                .Select(entry => entry.Scope + "/" + entry.Id)
                .ToArray();
            Assert.Equal(keys.Order(StringComparer.Ordinal).ToArray(), keys);

            var restarted = new PubSubIdentityRegistry(
                new FilePubSubIdentityRegistryStore(path));
            Assert.Equal((ushort)101,
                await restarted.TryGetIdAsync("writer-group", kWriterGroupAIdentity));
            Assert.Equal(kWriterGroupBIdentity,
                await restarted.TryGetPublicIdAsync("writer-group", 102));
        }

        [Fact]
        public async Task NativeIdentityFixtureResolvesCollisionsWithinEachScopeAsync()
        {
            using var workspace = new TestWorkspace();
            var path = workspace.CopyFixture(kIdentityFixture,
                "pubsub-identities.json");
            var registry = new PubSubIdentityRegistry(
                new FilePubSubIdentityRegistryStore(path), _ => 100);

            await using var transaction = await registry.BeginAsync();
            var group = transaction.GetOrAllocate("writer-group", "collision-group");
            var writer = transaction.GetOrAllocate("data-set-writer", "collision-writer");
            await transaction.CommitAsync();

            Assert.Equal((ushort)103, group);
            Assert.Equal((ushort)103, writer);
        }

        [Theory]
        [InlineData("{\"entries\":[")]
        [InlineData("{}")]
        [InlineData("{\"entries\":null}")]
        [InlineData("{\"entries\":[null]}")]
        [InlineData("{\"entries\":[{\"id\":\"invalid\",\"value\":101}]}")]
        [InlineData("{\"entries\":[{\"scope\":\"writer-group\",\"value\":101}]}")]
        [InlineData("{\"entries\":[{\"scope\":\"writer-group\",\"id\":\"invalid\"}]}")]
        [InlineData("{\"entries\":[{\"scope\":\"\",\"id\":\"invalid\",\"value\":101}]}")]
        [InlineData("{\"entries\":[{\"scope\":\"writer-group\",\"id\":null,\"value\":101}]}")]
        [InlineData("{\"entries\":[{\"scope\":\"writer-group\",\"id\":\"invalid\",\"value\":0}]}")]
        [InlineData("{\"entries\":[{\"scope\":\"writer-group\",\"id\":\"one\",\"value\":101}," +
            "{\"scope\":\"writer-group\",\"id\":\"one\",\"value\":102}]}")]
        [InlineData("{\"entries\":[{\"scope\":\"writer-group\",\"id\":\"one\",\"value\":101}," +
            "{\"scope\":\"writer-group\",\"id\":\"two\",\"value\":101}]}")]
        public async Task InvalidNativeIdentityPrimaryFallsBackToValidBackupAsync(
            string invalid)
        {
            using var workspace = new TestWorkspace();
            var path = workspace.GetPath("pubsub-identities.json");
            var valid = await ReadFixtureAsync(kIdentityFixture);
            await File.WriteAllTextAsync(path, invalid);
            await File.WriteAllTextAsync(path + ".bak", valid);

            var recovered = new PubSubIdentityRegistry(
                new FilePubSubIdentityRegistryStore(path));
            Assert.Equal((ushort)101,
                await recovered.TryGetIdAsync("writer-group", kWriterGroupAIdentity));
            Assert.Equal((ushort)102,
                await recovered.TryGetIdAsync("data-set-writer", kDataSetWriterBIdentity));
        }

        [Theory]
        [InlineData("{\"entries\":[{\"scope\":\"writer-group\",\"id\":\"one\"," +
            "\"value\":101}],\"futureVersion\":2}")]
        [InlineData("{\"entries\":[{\"scope\":\"writer-group\",\"id\":\"one\"," +
            "\"value\":101,\"futureMetadata\":{\"owner\":\"publisher\"}}]}")]
        public async Task AdditiveNativeIdentityFieldsAreIgnoredAsync(string extended)
        {
            using var workspace = new TestWorkspace();
            var path = workspace.GetPath("pubsub-identities.json");
            await File.WriteAllTextAsync(path, extended);
            var registry = new PubSubIdentityRegistry(
                new FilePubSubIdentityRegistryStore(path));

            Assert.Equal((ushort)101,
                await registry.TryGetIdAsync("writer-group", "one"));
        }

        [Fact]
        public async Task AllInvalidNativeIdentityCandidatesFailClosedAsync()
        {
            using var workspace = new TestWorkspace();
            var path = workspace.GetPath("pubsub-identities.json");
            await File.WriteAllTextAsync(path, "{}");
            await File.WriteAllTextAsync(path + ".bak",
                "{\"entries\":[{\"scope\":\"writer-group\",\"value\":101}]}");
            await File.WriteAllTextAsync(path + ".new",
                "{\"entries\":[{\"scope\":\"writer-group\",\"id\":\"one\",\"value\":101}," +
                "{\"scope\":\"writer-group\",\"id\":\"two\",\"value\":101}]}");

            var rejected = new PubSubIdentityRegistry(
                new FilePubSubIdentityRegistryStore(path));
            await Assert.ThrowsAsync<InvalidDataException>(async () =>
                await rejected.TryGetIdAsync("writer-group", kWriterGroupAIdentity));
        }

        [Fact]
        public async Task NativeIdentityStoreRejectsInvalidSnapshotBeforeSaveAsync()
        {
            using var workspace = new TestWorkspace();
            var store = new FilePubSubIdentityRegistryStore(
                workspace.GetPath("pubsub-identities.json"));
            var invalid = new PubSubIdentityRegistrySnapshot
            {
                Entries =
                [
                    new PubSubIdentityRegistryEntry
                    {
                        Scope = "writer-group",
                        Id = "one",
                        Value = 101
                    },
                    new PubSubIdentityRegistryEntry
                    {
                        Scope = "writer-group",
                        Id = "two",
                        Value = 101
                    }
                ]
            };

            await Assert.ThrowsAsync<InvalidDataException>(async () =>
                await store.SaveAsync(invalid));
            Assert.False(File.Exists(workspace.GetPath("pubsub-identities.json")));
        }

        [Fact]
        public async Task FailedNativeIdentityCommitRollsBackToFixtureAsync()
        {
            var store = new FailingIdentityStore(await ReadIdentityFixtureAsync());
            var registry = new PubSubIdentityRegistry(store, _ => 100);

            await using (var transaction = await registry.BeginAsync())
            {
                Assert.Equal((ushort)103,
                    transaction.GetOrAllocate("writer-group", "rolled-back-group"));
                await Assert.ThrowsAsync<IOException>(async () =>
                    await transaction.CommitAsync());
            }

            var restarted = new PubSubIdentityRegistry(store);
            Assert.Equal((ushort)101,
                await restarted.TryGetIdAsync("writer-group", kWriterGroupAIdentity));
            Assert.Null(await restarted.TryGetIdAsync(
                "writer-group", "rolled-back-group"));
        }

        [Fact]
        public async Task CurrentRoundTripsRemainReadableByTwoXDowngradeProjectionAsync()
        {
            var converter = CreateConverter();
            var entries = converter.Read(await ReadFixtureAsync(
                kPublishedNodesFixture));
            var currentEntries = converter.ToPublishedNodes(30, kLastChanged,
                converter.ToWriterGroups(entries)).ToList();
            var publishedNodesJson = Json.SerializeToString(currentEntries,
                Json.GetTypeInfo<List<PublishedNodesEntryModel>>());
            var twoXPublishedNodes = JsonSerializer.Deserialize(publishedNodesJson,
                DurableUpgradeCompatibilityJsonContext.Default.TwoXPublishedNodesEntries);

            var configuration = Json.Deserialize<SetConfiguredEndpointsRequestModel>(
                await ReadFixtureAsync(kConfigurationFixture));
            var configurationJson = Json.SerializeToString(configuration,
                Json.GetTypeInfo<SetConfiguredEndpointsRequestModel>());
            var twoXConfiguration = JsonSerializer.Deserialize(configurationJson,
                DurableUpgradeCompatibilityJsonContext.Default.TwoXConfiguration);

            var credential = Json.Deserialize<CredentialModel>(
                await ReadFixtureAsync(kCredentialFixture));
            var credentialJson = Json.SerializeToString(credential,
                Json.GetTypeInfo<CredentialModel>());
            var twoXCredential = JsonSerializer.Deserialize(credentialJson,
                DurableUpgradeCompatibilityJsonContext.Default.TwoXCredential);

            AssertTwoXPublishedNodesDowngradeContract(twoXPublishedNodes);
            AssertTwoXConfigurationDowngradeContract(twoXConfiguration);
            Assert.Equal("UserName", twoXCredential?.Type);
            Assert.Equal(kFixtureUser, twoXCredential?.Value?.User);
            Assert.Equal(kFixturePassword, twoXCredential?.Value?.Password);

            using var workspace = new TestWorkspace();
            var publishedNodesPath = workspace.CopyFixture(kPublishedNodesFixture,
                "published_nodes.json");
            var before = await File.ReadAllBytesAsync(publishedNodesPath);
            workspace.CopyFixture(kIdentityFixture,
                "pubsub-identities.json");
            var options = Options.Create(new PublisherOptions
            {
                PublishedNodesFile = publishedNodesPath
            });
            var registry = new PubSubIdentityRegistry(
                new FilePubSubIdentityRegistryStore(options), _ => 100);
            await using (var transaction = await registry.BeginAsync())
            {
                transaction.GetOrAllocate("writer-group", "upgrade-only-group");
                await transaction.CommitAsync();
            }

            Assert.Equal(before, await File.ReadAllBytesAsync(publishedNodesPath));
        }

        private static PublishedNodesConverter CreateConverter()
        {
            var options = new PublisherConfig(
                new ConfigurationBuilder().Build()).ToOptions();
            options.Value.MessagingProfile = MessagingProfile.Get(
                MessagingMode.Samples, MessageEncoding.Json);
            return new PublishedNodesConverter(
                Log.Console<PublishedNodesConverter>(), options);
        }

        private static void AssertTwoXInputEntries(
            List<PublishedNodesEntryModel> entries,
            uint? expectedVersion)
        {
            Assert.Equal(2, entries.Count);
            var entryA = Assert.Single(entries,
                entry => entry.DataSetWriterGroup == "legacy-group-a");
            Assert.Equal(expectedVersion, entryA.Version);
            Assert.Equal(expectedVersion.HasValue ? kLastChanged : null,
                entryA.LastChangeDateTime);
            Assert.Equal("legacy-writer-a", entryA.DataSetWriterId);
            Assert.Equal(kEndpointA, entryA.EndpointUrl);
            Assert.False(entryA.UseSecurity);
            Assert.Equal(OpcAuthenticationMode.Anonymous,
                entryA.OpcAuthenticationMode);
            Assert.Null(entryA.OpcAuthenticationUsername);
            Assert.Null(entryA.OpcAuthenticationPassword);
            Assert.Equal(MessagingMode.FullSamples, entryA.MessagingMode);
            Assert.Equal(MessageEncoding.Json, entryA.MessageEncoding);
            Assert.Equal((uint)4, entryA.BatchSize);
            Assert.Equal(TimeSpan.FromSeconds(2),
                entryA.BatchTriggerIntervalTimespan);
            Assert.Equal(TimeSpan.FromSeconds(1),
                entryA.DataSetPublishingIntervalTimespan);
            var nodeA = Assert.Single(entryA.OpcNodes!);
            Assert.Equal(kNodeA, nodeA.Id);
            Assert.Equal("temperature", nodeA.DisplayName);
            Assert.Equal(TimeSpan.FromMilliseconds(250),
                nodeA.OpcSamplingIntervalTimespan);
            Assert.Equal(TimeSpan.FromSeconds(30),
                nodeA.HeartbeatIntervalTimespan);
            Assert.Null(nodeA.OpcPublishingIntervalTimespan);

            var entryB = Assert.Single(entries,
                entry => entry.DataSetWriterGroup == "legacy-group-b");
            Assert.Equal(expectedVersion, entryB.Version);
            Assert.Equal(expectedVersion.HasValue ? kLastChanged : null,
                entryB.LastChangeDateTime);
            Assert.Equal("legacy-writer-b", entryB.DataSetWriterId);
            Assert.Equal(kEndpointB, entryB.EndpointUrl);
            Assert.True(entryB.UseSecurity);
            Assert.Equal(OpcAuthenticationMode.UsernamePassword,
                entryB.OpcAuthenticationMode);
            Assert.Equal(kFixtureUser, entryB.OpcAuthenticationUsername);
            Assert.Equal(kFixturePassword, entryB.OpcAuthenticationPassword);
            Assert.Equal(MessagingMode.PubSub, entryB.MessagingMode);
            Assert.Equal(MessageEncoding.Uadp, entryB.MessageEncoding);
            Assert.Equal((uint)8, entryB.BatchSize);
            Assert.Equal(TimeSpan.FromSeconds(5),
                entryB.BatchTriggerIntervalTimespan);
            Assert.Equal(TimeSpan.FromSeconds(2),
                entryB.DataSetPublishingIntervalTimespan);
            var nodeB = Assert.Single(entryB.OpcNodes!);
            Assert.Equal(kNodeB, nodeB.Id);
            Assert.Equal("pressure", nodeB.DisplayName);
            Assert.Equal(TimeSpan.FromMilliseconds(500),
                nodeB.OpcSamplingIntervalTimespan);
            Assert.Equal(TimeSpan.FromMinutes(1),
                nodeB.HeartbeatIntervalTimespan);
            Assert.Null(nodeB.OpcPublishingIntervalTimespan);
        }

        private static void AssertCurrentRoundTripEntries(
            List<PublishedNodesEntryModel> entries)
        {
            Assert.Equal(2, entries.Count);
            var entryA = Assert.Single(entries,
                entry => entry.DataSetWriterGroup == "legacy-group-a");
            Assert.Equal((uint)30, entryA.Version);
            Assert.Equal(kLastChanged, entryA.LastChangeDateTime);
            Assert.Equal("legacy-writer-a", entryA.DataSetWriterId);
            Assert.Equal(kEndpointA, entryA.EndpointUrl);
            Assert.False(entryA.UseSecurity);
            Assert.Equal(OpcAuthenticationMode.Anonymous,
                entryA.OpcAuthenticationMode);
            Assert.Null(entryA.OpcAuthenticationUsername);
            Assert.Null(entryA.OpcAuthenticationPassword);
            Assert.Equal(MessagingMode.FullSamples, entryA.MessagingMode);
            Assert.Equal(MessageEncoding.Json, entryA.MessageEncoding);
            Assert.Equal((uint)4, entryA.BatchSize);
            Assert.Equal(TimeSpan.FromSeconds(2),
                entryA.BatchTriggerIntervalTimespan);
            Assert.Null(entryA.DataSetPublishingIntervalTimespan);
            var nodeA = Assert.Single(entryA.OpcNodes!);
            Assert.Equal(kNodeA, nodeA.Id);
            Assert.Equal("temperature", nodeA.DisplayName);
            Assert.Equal(TimeSpan.FromSeconds(1),
                nodeA.OpcPublishingIntervalTimespan);
            Assert.Equal(TimeSpan.FromMilliseconds(250),
                nodeA.OpcSamplingIntervalTimespan);
            Assert.Equal(TimeSpan.FromSeconds(30),
                nodeA.HeartbeatIntervalTimespan);

            var entryB = Assert.Single(entries,
                entry => entry.DataSetWriterGroup == "legacy-group-b");
            Assert.Equal((uint)30, entryB.Version);
            Assert.Equal(kLastChanged, entryB.LastChangeDateTime);
            Assert.Equal("legacy-writer-b", entryB.DataSetWriterId);
            Assert.Equal(kEndpointB, entryB.EndpointUrl);
            Assert.True(entryB.UseSecurity);
            Assert.Equal(OpcAuthenticationMode.UsernamePassword,
                entryB.OpcAuthenticationMode);
            Assert.Equal(kFixtureUser, entryB.OpcAuthenticationUsername);
            Assert.Equal(kFixturePassword, entryB.OpcAuthenticationPassword);
            Assert.Equal(MessagingMode.PubSub, entryB.MessagingMode);
            Assert.Equal(MessageEncoding.Uadp, entryB.MessageEncoding);
            Assert.Equal((uint)8, entryB.BatchSize);
            Assert.Equal(TimeSpan.FromSeconds(5),
                entryB.BatchTriggerIntervalTimespan);
            Assert.Null(entryB.DataSetPublishingIntervalTimespan);
            var nodeB = Assert.Single(entryB.OpcNodes!);
            Assert.Equal(kNodeB, nodeB.Id);
            Assert.Equal("pressure", nodeB.DisplayName);
            Assert.Equal(TimeSpan.FromSeconds(2),
                nodeB.OpcPublishingIntervalTimespan);
            Assert.Equal(TimeSpan.FromMilliseconds(500),
                nodeB.OpcSamplingIntervalTimespan);
            Assert.Equal(TimeSpan.FromMinutes(1),
                nodeB.HeartbeatIntervalTimespan);
        }

        private static void AssertWriterGroupsMatchFrozenContract(
            IEnumerable<WriterGroupModel> writerGroups)
        {
            var groups = writerGroups.ToList();
            Assert.Equal(2, groups.Count);
            var groupA = Assert.Single(groups,
                group => group.Name == "legacy-group-a");
            Assert.Equal(kWriterGroupAIdentity, groupA.Id);
            Assert.Equal(MessageEncoding.Json, groupA.MessageType);
            Assert.Equal("FullSamples", groupA.HeaderLayoutUri);
            Assert.Equal((uint)4, groupA.NotificationPublishThreshold);
            Assert.Equal(TimeSpan.FromSeconds(2), groupA.PublishingInterval);
            var writerA = Assert.Single(groupA.DataSetWriters!);
            Assert.Equal(kDataSetWriterAIdentity, writerA.Id);
            Assert.Equal("legacy-writer-a", writerA.DataSetWriterName);
            var sourceA = writerA.DataSet!.DataSetSource!;
            Assert.Equal(TimeSpan.FromSeconds(1),
                sourceA.SubscriptionSettings?.PublishingInterval);
            Assert.Equal(kEndpointA, sourceA.Connection?.Endpoint.Url);
            Assert.Equal(SecurityMode.None,
                sourceA.Connection?.Endpoint.SecurityMode);
            Assert.Null(sourceA.Connection?.User);
            var variableA = Assert.Single(
                sourceA.PublishedVariables!.PublishedData!);
            Assert.Equal(kNodeA, variableA.PublishedVariableNodeId);
            Assert.Equal("temperature", variableA.PublishedVariableDisplayName);
            Assert.Equal(TimeSpan.FromMilliseconds(250),
                variableA.SamplingIntervalHint);
            Assert.Equal(TimeSpan.FromSeconds(30),
                variableA.HeartbeatInterval);

            var groupB = Assert.Single(groups,
                group => group.Name == "legacy-group-b");
            Assert.Equal(kWriterGroupBIdentity, groupB.Id);
            Assert.Equal(MessageEncoding.Uadp, groupB.MessageType);
            Assert.Equal("PubSub", groupB.HeaderLayoutUri);
            Assert.Equal((uint)8, groupB.NotificationPublishThreshold);
            Assert.Equal(TimeSpan.FromSeconds(5), groupB.PublishingInterval);
            var writerB = Assert.Single(groupB.DataSetWriters!);
            Assert.Equal(kDataSetWriterBIdentity, writerB.Id);
            Assert.Equal("legacy-writer-b", writerB.DataSetWriterName);
            var sourceB = writerB.DataSet!.DataSetSource!;
            Assert.Equal(TimeSpan.FromSeconds(2),
                sourceB.SubscriptionSettings?.PublishingInterval);
            Assert.Equal(kEndpointB, sourceB.Connection?.Endpoint.Url);
            Assert.Equal(SecurityMode.NotNone,
                sourceB.Connection?.Endpoint.SecurityMode);
            Assert.Equal(CredentialType.UserName, sourceB.Connection?.User?.Type);
            Assert.Equal(kFixtureUser,
                sourceB.Connection?.User?.Value?.User);
            Assert.Equal(kFixturePassword,
                sourceB.Connection?.User?.Value?.Password);
            var variableB = Assert.Single(
                sourceB.PublishedVariables!.PublishedData!);
            Assert.Equal(kNodeB, variableB.PublishedVariableNodeId);
            Assert.Equal("pressure", variableB.PublishedVariableDisplayName);
            Assert.Equal(TimeSpan.FromMilliseconds(500),
                variableB.SamplingIntervalHint);
            Assert.Equal(TimeSpan.FromMinutes(1),
                variableB.HeartbeatInterval);
        }

        private static void AssertTwoXPublishedNodesDowngradeContract(
            List<TwoXPublishedNodesEntry>? entries)
        {
            Assert.NotNull(entries);
            Assert.Equal(2, entries.Count);
            var entryA = Assert.Single(entries,
                entry => entry.DataSetWriterGroup == "legacy-group-a");
            Assert.Equal((uint)30, entryA.Version);
            Assert.Equal(kLastChanged, entryA.LastChangeDateTime);
            Assert.Equal("legacy-writer-a", entryA.DataSetWriterId);
            Assert.Equal(kEndpointA, entryA.EndpointUrl);
            Assert.False(entryA.UseSecurity);
            Assert.Equal("Anonymous", entryA.OpcAuthenticationMode);
            Assert.Null(entryA.OpcAuthenticationUsername);
            Assert.Null(entryA.OpcAuthenticationPassword);
            Assert.Equal("FullSamples", entryA.MessagingMode);
            Assert.Equal("Json", entryA.MessageEncoding);
            Assert.Equal((uint)4, entryA.BatchSize);
            Assert.Equal(TimeSpan.FromSeconds(2),
                entryA.BatchTriggerIntervalTimespan);
            Assert.Null(entryA.DataSetPublishingIntervalTimespan);
            var nodeA = Assert.Single(entryA.OpcNodes!);
            Assert.Equal(kNodeA, nodeA.Id);
            Assert.Equal("temperature", nodeA.DisplayName);
            Assert.Equal(TimeSpan.FromSeconds(1),
                nodeA.OpcPublishingIntervalTimespan);
            Assert.Equal(TimeSpan.FromMilliseconds(250),
                nodeA.OpcSamplingIntervalTimespan);
            Assert.Equal(TimeSpan.FromSeconds(30),
                nodeA.HeartbeatIntervalTimespan);

            var entryB = Assert.Single(entries,
                entry => entry.DataSetWriterGroup == "legacy-group-b");
            Assert.Equal((uint)30, entryB.Version);
            Assert.Equal(kLastChanged, entryB.LastChangeDateTime);
            Assert.Equal("legacy-writer-b", entryB.DataSetWriterId);
            Assert.Equal(kEndpointB, entryB.EndpointUrl);
            Assert.True(entryB.UseSecurity);
            Assert.Equal("UsernamePassword", entryB.OpcAuthenticationMode);
            Assert.Equal(kFixtureUser, entryB.OpcAuthenticationUsername);
            Assert.Equal(kFixturePassword, entryB.OpcAuthenticationPassword);
            Assert.Equal("PubSub", entryB.MessagingMode);
            Assert.Equal("Uadp", entryB.MessageEncoding);
            Assert.Equal((uint)8, entryB.BatchSize);
            Assert.Equal(TimeSpan.FromSeconds(5),
                entryB.BatchTriggerIntervalTimespan);
            Assert.Null(entryB.DataSetPublishingIntervalTimespan);
            var nodeB = Assert.Single(entryB.OpcNodes!);
            Assert.Equal(kNodeB, nodeB.Id);
            Assert.Equal("pressure", nodeB.DisplayName);
            Assert.Equal(TimeSpan.FromSeconds(2),
                nodeB.OpcPublishingIntervalTimespan);
            Assert.Equal(TimeSpan.FromMilliseconds(500),
                nodeB.OpcSamplingIntervalTimespan);
            Assert.Equal(TimeSpan.FromMinutes(1),
                nodeB.HeartbeatIntervalTimespan);
        }

        private static void AssertTwoXConfigurationDowngradeContract(
            TwoXConfiguration? configuration)
        {
            Assert.NotNull(configuration?.Endpoints);
            Assert.Equal(2, configuration.Endpoints.Count);
            var entryA = Assert.Single(configuration.Endpoints,
                entry => entry.DataSetWriterGroup == "legacy-group-a");
            Assert.Equal((uint)29, entryA.Version);
            Assert.Equal(kLastChanged, entryA.LastChangeDateTime);
            Assert.Equal("legacy-writer-a", entryA.DataSetWriterId);
            Assert.Equal(kEndpointA, entryA.EndpointUrl);
            Assert.False(entryA.UseSecurity);
            Assert.Equal("Anonymous", entryA.OpcAuthenticationMode);
            Assert.Null(entryA.OpcAuthenticationUsername);
            Assert.Null(entryA.OpcAuthenticationPassword);
            Assert.Equal("FullSamples", entryA.MessagingMode);
            Assert.Equal("Json", entryA.MessageEncoding);
            Assert.Equal((uint)4, entryA.BatchSize);
            Assert.Equal(TimeSpan.FromSeconds(2),
                entryA.BatchTriggerIntervalTimespan);
            Assert.Equal(TimeSpan.FromSeconds(1),
                entryA.DataSetPublishingIntervalTimespan);
            var nodeA = Assert.Single(entryA.OpcNodes!);
            Assert.Equal(kNodeA, nodeA.Id);
            Assert.Equal("temperature", nodeA.DisplayName);
            Assert.Null(nodeA.OpcPublishingIntervalTimespan);
            Assert.Equal(TimeSpan.FromMilliseconds(250),
                nodeA.OpcSamplingIntervalTimespan);
            Assert.Equal(TimeSpan.FromSeconds(30),
                nodeA.HeartbeatIntervalTimespan);

            var entryB = Assert.Single(configuration.Endpoints,
                entry => entry.DataSetWriterGroup == "legacy-group-b");
            Assert.Equal((uint)29, entryB.Version);
            Assert.Equal(kLastChanged, entryB.LastChangeDateTime);
            Assert.Equal("legacy-writer-b", entryB.DataSetWriterId);
            Assert.Equal(kEndpointB, entryB.EndpointUrl);
            Assert.True(entryB.UseSecurity);
            Assert.Equal("UsernamePassword", entryB.OpcAuthenticationMode);
            Assert.Equal(kFixtureUser, entryB.OpcAuthenticationUsername);
            Assert.Equal(kFixturePassword, entryB.OpcAuthenticationPassword);
            Assert.Equal("PubSub", entryB.MessagingMode);
            Assert.Equal("Uadp", entryB.MessageEncoding);
            Assert.Equal((uint)8, entryB.BatchSize);
            Assert.Equal(TimeSpan.FromSeconds(5),
                entryB.BatchTriggerIntervalTimespan);
            Assert.Equal(TimeSpan.FromSeconds(2),
                entryB.DataSetPublishingIntervalTimespan);
            var nodeB = Assert.Single(entryB.OpcNodes!);
            Assert.Equal(kNodeB, nodeB.Id);
            Assert.Equal("pressure", nodeB.DisplayName);
            Assert.Null(nodeB.OpcPublishingIntervalTimespan);
            Assert.Equal(TimeSpan.FromMilliseconds(500),
                nodeB.OpcSamplingIntervalTimespan);
            Assert.Equal(TimeSpan.FromMinutes(1),
                nodeB.HeartbeatIntervalTimespan);
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
            throw new Xunit.Sdk.XunitException("Expected native PubSub item was not found.");
        }

        private static async Task<string> ReadFixtureAsync(string name)
        {
            return await File.ReadAllTextAsync(GetFixturePath(name));
        }

        private static async Task<PubSubIdentityRegistrySnapshot>
            ReadIdentityFixtureAsync()
        {
            return await new FilePubSubIdentityRegistryStore(
                GetFixturePath(kIdentityFixture)).LoadAsync();
        }

        private static async Task WriteSnapshotAsync(string path,
            PubSubIdentityRegistrySnapshot snapshot)
        {
            var json = JsonSerializer.Serialize(snapshot,
                PubSubIdentityJsonContext.Default.PubSubIdentityRegistrySnapshot);
            await File.WriteAllTextAsync(path, json);
        }

        private static string GetFixturePath(string name)
        {
            return Path.Combine(AppContext.BaseDirectory, "Publisher", name);
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

        private sealed class FailingIdentityStore : IPubSubIdentityRegistryStore
        {
            public FailingIdentityStore(PubSubIdentityRegistrySnapshot snapshot)
            {
                _snapshot = Clone(snapshot);
            }

            public ValueTask<PubSubIdentityRegistrySnapshot> LoadAsync(
                CancellationToken cancellationToken = default)
            {
                cancellationToken.ThrowIfCancellationRequested();
                return new ValueTask<PubSubIdentityRegistrySnapshot>(
                    Clone(_snapshot));
            }

            public ValueTask SaveAsync(PubSubIdentityRegistrySnapshot snapshot,
                CancellationToken cancellationToken = default)
            {
                cancellationToken.ThrowIfCancellationRequested();
                return ValueTask.FromException(
                    new IOException("Simulated durable write failure."));
            }

            private readonly PubSubIdentityRegistrySnapshot _snapshot;
        }

        private sealed class TestWorkspace : IDisposable
        {
            public TestWorkspace()
            {
                _path = Path.Combine(AppContext.BaseDirectory,
                    ".durable-upgrade-" + Guid.NewGuid().ToString("N"));
                Directory.CreateDirectory(_path);
            }

            public string GetPath(string name)
            {
                return Path.Combine(_path, name);
            }

            public string CopyFixture(string fixture, string name)
            {
                var path = GetPath(name);
                File.Copy(GetFixturePath(fixture), path);
                return path;
            }

            public void Dispose()
            {
                if (Directory.Exists(_path))
                {
                    Directory.Delete(_path, recursive: true);
                }
            }

            private readonly string _path;
        }

        private static readonly DateTimeOffset kLastChanged =
            new(2025, 1, 2, 3, 4, 5, TimeSpan.Zero);
        private const string kEndpointA =
            "opc.tcp://upgrade-a.example.invalid:4840";
        private const string kEndpointB =
            "opc.tcp://upgrade-b.example.invalid:4840";
        private const string kFixtureUser = "fixture-user";
        private const string kFixturePassword = "example-only";
        private const string kNodeA =
            "nsu=urn:example:durable-upgrade;s=Temperature";
        private const string kNodeB =
            "nsu=urn:example:durable-upgrade;s=Pressure";
        private const string kWriterGroupAIdentity =
            "55afa8512376ccb9ee67e672a5ffc6bf1dd2c55e";
        private const string kWriterGroupBIdentity =
            "7cd4955d0555d76ba2de0a51a69c727ec7068f47";
        private const string kDataSetWriterAIdentity =
            "78a15b4de501a712241cc2afdbdd408f523647de_0";
        private const string kDataSetWriterBIdentity =
            "73f2e2c1bb8ed48840e3d6baa3b3af93c842b54b_0";
        private const string kPublishedNodesFixture =
            "durable_2x_published_nodes.json";
        private const string kConfigurationFixture =
            "durable_2x_configuration.json";
        private const string kCredentialFixture =
            "durable_2x_credential.json";
        private const string kIdentityFixture =
            "durable_native_pubsub_identities.json";
    }

    internal sealed class TwoXPublishedNodesEntry
    {
        public uint? Version { get; set; }
        public DateTimeOffset? LastChangeDateTime { get; set; }
        public string? DataSetWriterGroup { get; set; }
        public string? DataSetWriterId { get; set; }
        public string? EndpointUrl { get; set; }
        public bool? UseSecurity { get; set; }
        public string? OpcAuthenticationMode { get; set; }
        public string? OpcAuthenticationUsername { get; set; }
        public string? OpcAuthenticationPassword { get; set; }
        public string? MessagingMode { get; set; }
        public string? MessageEncoding { get; set; }
        public uint? BatchSize { get; set; }
        public TimeSpan? BatchTriggerIntervalTimespan { get; set; }
        public TimeSpan? DataSetPublishingIntervalTimespan { get; set; }
        public List<TwoXOpcNode>? OpcNodes { get; set; }
    }

    internal sealed class TwoXOpcNode
    {
        public string? Id { get; set; }
        public string? DisplayName { get; set; }
        public TimeSpan? OpcPublishingIntervalTimespan { get; set; }
        public TimeSpan? OpcSamplingIntervalTimespan { get; set; }
        public TimeSpan? HeartbeatIntervalTimespan { get; set; }
    }

    internal sealed class TwoXConfiguration
    {
        [JsonPropertyName("endpoints")]
        public List<TwoXPublishedNodesEntry>? Endpoints { get; set; }
    }

    internal sealed class TwoXCredential
    {
        [JsonPropertyName("type")]
        public string? Type { get; set; }

        [JsonPropertyName("value")]
        public TwoXUserIdentity? Value { get; set; }
    }

    internal sealed class TwoXUserIdentity
    {
        [JsonPropertyName("user")]
        public string? User { get; set; }

        [JsonPropertyName("password")]
        public string? Password { get; set; }
    }

    [JsonSerializable(typeof(List<TwoXPublishedNodesEntry>),
        TypeInfoPropertyName = "TwoXPublishedNodesEntries")]
    [JsonSerializable(typeof(TwoXConfiguration),
        TypeInfoPropertyName = "TwoXConfiguration")]
    [JsonSerializable(typeof(TwoXCredential),
        TypeInfoPropertyName = "TwoXCredential")]
    internal sealed partial class DurableUpgradeCompatibilityJsonContext :
        JsonSerializerContext
    {
    }
}
