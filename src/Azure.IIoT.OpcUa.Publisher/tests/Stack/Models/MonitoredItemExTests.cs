// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Azure.IIoT.OpcUa.Publisher.Tests.Stack.Models
{
    using Azure.IIoT.OpcUa.Publisher.Models;
    using Azure.IIoT.OpcUa.Publisher.Stack;
    using Azure.IIoT.OpcUa.Publisher.Stack.Models;
    using System;
    using System.Collections.Generic;
    using Xunit;

    public class MonitoredItemExTests
    {
        // ── MonitoredAddressSpaceModel ────────────────────────────────────────

        [Fact]
        public void SetDefaults_AddressSpaceModel_UsesDefaultRebrowsePeriod()
        {
            var options = new OpcUaSubscriptionOptions
            {
                DefaultRebrowsePeriod = TimeSpan.FromHours(2)
            };
            var item = new MonitoredAddressSpaceModel
            {
                StartNodeId = "i=84",
                RebrowsePeriod = null
            };

            var result = (MonitoredAddressSpaceModel)item.SetDefaults(options);

            Assert.Equal(TimeSpan.FromHours(2), result.RebrowsePeriod);
        }

        [Fact]
        public void SetDefaults_AddressSpaceModel_FallsBackTo12HWhenNoDefault()
        {
            var options = new OpcUaSubscriptionOptions
            {
                DefaultRebrowsePeriod = null
            };
            var item = new MonitoredAddressSpaceModel
            {
                StartNodeId = "i=84",
                RebrowsePeriod = null
            };

            var result = (MonitoredAddressSpaceModel)item.SetDefaults(options);

            Assert.Equal(TimeSpan.FromHours(12), result.RebrowsePeriod);
        }

        [Fact]
        public void SetDefaults_AddressSpaceModel_PreservesExistingRebrowsePeriod()
        {
            var options = new OpcUaSubscriptionOptions
            {
                DefaultRebrowsePeriod = TimeSpan.FromHours(2)
            };
            var item = new MonitoredAddressSpaceModel
            {
                StartNodeId = "i=84",
                RebrowsePeriod = TimeSpan.FromMinutes(30)
            };

            var result = (MonitoredAddressSpaceModel)item.SetDefaults(options);

            Assert.Equal(TimeSpan.FromMinutes(30), result.RebrowsePeriod);
        }

        [Fact]
        public void SetDefaults_AddressSpaceModel_UsesDefaultQueueSize()
        {
            var options = new OpcUaSubscriptionOptions
            {
                DefaultQueueSize = 10u
            };
            var item = new MonitoredAddressSpaceModel
            {
                StartNodeId = "i=84",
                QueueSize = null
            };

            var result = item.SetDefaults(options);

            Assert.Equal(10u, result.QueueSize);
        }

        [Fact]
        public void SetDefaults_AddressSpaceModel_QueueSizeFallsBackToZero()
        {
            var options = new OpcUaSubscriptionOptions
            {
                DefaultQueueSize = null
            };
            var item = new MonitoredAddressSpaceModel
            {
                StartNodeId = "i=84",
                QueueSize = null
            };

            var result = item.SetDefaults(options);

            Assert.Equal(0u, result.QueueSize);
        }

        [Fact]
        public void SetDefaults_AddressSpaceModel_SetsMonitoringModeDefault()
        {
            var options = new OpcUaSubscriptionOptions();
            var item = new MonitoredAddressSpaceModel
            {
                StartNodeId = "i=84",
                MonitoringMode = null
            };

            var result = item.SetDefaults(options);

            Assert.Equal(MonitoringMode.Reporting, result.MonitoringMode);
        }

        [Fact]
        public void SetDefaults_AddressSpaceModel_SetsTriggeredItemsRecursively()
        {
            var options = new OpcUaSubscriptionOptions
            {
                DefaultQueueSize = 5u
            };
            var child = new MonitoredAddressSpaceModel
            {
                StartNodeId = "i=85",
                QueueSize = null
            };
            var item = new MonitoredAddressSpaceModel
            {
                StartNodeId = "i=84",
                TriggeredItems = new List<BaseMonitoredItemModel> { child }
            };

            var result = item.SetDefaults(options);

            Assert.NotNull(result.TriggeredItems);
            Assert.Equal(5u, result.TriggeredItems![0].QueueSize);
        }

        // ── DataMonitoredItemModel ────────────────────────────────────────────

        [Fact]
        public void SetDefaults_DataMonitoredItem_UsesDefaultSamplingInterval()
        {
            var options = new OpcUaSubscriptionOptions
            {
                DefaultSamplingInterval = TimeSpan.FromSeconds(5)
            };
            var item = new DataMonitoredItemModel
            {
                StartNodeId = "i=2258",
                SamplingInterval = null
            };

            var result = (DataMonitoredItemModel)item.SetDefaults(options);

            Assert.Equal(TimeSpan.FromSeconds(5), result.SamplingInterval);
        }

        [Fact]
        public void SetDefaults_DataMonitoredItem_DefaultQueueSizeIsOne()
        {
            var options = new OpcUaSubscriptionOptions
            {
                DefaultQueueSize = null
            };
            var item = new DataMonitoredItemModel
            {
                StartNodeId = "i=2258",
                QueueSize = null
            };

            var result = (DataMonitoredItemModel)item.SetDefaults(options);

            Assert.Equal(1u, result.QueueSize);
        }

        [Fact]
        public void SetDefaults_DataMonitoredItem_UsesDefaultQueueSizeWhenSet()
        {
            var options = new OpcUaSubscriptionOptions
            {
                DefaultQueueSize = 7u
            };
            var item = new DataMonitoredItemModel
            {
                StartNodeId = "i=2258",
                QueueSize = null
            };

            var result = (DataMonitoredItemModel)item.SetDefaults(options);

            Assert.Equal(7u, result.QueueSize);
        }

        [Fact]
        public void SetDefaults_DataMonitoredItem_SetsDefaultSkipFirst()
        {
            var options = new OpcUaSubscriptionOptions
            {
                DefaultSkipFirst = true
            };
            var item = new DataMonitoredItemModel
            {
                StartNodeId = "i=2258",
                SkipFirst = null
            };

            var result = (DataMonitoredItemModel)item.SetDefaults(options);

            Assert.True(result.SkipFirst);
        }

        [Fact]
        public void SetDefaults_DataMonitoredItem_PreservesExistingSkipFirst()
        {
            var options = new OpcUaSubscriptionOptions
            {
                DefaultSkipFirst = true
            };
            var item = new DataMonitoredItemModel
            {
                StartNodeId = "i=2258",
                SkipFirst = false
            };

            var result = (DataMonitoredItemModel)item.SetDefaults(options);

            Assert.False(result.SkipFirst);
        }

        [Fact]
        public void SetDefaults_DataMonitoredItem_SetsDataChangeFilterFromOptions()
        {
            var options = new OpcUaSubscriptionOptions
            {
                DefaultDataChangeTrigger = DataChangeTriggerType.StatusValueTimestamp
            };
            var item = new DataMonitoredItemModel
            {
                StartNodeId = "i=2258",
                DataChangeFilter = null
            };

            var result = (DataMonitoredItemModel)item.SetDefaults(options);

            Assert.NotNull(result.DataChangeFilter);
            Assert.Equal(DataChangeTriggerType.StatusValueTimestamp,
                result.DataChangeFilter!.DataChangeTrigger);
        }

        [Fact]
        public void SetDefaults_DataMonitoredItem_NoDataChangeFilterWhenNoDefault()
        {
            var options = new OpcUaSubscriptionOptions
            {
                DefaultDataChangeTrigger = null
            };
            var item = new DataMonitoredItemModel
            {
                StartNodeId = "i=2258",
                DataChangeFilter = null
            };

            var result = (DataMonitoredItemModel)item.SetDefaults(options);

            Assert.Null(result.DataChangeFilter);
        }

        [Fact]
        public void SetDefaults_DataMonitoredItem_PreservesExistingDataChangeFilter()
        {
            var options = new OpcUaSubscriptionOptions
            {
                DefaultDataChangeTrigger = DataChangeTriggerType.StatusValueTimestamp
            };
            var existing = new DataChangeFilterModel
            {
                DataChangeTrigger = DataChangeTriggerType.Status
            };
            var item = new DataMonitoredItemModel
            {
                StartNodeId = "i=2258",
                DataChangeFilter = existing
            };

            var result = (DataMonitoredItemModel)item.SetDefaults(options);

            // Existing filter's trigger is preserved; default is not applied when filter present
            Assert.NotNull(result.DataChangeFilter);
            Assert.Equal(DataChangeTriggerType.Status,
                result.DataChangeFilter!.DataChangeTrigger);
        }

        [Fact]
        public void SetDefaults_DataMonitoredItem_SetsDefaultHeartbeat()
        {
            var options = new OpcUaSubscriptionOptions
            {
                DefaultHeartbeatInterval = TimeSpan.FromSeconds(60),
                DefaultHeartbeatBehavior = HeartbeatBehavior.WatchdogLKV
            };
            var item = new DataMonitoredItemModel
            {
                StartNodeId = "i=2258",
                HeartbeatInterval = null,
                HeartbeatBehavior = null
            };

            var result = (DataMonitoredItemModel)item.SetDefaults(options);

            Assert.Equal(TimeSpan.FromSeconds(60), result.HeartbeatInterval);
            Assert.Equal(HeartbeatBehavior.WatchdogLKV, result.HeartbeatBehavior);
        }

        // ── EventMonitoredItemModel ───────────────────────────────────────────

        [Fact]
        public void SetDefaults_EventMonitoredItem_DefaultQueueSizeIsZero()
        {
            var options = new OpcUaSubscriptionOptions
            {
                DefaultQueueSize = null
            };
            var item = new EventMonitoredItemModel
            {
                StartNodeId = "i=2041",
                EventFilter = new EventFilterModel(),
                QueueSize = null
            };

            var result = (EventMonitoredItemModel)item.SetDefaults(options);

            Assert.Equal(0u, result.QueueSize);
        }

        [Fact]
        public void SetDefaults_EventMonitoredItem_UsesDefaultQueueSizeWhenSet()
        {
            var options = new OpcUaSubscriptionOptions
            {
                DefaultQueueSize = 3u
            };
            var item = new EventMonitoredItemModel
            {
                StartNodeId = "i=2041",
                EventFilter = new EventFilterModel(),
                QueueSize = null
            };

            var result = (EventMonitoredItemModel)item.SetDefaults(options);

            Assert.Equal(3u, result.QueueSize);
        }

        [Fact]
        public void SetDefaults_EventMonitoredItem_SetsDiscardNew()
        {
            var options = new OpcUaSubscriptionOptions
            {
                DefaultDiscardNew = true
            };
            var item = new EventMonitoredItemModel
            {
                StartNodeId = "i=2041",
                EventFilter = new EventFilterModel(),
                DiscardNew = null
            };

            var result = (EventMonitoredItemModel)item.SetDefaults(options);

            Assert.True(result.DiscardNew);
        }

        [Fact]
        public void SetDefaults_EventMonitoredItem_SetsTriggeredItemsRecursively()
        {
            var options = new OpcUaSubscriptionOptions
            {
                DefaultQueueSize = 2u
            };
            var child = new DataMonitoredItemModel
            {
                StartNodeId = "i=2259",
                QueueSize = null
            };
            var item = new EventMonitoredItemModel
            {
                StartNodeId = "i=2041",
                EventFilter = new EventFilterModel(),
                TriggeredItems = new List<BaseMonitoredItemModel> { child }
            };

            var result = (EventMonitoredItemModel)item.SetDefaults(options);

            Assert.NotNull(result.TriggeredItems);
            Assert.Equal(2u, result.TriggeredItems![0].QueueSize);
        }

        // ── DataMonitoredItemModel – cyclic read + auto queue ─────────────────

        [Fact]
        public void SetDefaults_DataMonitoredItem_SetsSamplingUsingCyclicRead()
        {
            var options = new OpcUaSubscriptionOptions
            {
                DefaultSamplingUsingCyclicRead = true
            };
            var item = new DataMonitoredItemModel
            {
                StartNodeId = "i=2256",
                SamplingUsingCyclicRead = null
            };

            var result = (DataMonitoredItemModel)item.SetDefaults(options);

            Assert.True(result.SamplingUsingCyclicRead);
        }

        [Fact]
        public void SetDefaults_DataMonitoredItem_SetsCyclicReadMaxAge()
        {
            var maxAge = TimeSpan.FromSeconds(30);
            var options = new OpcUaSubscriptionOptions
            {
                DefaultCyclicReadMaxAge = maxAge
            };
            var item = new DataMonitoredItemModel
            {
                StartNodeId = "i=2256",
                CyclicReadMaxAge = null
            };

            var result = (DataMonitoredItemModel)item.SetDefaults(options);

            Assert.Equal(maxAge, result.CyclicReadMaxAge);
        }

        [Fact]
        public void SetDefaults_DataMonitoredItem_SetsAutoSetQueueSize()
        {
            var options = new OpcUaSubscriptionOptions
            {
                AutoSetQueueSizes = true
            };
            var item = new DataMonitoredItemModel
            {
                StartNodeId = "i=2256",
                AutoSetQueueSize = null
            };

            var result = (DataMonitoredItemModel)item.SetDefaults(options);

            Assert.True(result.AutoSetQueueSize);
        }

        [Fact]
        public void SetDefaults_DataMonitoredItem_SetsFetchDataSetFieldName()
        {
            var options = new OpcUaSubscriptionOptions
            {
                ResolveDisplayName = true
            };
            var item = new DataMonitoredItemModel
            {
                StartNodeId = "i=2256",
                FetchDataSetFieldName = null
            };

            var result = (DataMonitoredItemModel)item.SetDefaults(options);

            Assert.True(result.FetchDataSetFieldName);
        }

        [Fact]
        public void SetDefaults_DataMonitoredItem_PreservesHeartbeatBehavior()
        {
            var options = new OpcUaSubscriptionOptions
            {
                DefaultHeartbeatBehavior = HeartbeatBehavior.WatchdogLKG
            };
            var item = new DataMonitoredItemModel
            {
                StartNodeId = "i=2256",
                HeartbeatBehavior = HeartbeatBehavior.WatchdogLKV
            };

            var result = (DataMonitoredItemModel)item.SetDefaults(options);

            Assert.Equal(HeartbeatBehavior.WatchdogLKV, result.HeartbeatBehavior);
        }

        // ── AddressSpaceModel – missing branches ──────────────────────────────

        [Fact]
        public void SetDefaults_AddressSpaceModel_SetsAutoSetQueueSize()
        {
            var options = new OpcUaSubscriptionOptions
            {
                AutoSetQueueSizes = true
            };
            var item = new MonitoredAddressSpaceModel
            {
                StartNodeId = "i=84",
                AutoSetQueueSize = null
            };

            var result = (MonitoredAddressSpaceModel)item.SetDefaults(options);

            Assert.True(result.AutoSetQueueSize);
        }

        [Fact]
        public void SetDefaults_AddressSpaceModel_SetsFetchDataSetFieldName()
        {
            var options = new OpcUaSubscriptionOptions
            {
                ResolveDisplayName = true
            };
            var item = new MonitoredAddressSpaceModel
            {
                StartNodeId = "i=84",
                FetchDataSetFieldName = null
            };

            var result = (MonitoredAddressSpaceModel)item.SetDefaults(options);

            Assert.True(result.FetchDataSetFieldName);
        }

        // ── EventMonitoredItem – missing branches ─────────────────────────────

        [Fact]
        public void SetDefaults_EventMonitoredItem_SetsFetchDataSetFieldName()
        {
            var options = new OpcUaSubscriptionOptions
            {
                ResolveDisplayName = true
            };
            var item = new EventMonitoredItemModel
            {
                StartNodeId = "i=2041",
                EventFilter = new EventFilterModel(),
                FetchDataSetFieldName = null
            };

            var result = (EventMonitoredItemModel)item.SetDefaults(options);

            Assert.True(result.FetchDataSetFieldName);
        }

        [Fact]
        public void SetDefaults_EventMonitoredItem_SetsAutoSetQueueSize()
        {
            var options = new OpcUaSubscriptionOptions
            {
                AutoSetQueueSizes = true
            };
            var item = new EventMonitoredItemModel
            {
                StartNodeId = "i=2041",
                EventFilter = new EventFilterModel(),
                AutoSetQueueSize = null
            };

            var result = (EventMonitoredItemModel)item.SetDefaults(options);

            Assert.True(result.AutoSetQueueSize);
        }

        // ── DataChangeFilter defaults ─────────────────────────────────────────

        [Fact]
        public void SetDefaults_DataChangeFilter_WithExistingFilterAndNullTrigger_UsesOptionDefault()
        {
            var options = new OpcUaSubscriptionOptions
            {
                DefaultDataChangeTrigger = DataChangeTriggerType.StatusValueTimestamp
            };
            var item = new DataMonitoredItemModel
            {
                StartNodeId = "i=2256",
                DataChangeFilter = new DataChangeFilterModel
                {
                    DataChangeTrigger = null
                }
            };

            var result = (DataMonitoredItemModel)item.SetDefaults(options);

            Assert.NotNull(result.DataChangeFilter);
            Assert.Equal(DataChangeTriggerType.StatusValueTimestamp,
                result.DataChangeFilter!.DataChangeTrigger);
        }
    }
}
