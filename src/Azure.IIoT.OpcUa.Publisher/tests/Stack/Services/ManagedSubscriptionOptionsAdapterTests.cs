// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License. See LICENSE in the repo root for license information.
// ------------------------------------------------------------

namespace Azure.IIoT.OpcUa.Publisher.Stack.Services
{
    using Azure.IIoT.OpcUa.Encoders;
    using Azure.IIoT.OpcUa.Publisher.Models;
    using Azure.IIoT.OpcUa.Publisher.Stack;
    using Azure.IIoT.OpcUa.Publisher.Stack.Models;
    using Opc.Ua;
    using Opc.Ua.Client.Subscriptions.MonitoredItems;
    using System;
    using System.Linq;
    using Xunit;

    /// <summary>
    /// Tests for <see cref="ManagedSubscriptionOptionsAdapter"/> static helpers.
    /// </summary>
    public sealed class ManagedSubscriptionOptionsAdapterTests
    {
        private static IVariantEncoder CreateEncoder() =>
            new JsonVariantEncoder(new ServiceMessageContext());

        // ── ToManagedOptions(SubscriptionModel, OpcUaSubscriptionOptions) ─────

        [Fact]
        public void ToManagedOptions_NullTemplate_ThrowsArgumentNullException()
        {
            Assert.Throws<ArgumentNullException>(() =>
                ManagedSubscriptionOptionsAdapter.ToManagedOptions(
                    null!, new OpcUaSubscriptionOptions()));
        }

        [Fact]
        public void ToManagedOptions_NullOptions_ThrowsArgumentNullException()
        {
            Assert.Throws<ArgumentNullException>(() =>
                ManagedSubscriptionOptionsAdapter.ToManagedOptions(
                    new SubscriptionModel(), null!));
        }

        [Fact]
        public void ToManagedOptions_MaxPartitionsIsZero_ThrowsArgumentOutOfRangeException()
        {
            var options = new OpcUaSubscriptionOptions
            {
                MaxSubscriptionPartitions = 0
            };

            Assert.Throws<ArgumentOutOfRangeException>(() =>
                ManagedSubscriptionOptionsAdapter.ToManagedOptions(
                    new SubscriptionModel(), options));
        }

        [Fact]
        public void ToManagedOptions_DefaultTemplate_Uses1SecondPublishingInterval()
        {
            var result = ManagedSubscriptionOptionsAdapter.ToManagedOptions(
                new SubscriptionModel(), new OpcUaSubscriptionOptions());

            Assert.Equal(TimeSpan.FromSeconds(1), result.PublishingInterval);
        }

        [Fact]
        public void ToManagedOptions_ExplicitInterval_UsesItDirectly()
        {
            var template = new SubscriptionModel
            {
                PublishingInterval = TimeSpan.FromSeconds(5)
            };

            var result = ManagedSubscriptionOptionsAdapter.ToManagedOptions(
                template, new OpcUaSubscriptionOptions());

            Assert.Equal(TimeSpan.FromSeconds(5), result.PublishingInterval);
        }

        [Fact]
        public void ToManagedOptions_DefaultIntervalFromOptions_UseOptionsDefault()
        {
            var options = new OpcUaSubscriptionOptions
            {
                DefaultPublishingInterval = TimeSpan.FromSeconds(2)
            };

            var result = ManagedSubscriptionOptionsAdapter.ToManagedOptions(
                new SubscriptionModel(), options);

            Assert.Equal(TimeSpan.FromSeconds(2), result.PublishingInterval);
        }

        [Fact]
        public void ToManagedOptions_FastInterval_ZeroKeepAliveAndLifetime()
        {
            // Fast interval (<5s) → keep alive 0, lifetime 0
            var template = new SubscriptionModel
            {
                PublishingInterval = TimeSpan.FromSeconds(1)
            };

            var result = ManagedSubscriptionOptionsAdapter.ToManagedOptions(
                template, new OpcUaSubscriptionOptions());

            Assert.Equal(0u, result.KeepAliveCount);
            Assert.Equal(0u, result.LifetimeCount);
        }

        [Fact]
        public void ToManagedOptions_MediumInterval_KeepAliveTwo()
        {
            // 5s ≤ interval < 60s → keep alive 2
            var template = new SubscriptionModel
            {
                PublishingInterval = TimeSpan.FromSeconds(10)
            };

            var result = ManagedSubscriptionOptionsAdapter.ToManagedOptions(
                template, new OpcUaSubscriptionOptions());

            Assert.Equal(2u, result.KeepAliveCount);
        }

        [Fact]
        public void ToManagedOptions_SlowInterval_KeepAliveOneLifetimeTwo()
        {
            // >= 60s → keep alive 1, lifetime 2
            var template = new SubscriptionModel
            {
                PublishingInterval = TimeSpan.FromMinutes(2)
            };

            var result = ManagedSubscriptionOptionsAdapter.ToManagedOptions(
                template, new OpcUaSubscriptionOptions());

            Assert.Equal(1u, result.KeepAliveCount);
            Assert.Equal(2u, result.LifetimeCount);
        }

        [Fact]
        public void ToManagedOptions_IntervalBetween5And30Seconds_LifetimeFive()
        {
            // 5s ≤ interval < 30s → lifetime 5
            var template = new SubscriptionModel
            {
                PublishingInterval = TimeSpan.FromSeconds(10)
            };

            var result = ManagedSubscriptionOptionsAdapter.ToManagedOptions(
                template, new OpcUaSubscriptionOptions());

            Assert.Equal(5u, result.LifetimeCount);
        }

        [Fact]
        public void ToManagedOptions_ExplicitKeepAlive_OverridesDefault()
        {
            var template = new SubscriptionModel
            {
                PublishingInterval = TimeSpan.FromMinutes(2),
                KeepAliveCount = 5
            };

            var result = ManagedSubscriptionOptionsAdapter.ToManagedOptions(
                template, new OpcUaSubscriptionOptions());

            Assert.Equal(5u, result.KeepAliveCount);
        }

        [Fact]
        public void ToManagedOptions_ExplicitLifetimeCount_OverridesDefault()
        {
            var template = new SubscriptionModel
            {
                PublishingInterval = TimeSpan.FromMinutes(2),
                LifetimeCount = 10
            };

            var result = ManagedSubscriptionOptionsAdapter.ToManagedOptions(
                template, new OpcUaSubscriptionOptions());

            Assert.Equal(10u, result.LifetimeCount);
        }

        [Fact]
        public void ToManagedOptions_Priority_MappedToResult()
        {
            var template = new SubscriptionModel { Priority = 7 };

            var result = ManagedSubscriptionOptionsAdapter.ToManagedOptions(
                template, new OpcUaSubscriptionOptions());

            Assert.Equal(7, result.Priority);
        }

        [Fact]
        public void ToManagedOptions_ImmediatePublishingFromTemplate_EnablesPublishing()
        {
            var template = new SubscriptionModel { EnableImmediatePublishing = true };

            var result = ManagedSubscriptionOptionsAdapter.ToManagedOptions(
                template, new OpcUaSubscriptionOptions());

            Assert.True(result.PublishingEnabled);
        }

        [Fact]
        public void ToManagedOptions_ImmediatePublishingFromOptions_EnablesPublishing()
        {
            var options = new OpcUaSubscriptionOptions { EnableImmediatePublishing = true };

            var result = ManagedSubscriptionOptionsAdapter.ToManagedOptions(
                new SubscriptionModel(), options);

            Assert.True(result.PublishingEnabled);
        }

        [Fact]
        public void ToManagedOptions_MaxPartitionsNull_MapsToZeroUnbounded()
        {
            var options = new OpcUaSubscriptionOptions
            {
                MaxSubscriptionPartitions = null
            };

            var result = ManagedSubscriptionOptionsAdapter.ToManagedOptions(
                new SubscriptionModel(), options);

            Assert.Equal(0u, result.MaxPartitionCount);
        }

        [Fact]
        public void ToManagedOptions_MaxPartitionsNonZero_MapsValue()
        {
            var options = new OpcUaSubscriptionOptions
            {
                MaxSubscriptionPartitions = 4
            };

            var result = ManagedSubscriptionOptionsAdapter.ToManagedOptions(
                new SubscriptionModel(), options);

            Assert.Equal(4u, result.MaxPartitionCount);
        }

        [Fact]
        public void ToManagedOptions_MaxMonitoredItemsZero_MapsToNull()
        {
            var options = new OpcUaSubscriptionOptions
            {
                MaxMonitoredItemPerSubscription = 0
            };

            var result = ManagedSubscriptionOptionsAdapter.ToManagedOptions(
                new SubscriptionModel(), options);

            Assert.Null(result.MaxMonitoredItemsPerPartition);
        }

        [Fact]
        public void ToManagedOptions_MaxMonitoredItemsNonZero_MapsValue()
        {
            var options = new OpcUaSubscriptionOptions
            {
                MaxMonitoredItemPerSubscription = 1000
            };

            var result = ManagedSubscriptionOptionsAdapter.ToManagedOptions(
                new SubscriptionModel(), options);

            Assert.Equal(1000u, result.MaxMonitoredItemsPerPartition);
        }

        // ── ToManagedOptions(BaseMonitoredItemModel, ...) ─────────────────────

        [Fact]
        public void ToManagedOptionsItem_NullTemplate_ThrowsArgumentNullException()
        {
            Assert.Throws<ArgumentNullException>(() =>
                ManagedSubscriptionOptionsAdapter.ToManagedOptions(
                    null!, new OpcUaSubscriptionOptions(), CreateEncoder()));
        }

        [Fact]
        public void ToManagedOptionsItem_NullOptions_ThrowsArgumentNullException()
        {
            var template = new DataMonitoredItemModel { StartNodeId = "i=1" };

            Assert.Throws<ArgumentNullException>(() =>
                ManagedSubscriptionOptionsAdapter.ToManagedOptions(
                    template, null!, CreateEncoder()));
        }

        [Fact]
        public void ToManagedOptionsItem_NullCodec_ThrowsArgumentNullException()
        {
            var template = new DataMonitoredItemModel { StartNodeId = "i=1" };

            Assert.Throws<ArgumentNullException>(() =>
                ManagedSubscriptionOptionsAdapter.ToManagedOptions(
                    template, new OpcUaSubscriptionOptions(), null!));
        }

        [Fact]
        public void ToManagedOptionsItem_InvalidNodeId_ThrowsArgumentException()
        {
            // Empty/null StartNodeId converts to NodeId.Null → throws
            var template = new DataMonitoredItemModel { StartNodeId = string.Empty };

            Assert.Throws<ArgumentException>(() =>
                ManagedSubscriptionOptionsAdapter.ToManagedOptions(
                    template, new OpcUaSubscriptionOptions(), CreateEncoder()));
        }

        [Fact]
        public void ToManagedOptionsItem_DataItem_SetsValueAttributeAndQueueSizeOne()
        {
            var template = new DataMonitoredItemModel { StartNodeId = "i=2258" };

            var result = ManagedSubscriptionOptionsAdapter.ToManagedOptions(
                template, new OpcUaSubscriptionOptions(), CreateEncoder());

            Assert.Equal((uint)Attributes.Value, result.AttributeId);
            Assert.Equal(1u, result.QueueSize);
        }

        [Fact]
        public void ToManagedOptionsItem_DataItemWithExplicitSamplingInterval_UsesIt()
        {
            var template = new DataMonitoredItemModel
            {
                StartNodeId = "i=2258",
                SamplingInterval = TimeSpan.FromMilliseconds(500)
            };

            var result = ManagedSubscriptionOptionsAdapter.ToManagedOptions(
                template, new OpcUaSubscriptionOptions(), CreateEncoder());

            Assert.Equal(TimeSpan.FromMilliseconds(500), result.SamplingInterval);
        }

        [Fact]
        public void ToManagedOptionsItem_DataItemWithCyclicRead_SetsMonitoringModeDisabled()
        {
            var template = new DataMonitoredItemModel
            {
                StartNodeId = "i=2258",
                SamplingUsingCyclicRead = true
            };

            var result = ManagedSubscriptionOptionsAdapter.ToManagedOptions(
                template, new OpcUaSubscriptionOptions(), CreateEncoder());

            Assert.Equal(Opc.Ua.MonitoringMode.Disabled, result.MonitoringMode);
        }

        [Fact]
        public void ToManagedOptionsItem_DataItemNoDiscardNew_SetsDiscardOldestTrue()
        {
            var template = new DataMonitoredItemModel { StartNodeId = "i=2258" };

            var result = ManagedSubscriptionOptionsAdapter.ToManagedOptions(
                template, new OpcUaSubscriptionOptions(), CreateEncoder());

            // DiscardNew = false (default) → DiscardOldest = true
            Assert.True(result.DiscardOldest);
        }

        [Fact]
        public void ToManagedOptionsItem_DataItemWithDiscardNew_SetsDiscardOldestFalse()
        {
            var template = new DataMonitoredItemModel
            {
                StartNodeId = "i=2258",
                DiscardNew = true
            };

            var result = ManagedSubscriptionOptionsAdapter.ToManagedOptions(
                template, new OpcUaSubscriptionOptions(), CreateEncoder());

            Assert.False(result.DiscardOldest);
        }

        [Fact]
        public void ToManagedOptionsItem_EventItem_SetsZeroSamplingInterval()
        {
            var template = new EventMonitoredItemModel
            {
                StartNodeId = "i=2253",
                EventFilter = new EventFilterModel()
            };

            var result = ManagedSubscriptionOptionsAdapter.ToManagedOptions(
                template, new OpcUaSubscriptionOptions(), CreateEncoder());

            Assert.Equal(TimeSpan.Zero, result.SamplingInterval);
        }

        [Fact]
        public void ToManagedOptionsItem_EventItem_SetsEventNotifierAttribute()
        {
            var template = new EventMonitoredItemModel
            {
                StartNodeId = "i=2253",
                EventFilter = new EventFilterModel()
            };

            var result = ManagedSubscriptionOptionsAdapter.ToManagedOptions(
                template, new OpcUaSubscriptionOptions(), CreateEncoder());

            Assert.Equal((uint)Attributes.EventNotifier, result.AttributeId);
        }

        [Theory]
        [InlineData("i=2041")]
        [InlineData("i=2782")]
        public void ConditionFilterReusesAnEquivalentRetainClause(string typeDefinitionId)
        {
            var template = new EventMonitoredItemModel
            {
                StartNodeId = "i=2253",
                ConditionHandling = new ConditionHandlingOptionsModel { SnapshotInterval = 60 },
                EventFilter = new EventFilterModel
                {
                    SelectClauses =
                    [
                        new SimpleAttributeOperandModel
                        {
                            TypeDefinitionId = typeDefinitionId,
                            AttributeId = NodeAttribute.Value,
                            BrowsePath = [BrowseNames.Retain],
                            DisplayName = "ConfiguredRetain"
                        }
                    ]
                }
            };

            var result = ManagedSubscriptionOptionsAdapter.ToManagedOptions(
                template, new OpcUaSubscriptionOptions(), CreateEncoder());
            var filter = Assert.IsType<EventFilter>(result.Filter);

            var retain = Assert.Single(filter.SelectClauses.ToArray().Where(clause =>
                clause.BrowsePath.Count == 1 && clause.BrowsePath[0] == BrowseNames.Retain));
            Assert.Equal(typeDefinitionId, retain.TypeDefinitionId.ToString());
            Assert.Equal(3, filter.SelectClauses.Count);
        }

        [Fact]
        public void ToManagedOptionsItem_AddressSpaceItem_SetsEventNotifierAttribute()
        {
            var template = new MonitoredAddressSpaceModel
            {
                StartNodeId = "i=84"
            };

            var result = ManagedSubscriptionOptionsAdapter.ToManagedOptions(
                template, new OpcUaSubscriptionOptions(), CreateEncoder());

            Assert.Equal((uint)Attributes.EventNotifier, result.AttributeId);
        }

        [Fact]
        public void ToManagedOptionsItem_AddressSpaceItem_SetsZeroSamplingInterval()
        {
            var template = new MonitoredAddressSpaceModel
            {
                StartNodeId = "i=84"
            };

            var result = ManagedSubscriptionOptionsAdapter.ToManagedOptions(
                template, new OpcUaSubscriptionOptions(), CreateEncoder());

            Assert.Equal(TimeSpan.Zero, result.SamplingInterval);
        }

        [Fact]
        public void ToManagedOptionsItem_AffinityPropagated()
        {
            var template = new DataMonitoredItemModel { StartNodeId = "i=2258" };

            var result = ManagedSubscriptionOptionsAdapter.ToManagedOptions(
                template, new OpcUaSubscriptionOptions(), CreateEncoder(),
                affinity: "partition-a");

            Assert.Equal("partition-a", result.Affinity);
        }

        [Fact]
        public void ToManagedOptionsItem_TriggeredByNamesPropagated()
        {
            var template = new DataMonitoredItemModel { StartNodeId = "i=2258" };
            var triggers = new[] { "trigger1", "trigger2" };

            var result = ManagedSubscriptionOptionsAdapter.ToManagedOptions(
                template, new OpcUaSubscriptionOptions(), CreateEncoder(),
                triggeredByNames: triggers);

            Assert.Equal(triggers, result.TriggeredByNames);
        }
    }
}
