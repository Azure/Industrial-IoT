// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Azure.IIoT.OpcUa.Publisher.Tests.PubSub
{
    using Azure.IIoT.OpcUa.Core.Messaging;
    using Azure.IIoT.OpcUa.Publisher.Models;
    using Azure.IIoT.OpcUa.Publisher.PubSub;
    using Moq;
    using Opc.Ua;
    using System;
    using System.Collections.Generic;
    using System.Collections.ObjectModel;
    using System.Text.Json.Nodes;
    using System.Threading;
    using System.Threading.Tasks;
    using Xunit;

    /// <summary>
    /// Unit tests for <see cref="PubSubShadowEgressSettingsRegistry"/> and
    /// <see cref="PubSubShadowEgressSettings"/> covering branches not exercised
    /// by the integration-style <see cref="EventClientPubSubTransportTests"/>.
    /// </summary>
    public sealed class PubSubShadowEgressSettingsTests
    {
        // ── RequiredCapabilities property branches ─────────────────────────────

        [Fact]
        public void RequiredCapabilities_BaselineAlwaysIncludesPayloadTopicContentType()
        {
            var settings = CreateMinimalSettings();

            var caps = settings.RequiredCapabilities;

            Assert.True((caps & EventClientCapabilities.Payload) != 0);
            Assert.True((caps & EventClientCapabilities.Topic) != 0);
            Assert.True((caps & EventClientCapabilities.ContentType) != 0);
        }

        [Fact]
        public void RequiredCapabilities_AtMostOnceQoSDoesNotRequireQoSCapability()
        {
            var settings = CreateMinimalSettings() with { QualityOfService = QoS.AtMostOnce };

            var caps = settings.RequiredCapabilities;

            Assert.False((caps & EventClientCapabilities.QualityOfService) != 0);
        }

        [Fact]
        public void RequiredCapabilities_AtLeastOnceQoSRequiresQoSCapability()
        {
            var settings = CreateMinimalSettings() with { QualityOfService = QoS.AtLeastOnce };

            var caps = settings.RequiredCapabilities;

            Assert.True((caps & EventClientCapabilities.QualityOfService) != 0);
        }

        [Fact]
        public void RequiredCapabilities_RetainTrueRequiresRetainCapability()
        {
            var settings = CreateMinimalSettings() with { Retain = true };

            var caps = settings.RequiredCapabilities;

            Assert.True((caps & EventClientCapabilities.Retain) != 0);
        }

        [Fact]
        public void RequiredCapabilities_RetainFalseDoesNotRequireRetainCapability()
        {
            var settings = CreateMinimalSettings() with { Retain = false };

            var caps = settings.RequiredCapabilities;

            Assert.False((caps & EventClientCapabilities.Retain) != 0);
        }

        [Fact]
        public void RequiredCapabilities_ContentEncodingRequiresCapability()
        {
            var settings = CreateMinimalSettings() with { ContentEncoding = "gzip" };

            var caps = settings.RequiredCapabilities;

            Assert.True((caps & EventClientCapabilities.ContentEncoding) != 0);
        }

        [Fact]
        public void RequiredCapabilities_NullContentEncodingDoesNotRequireCapability()
        {
            var settings = CreateMinimalSettings() with { ContentEncoding = null };

            var caps = settings.RequiredCapabilities;

            Assert.False((caps & EventClientCapabilities.ContentEncoding) != 0);
        }

        [Fact]
        public void RequiredCapabilities_TimeToLiveRequiresCapability()
        {
            var settings = CreateMinimalSettings() with
            {
                TimeToLive = TimeSpan.FromMinutes(5)
            };

            var caps = settings.RequiredCapabilities;

            Assert.True((caps & EventClientCapabilities.TimeToLive) != 0);
        }

        [Fact]
        public void RequiredCapabilities_NullTimeToLiveDoesNotRequireCapability()
        {
            var settings = CreateMinimalSettings() with { TimeToLive = null };

            var caps = settings.RequiredCapabilities;

            Assert.False((caps & EventClientCapabilities.TimeToLive) != 0);
        }

        [Fact]
        public void RequiredCapabilities_NonEmptyPropertiesRequiresCustomPropertiesCapability()
        {
            var settings = CreateMinimalSettings() with
            {
                Properties = new ReadOnlyDictionary<string, string?>(
                    new Dictionary<string, string?> { ["key"] = "value" })
            };

            var caps = settings.RequiredCapabilities;

            Assert.True((caps & EventClientCapabilities.CustomProperties) != 0);
        }

        [Fact]
        public void RequiredCapabilities_EmptyPropertiesDoesNotRequireCapability()
        {
            var settings = CreateMinimalSettings() with
            {
                Properties = new ReadOnlyDictionary<string, string?>(
                    new Dictionary<string, string?>())
            };

            var caps = settings.RequiredCapabilities;

            Assert.False((caps & EventClientCapabilities.CustomProperties) != 0);
        }

        [Fact]
        public void RequiredCapabilities_UseCloudEventsTrueRequiresCapability()
        {
            var settings = CreateMinimalSettings() with { UseCloudEvents = true };

            var caps = settings.RequiredCapabilities;

            Assert.True((caps & EventClientCapabilities.CloudEvents) != 0);
        }

        [Fact]
        public void RequiredCapabilities_UseCloudEventsFalseDoesNotRequireCapability()
        {
            var settings = CreateMinimalSettings() with { UseCloudEvents = false };

            var caps = settings.RequiredCapabilities;

            Assert.False((caps & EventClientCapabilities.CloudEvents) != 0);
        }

        [Fact]
        public void RequiredCapabilities_NonNullSchemaRequiresCapability()
        {
            var settings = CreateMinimalSettings() with
            {
                Schema = new PubSubShadowEventSchema("group", PubSubShadowEncoding.Json)
            };

            var caps = settings.RequiredCapabilities;

            Assert.True((caps & EventClientCapabilities.Schema) != 0);
        }

        [Fact]
        public void RequiredCapabilities_NullSchemaDoesNotRequireCapability()
        {
            var settings = CreateMinimalSettings() with { Schema = null };

            var caps = settings.RequiredCapabilities;

            Assert.False((caps & EventClientCapabilities.Schema) != 0);
        }

        [Fact]
        public void RequiredCapabilities_DegradedCapabilitiesAreSubtracted()
        {
            var settings = CreateMinimalSettings() with
            {
                QualityOfService = QoS.AtLeastOnce,
                DegradedCapabilities = EventClientCapabilities.QualityOfService
            };

            var caps = settings.RequiredCapabilities;

            // QoS was requested but marked as degraded, so it is removed from required
            Assert.False((caps & EventClientCapabilities.QualityOfService) != 0);
        }

        // ── WithTransportSettings ──────────────────────────────────────────────

        [Fact]
        public void WithTransportSettings_OverridesTopicQoSRetainTtlFromPublishing()
        {
            var original = CreateMinimalSettings();
            var publishing = new PublishingQueueSettingsModel
            {
                QueueName = "override/topic",
                RequestedDeliveryGuarantee = QoS.ExactlyOnce,
                Retain = true,
                Ttl = TimeSpan.FromHours(1)
            };

            var updated = original.WithTransportSettings("override/topic", publishing, false);

            Assert.Equal("override/topic", updated.Topic);
            Assert.Equal(QoS.ExactlyOnce, updated.QualityOfService);
            Assert.True(updated.Retain);
            Assert.Equal(TimeSpan.FromHours(1), updated.TimeToLive);
        }

        [Fact]
        public void WithTransportSettings_NullPublishingFallsBackToDefaultRetainAndOriginalValues()
        {
            var original = CreateMinimalSettings() with
            {
                QualityOfService = QoS.AtMostOnce,
                TimeToLive = TimeSpan.FromMinutes(10)
            };

            var updated = original.WithTransportSettings("new/topic", null, defaultRetain: true);

            Assert.Equal("new/topic", updated.Topic);
            Assert.Equal(QoS.AtMostOnce, updated.QualityOfService);
            Assert.True(updated.Retain);
            Assert.Equal(TimeSpan.FromMinutes(10), updated.TimeToLive);
        }

        // ── PubSubShadowEgressSettingsRegistry ─────────────────────────────────

        [Fact]
        public void Registry_Replace_SnapshotReturnsReplacedEntries()
        {
            var client = Mock.Of<IEventClient>();
            var selector = new FixedEventClientSelector(client);
            var registry = new PubSubShadowEgressSettingsRegistry(selector);
            var group = CreateWriterGroup("group-a");

            registry.Replace([group], new PublisherOptions(), new PubSubShadowEgressOptions());

            var snapshot = registry.Snapshot();
            Assert.Single(snapshot);
            Assert.Contains("shadow-group-a", snapshot.Keys);
        }

        [Fact]
        public void Registry_Replace_ReplacesClearsPreviousEntries()
        {
            var client = Mock.Of<IEventClient>();
            var selector = new FixedEventClientSelector(client);
            var registry = new PubSubShadowEgressSettingsRegistry(selector);

            registry.Replace([CreateWriterGroup("old-group")], new PublisherOptions(),
                new PubSubShadowEgressOptions());
            registry.Replace([CreateWriterGroup("new-group")], new PublisherOptions(),
                new PubSubShadowEgressOptions());

            var snapshot = registry.Snapshot();
            Assert.Single(snapshot);
            Assert.Contains("shadow-new-group", snapshot.Keys);
            Assert.DoesNotContain("shadow-old-group", snapshot.Keys);
        }

        [Fact]
        public void Registry_Replace_DuplicateGroupThrowsArgumentException()
        {
            var client = Mock.Of<IEventClient>();
            var selector = new FixedEventClientSelector(client);
            var registry = new PubSubShadowEgressSettingsRegistry(selector);
            var group1 = CreateWriterGroup("same-id");
            var group2 = CreateWriterGroup("same-id");

            Assert.Throws<ArgumentException>(() =>
                registry.Replace([group1, group2], new PublisherOptions(),
                    new PubSubShadowEgressOptions()));
        }

        [Fact]
        public void Registry_Replace_NullGroupInListThrowsArgumentNullException()
        {
            var client = Mock.Of<IEventClient>();
            var selector = new FixedEventClientSelector(client);
            var registry = new PubSubShadowEgressSettingsRegistry(selector);

            Assert.Throws<ArgumentNullException>(() =>
                registry.Replace([null!], new PublisherOptions(),
                    new PubSubShadowEgressOptions()));
        }

        [Fact]
        public void Registry_Restore_ReplacesCurrentSettingsWithSnapshot()
        {
            var client = Mock.Of<IEventClient>();
            var selector = new FixedEventClientSelector(client);
            var registry = new PubSubShadowEgressSettingsRegistry(selector);

            registry.Replace([CreateWriterGroup("original")], new PublisherOptions(),
                new PubSubShadowEgressOptions());
            var savedSnapshot = registry.Snapshot();

            registry.Replace([CreateWriterGroup("replacement")], new PublisherOptions(),
                new PubSubShadowEgressOptions());
            Assert.Contains("shadow-replacement", registry.Snapshot().Keys);

            registry.Restore(savedSnapshot);

            var restored = registry.Snapshot();
            Assert.Contains("shadow-original", restored.Keys);
            Assert.DoesNotContain("shadow-replacement", restored.Keys);
        }

        [Fact]
        public void Registry_Restore_NullSnapshotThrows()
        {
            var client = Mock.Of<IEventClient>();
            var selector = new FixedEventClientSelector(client);
            var registry = new PubSubShadowEgressSettingsRegistry(selector);

            Assert.Throws<ArgumentNullException>(() => registry.Restore(null!));
        }

        [Fact]
        public void Registry_Resolve_UnknownConnectionThrowsInvalidOperationException()
        {
            var client = Mock.Of<IEventClient>();
            var selector = new FixedEventClientSelector(client);
            var registry = new PubSubShadowEgressSettingsRegistry(selector);

            var connection = new PubSubConnectionDataType
            {
                Name = "does-not-exist"
            };

            Assert.Throws<InvalidOperationException>(() => registry.Resolve(connection));
        }

        // ── CreateSettings — EnableDataSetRoutingInfo branch ──────────────────

        [Fact]
        public void CreateSettings_EnableDataSetRoutingInfo_AddsRoutingProperty()
        {
            var client = Mock.Of<IEventClient>();
            var selector = new FixedEventClientSelector(client);
            var registry = new PubSubShadowEgressSettingsRegistry(selector);
            var group = CreateWriterGroup("group-r");
            group.Name = "My Group";

            var options = new PublisherOptions { EnableDataSetRoutingInfo = true };
            registry.Replace([group], options, new PubSubShadowEgressOptions());

            var settings = Assert.Single(registry.Snapshot().Values);
            Assert.True(settings.Properties.ContainsKey(OpcUa.Constants.MessagePropertyRoutingKey),
                "Routing property should be present when EnableDataSetRoutingInfo is true");
            Assert.Equal("My Group", settings.Properties[OpcUa.Constants.MessagePropertyRoutingKey]);
        }

        [Fact]
        public void CreateSettings_DisabledDataSetRoutingInfo_DoesNotAddRoutingProperty()
        {
            var client = Mock.Of<IEventClient>();
            var selector = new FixedEventClientSelector(client);
            var registry = new PubSubShadowEgressSettingsRegistry(selector);
            var group = CreateWriterGroup("group-nr");

            var options = new PublisherOptions { EnableDataSetRoutingInfo = false };
            registry.Replace([group], options, new PubSubShadowEgressOptions());

            var settings = Assert.Single(registry.Snapshot().Values);
            Assert.False(settings.Properties.ContainsKey(OpcUa.Constants.MessagePropertyRoutingKey),
                "Routing property should not be present when EnableDataSetRoutingInfo is false");
        }

        // ── CreateSettings — writerGroup.Properties branch ────────────────────

        [Fact]
        public void CreateSettings_GroupProperties_StringValue_AddedToSettingsProperties()
        {
            var client = Mock.Of<IEventClient>();
            var selector = new FixedEventClientSelector(client);
            var registry = new PubSubShadowEgressSettingsRegistry(selector);
            var group = CreateWriterGroup("group-p");
            group.Properties = new Dictionary<string, JsonNode?>
            {
                ["my-key"] = JsonValue.Create("my-string-value")
            };

            registry.Replace([group], new PublisherOptions(), new PubSubShadowEgressOptions());

            var settings = Assert.Single(registry.Snapshot().Values);
            Assert.True(settings.Properties.ContainsKey("my-key"));
            Assert.Equal("my-string-value", settings.Properties["my-key"]);
        }

        [Fact]
        public void CreateSettings_GroupProperties_NullValue_AddedAsNullProperty()
        {
            var client = Mock.Of<IEventClient>();
            var selector = new FixedEventClientSelector(client);
            var registry = new PubSubShadowEgressSettingsRegistry(selector);
            var group = CreateWriterGroup("group-pnull");
            group.Properties = new Dictionary<string, JsonNode?>
            {
                ["null-key"] = null
            };

            registry.Replace([group], new PublisherOptions(), new PubSubShadowEgressOptions());

            var settings = Assert.Single(registry.Snapshot().Values);
            Assert.True(settings.Properties.ContainsKey("null-key"));
            Assert.Null(settings.Properties["null-key"]);
        }

        [Fact]
        public void CreateSettings_GroupProperties_NonStringJsonValue_SerializedToJson()
        {
            var client = Mock.Of<IEventClient>();
            var selector = new FixedEventClientSelector(client);
            var registry = new PubSubShadowEgressSettingsRegistry(selector);
            var group = CreateWriterGroup("group-pjson");
            group.Properties = new Dictionary<string, JsonNode?>
            {
                ["num-key"] = JsonValue.Create(42),
                ["bool-key"] = JsonValue.Create(true)
            };

            registry.Replace([group], new PublisherOptions(), new PubSubShadowEgressOptions());

            var settings = Assert.Single(registry.Snapshot().Values);
            Assert.True(settings.Properties.ContainsKey("num-key"));
            Assert.Equal("42", settings.Properties["num-key"]);
            Assert.Equal("true", settings.Properties["bool-key"]);
        }

        // ── CreateSettings — writer-specific conflicting queue settings ────────

        [Fact]
        public void CreateSettings_WriterWithConflictingQueueSettings_Throws()
        {
            var client = Mock.Of<IEventClient>();
            var selector = new FixedEventClientSelector(client);
            var registry = new PubSubShadowEgressSettingsRegistry(selector);

            var group = new WriterGroupModel
            {
                Id = "conflict-group",
                DataSetWriters =
                [
                    new DataSetWriterModel
                    {
                        Id = "writer-a",
                        Publishing = new PublishingQueueSettingsModel
                        {
                            QueueName = "topic/a",
                            RequestedDeliveryGuarantee = QoS.AtLeastOnce
                        }
                    },
                    new DataSetWriterModel
                    {
                        Id = "writer-b",
                        Publishing = new PublishingQueueSettingsModel
                        {
                            QueueName = "topic/b", // different topic → conflict
                            RequestedDeliveryGuarantee = QoS.AtLeastOnce
                        }
                    }
                ]
            };

            Assert.Throws<InvalidOperationException>(() =>
                registry.Replace([group], new PublisherOptions(),
                    new PubSubShadowEgressOptions()));
        }

        [Fact]
        public void CreateSettings_WriterWithSameQueueSettings_DoesNotThrow()
        {
            var client = Mock.Of<IEventClient>();
            var selector = new FixedEventClientSelector(client);
            var registry = new PubSubShadowEgressSettingsRegistry(selector);

            var samePublishing = new PublishingQueueSettingsModel
            {
                QueueName = "shared/topic",
                RequestedDeliveryGuarantee = QoS.AtLeastOnce,
                Retain = false,
                Ttl = null
            };
            var group = new WriterGroupModel
            {
                Id = "same-queue-group",
                DataSetWriters =
                [
                    new DataSetWriterModel { Id = "wa", Publishing = samePublishing },
                    new DataSetWriterModel { Id = "wb", Publishing = samePublishing }
                ]
            };

            var ex = Record.Exception(() =>
                registry.Replace([group], new PublisherOptions(),
                    new PubSubShadowEgressOptions()));
            Assert.Null(ex);
        }

        [Fact]
        public void CreateSettings_EmptyGroupId_ThrowsArgumentException()
        {
            var client = Mock.Of<IEventClient>();
            var selector = new FixedEventClientSelector(client);
            var registry = new PubSubShadowEgressSettingsRegistry(selector);

            var group = new WriterGroupModel { Id = string.Empty };

            Assert.Throws<ArgumentException>(() =>
                registry.Replace([group], new PublisherOptions(),
                    new PubSubShadowEgressOptions()));
        }

        // ── PubSubShadowSingleEventClientSelector ─────────────────────────────

        [Fact]
        public void SingleEventClientSelector_AlwaysReturnsSameClient()
        {
            var client = Mock.Of<IEventClient>();
            var selector = new PubSubShadowSingleEventClientSelector(client);

            using var result1 = selector.Select(new WriterGroupModel { Id = "g1" });
            using var result2 = selector.Select(new WriterGroupModel { Id = "g2" });

            Assert.Same(client, result1.EventClient);
            Assert.Same(client, result2.EventClient);
        }

        [Fact]
        public void SingleEventClientSelector_NullClient_Throws()
        {
            Assert.Throws<ArgumentNullException>(() =>
                new PubSubShadowSingleEventClientSelector(null!));
        }

        [Theory]
        [InlineData("duplicate")]
        [InlineData("invalid-settings")]
        [InlineData("selection")]
        [InlineData("canceled")]
        [InlineData("canceled-last")]
        public async Task FailedStagingReleasesEveryAcquiredLeaseAndKeepsOldSettingsAsync(
            string failureMode)
        {
            var oldClient = Mock.Of<IEventClient>();
            var oldScope = new Mock<IDisposable>(MockBehavior.Strict);
            oldScope.Setup(scope => scope.Dispose());
            var old = CreateWriterGroup("old");
            old.Publishing = new PublishingQueueSettingsModel
            {
                QueueName = "old/topic",
                RequestedDeliveryGuarantee = QoS.ExactlyOnce
            };
            var valid = CreateWriterGroup("staged");
            var last = CreateWriterGroup(failureMode == "duplicate" ? "staged" : "last");
            if (failureMode == "invalid-settings")
            {
                last.Publishing = new PublishingQueueSettingsModel { QueueName = "group/topic" };
                last.DataSetWriters![0].Publishing = new PublishingQueueSettingsModel
                {
                    QueueName = "conflicting/writer/topic"
                };
            }
            using var cts = new CancellationTokenSource();
            var failure = new InvalidOperationException("second selection failed");
            var staged = new List<(Mock<IDisposable> Scope, PubSubShadowEventClientLease Lease)>();
            var selector = new DelegateEventClientSelector(group =>
            {
                if (ReferenceEquals(group, old))
                {
                    return new PubSubShadowEventClientLease(oldClient, oldScope.Object);
                }
                if (ReferenceEquals(group, last) && failureMode == "selection")
                {
                    throw failure;
                }
                var scope = new Mock<IDisposable>(MockBehavior.Strict);
                scope.Setup(instance => instance.Dispose());
                var lease = new PubSubShadowEventClientLease(Mock.Of<IEventClient>(),
                    scope.Object);
                staged.Add((scope, lease));
                if (failureMode == "canceled"
                    || (failureMode == "canceled-last" && ReferenceEquals(group, last)))
                {
                    cts.Cancel();
                }
                return lease;
            });
            await using var registry = new PubSubShadowEgressSettingsRegistry(selector);
            await registry.ReplaceAsync([old], new PublisherOptions(),
                new PubSubShadowEgressOptions());
            var previous = Assert.Single(registry.Snapshot().Values);

            var error = await Record.ExceptionAsync(() => registry.ReplaceAsync(
                [valid, last], new PublisherOptions(), new PubSubShadowEgressOptions(),
                cts.Token).AsTask().WaitAsync(TimeSpan.FromSeconds(10)));

            switch (failureMode)
            {
                case "duplicate":
                    Assert.Equal("writerGroups", Assert.IsType<ArgumentException>(error).ParamName);
                    break;
                case "invalid-settings":
                    Assert.Contains("writer-specific egress settings",
                        Assert.IsType<InvalidOperationException>(error).Message,
                        StringComparison.Ordinal);
                    break;
                case "selection":
                    Assert.Same(failure, error);
                    break;
                default:
                    Assert.Equal(cts.Token,
                        Assert.IsAssignableFrom<OperationCanceledException>(error).CancellationToken);
                    break;
            }
            Assert.Equal(failureMode is "selection" or "canceled" ? 1 : 2, staged.Count);
            Assert.All(staged, item =>
            {
                item.Scope.Verify(scope => scope.Dispose(), Times.Once);
                Assert.Throws<InvalidOperationException>(() => item.Lease.Acquire());
            });
            Assert.Same(previous, Assert.Single(registry.Snapshot().Values));
            Assert.Same(oldClient, registry.Resolve(new PubSubConnectionDataType
            {
                Name = "shadow-old"
            }).EventClient);
            Assert.Equal("old/topic", previous.Topic);
            Assert.Equal(QoS.ExactlyOnce, previous.QualityOfService);
            oldScope.Verify(scope => scope.Dispose(), Times.Never);

            await registry.DisposeAsync();
            oldScope.Verify(scope => scope.Dispose(), Times.Once);
            Assert.All(staged, item => item.Scope.Verify(scope => scope.Dispose(), Times.Once));
        }

        [Fact]
        public async Task OwnedSnapshotPinsRollbackRootsButBorrowedAndMetadataViewsDoNotAsync()
        {
            var oldClient = Mock.Of<IEventClient>();
            var newClient = Mock.Of<IEventClient>();
            var oldScope = new Mock<IDisposable>(MockBehavior.Strict);
            oldScope.Setup(scope => scope.Dispose());
            var newScope = new Mock<IDisposable>(MockBehavior.Strict);
            newScope.Setup(scope => scope.Dispose());
            var old = CreateWriterGroup("old");
            var replacement = CreateWriterGroup("replacement");
            var selector = new DelegateEventClientSelector(group =>
                ReferenceEquals(group, old)
                    ? new PubSubShadowEventClientLease(oldClient, oldScope.Object)
                    : new PubSubShadowEventClientLease(newClient, newScope.Object));
            await using var registry = new PubSubShadowEgressSettingsRegistry(selector);
            await registry.ReplaceAsync([old], new PublisherOptions(),
                new PubSubShadowEgressOptions());
            var borrowed = Assert.Single(registry.Snapshot().Values);
            var metadata = borrowed.WithTransportSettings("old/metadata",
                new PublishingQueueSettingsModel { Retain = true }, defaultRetain: false);
            await using var saved = registry.AcquireSnapshot();
            Assert.NotSame(borrowed.ClientLease, saved.Settings["shadow-old"].ClientLease);
            Assert.Same(borrowed.ClientLease, metadata.ClientLease);
            Assert.Equal("old/metadata", metadata.Topic);
            Assert.True(metadata.Retain);

            await registry.ReplaceAsync([replacement], new PublisherOptions(),
                new PubSubShadowEgressOptions());
            oldScope.Verify(scope => scope.Dispose(), Times.Never);
            Assert.Same(newClient, Assert.Single(registry.Snapshot().Values).EventClient);

            await registry.RestoreAsync(saved.Settings);
            newScope.Verify(scope => scope.Dispose(), Times.Once);
            Assert.Same(oldClient, Assert.Single(registry.Snapshot().Values).EventClient);
            Assert.NotSame(saved.Settings["shadow-old"].ClientLease,
                registry.Snapshot()["shadow-old"].ClientLease);
            await saved.DisposeAsync();
            await saved.DisposeAsync();
            oldScope.Verify(scope => scope.Dispose(), Times.Never);

            await registry.DisposeAsync();
            await registry.DisposeAsync();
            oldScope.Verify(scope => scope.Dispose(), Times.Once);
            newScope.Verify(scope => scope.Dispose(), Times.Once);
            Assert.Empty(registry.Snapshot());
            Assert.Throws<InvalidOperationException>(() => borrowed.ClientLease!.Acquire());
            Assert.Throws<InvalidOperationException>(() => metadata.ClientLease!.Acquire());
            Assert.Collection(selector.Selected,
                group => Assert.Same(old, group),
                group => Assert.Same(replacement, group));
        }

        // ── Factory helpers ────────────────────────────────────────────────────

        private static PubSubShadowEgressSettings CreateMinimalSettings()
        {
            return new PubSubShadowEgressSettings
            {
                ConnectionName = "test-conn",
                EventClient = Mock.Of<IEventClient>(),
                Encoding = PubSubShadowEncoding.Json,
                Topic = "test/topic",
                ContentType = "application/json",
                ContentEncoding = null,
                QualityOfService = QoS.AtMostOnce,
                Retain = false,
                TimeToLive = null,
                UseCloudEvents = false,
                CloudEventSource = new Uri("urn:test:source"),
                CloudEventType = "test.type",
                CloudEventSubject = null,
                Schema = null,
                Properties = new ReadOnlyDictionary<string, string?>(
                    new Dictionary<string, string?>())
            };
        }

        private static WriterGroupModel CreateWriterGroup(string id)
        {
            return new WriterGroupModel
            {
                Id = id,
                DataSetWriters =
                [
                    new DataSetWriterModel
                    {
                        Id = id + "-writer",
                        DataSet = new PublishedDataSetModel
                        {
                            Name = id + "-data",
                            DataSetMetaData = new DataSetMetaDataModel
                            {
                                Name = id + "-data",
                                DataSetClassId = Guid.Empty
                            }
                        }
                    }
                ]
            };
        }

        private sealed class DelegateEventClientSelector(
            Func<WriterGroupModel, PubSubShadowEventClientLease> select)
            : IPubSubShadowEventClientSelector
        {
            public List<WriterGroupModel> Selected { get; } = [];

            public PubSubShadowEventClientLease Select(WriterGroupModel writerGroup)
            {
                Selected.Add(writerGroup);
                return select(writerGroup);
            }
        }

        private sealed class FixedEventClientSelector : IPubSubShadowEventClientSelector
        {
            public FixedEventClientSelector(IEventClient client)
            {
                _client = client;
            }

            public PubSubShadowEventClientLease Select(WriterGroupModel writerGroup)
            {
                return new PubSubShadowEventClientLease(_client);
            }

            private readonly IEventClient _client;
        }
    }
}
