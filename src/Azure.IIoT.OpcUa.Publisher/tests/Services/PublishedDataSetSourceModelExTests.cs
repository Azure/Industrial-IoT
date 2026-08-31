// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Azure.IIoT.OpcUa.Publisher.Tests.Services
{
    using Azure.IIoT.OpcUa.Publisher.Models;
    using Azure.IIoT.OpcUa.Publisher.Stack.Models;
    using Microsoft.Extensions.Options;
    using System;
    using System.Collections.Generic;
    using Xunit;

    /// <summary>
    /// Tests for <see cref="PublishedDataSetSourceModelEx.ToSubscriptionModel"/>
    /// and related conversion helpers.
    /// </summary>
    public sealed class PublishedDataSetSourceModelExTests
    {
        // ── ToSubscriptionModel ───────────────────────────────────────────────

        [Fact]
        public void ToSubscriptionModel_NullSettings_ReturnsDefaultSubscription()
        {
            PublishedDataSetSettingsModel? settings = null;

            var result = settings.ToSubscriptionModel(null, null);

            Assert.NotNull(result);
            Assert.Null(result.Priority);
            Assert.Null(result.PublishingInterval);
        }

        [Fact]
        public void ToSubscriptionModel_WithSettings_CopiesAllProperties()
        {
            var settings = new PublishedDataSetSettingsModel
            {
                Priority = 5,
                PublishingInterval = TimeSpan.FromMilliseconds(500),
                MaxNotificationsPerPublish = 100,
                LifeTimeCount = 10,
                MaxKeepAliveCount = 3,
                UseDeferredAcknoledgements = true,
                EnableImmediatePublishing = true,
                EnableSequentialPublishing = false,
                RepublishAfterTransfer = true
            };

            var result = settings.ToSubscriptionModel(null, null);

            Assert.Equal((byte)5, result.Priority);
            Assert.Equal(TimeSpan.FromMilliseconds(500), result.PublishingInterval);
            Assert.Equal((uint)100, result.MaxNotificationsPerPublish);
            Assert.Equal((uint)10, result.LifetimeCount);
            Assert.Equal((uint)3, result.KeepAliveCount);
            Assert.True(result.UseDeferredAcknoledgements);
            Assert.True(result.EnableImmediatePublishing);
            Assert.False(result.EnableSequentialPublishing);
            Assert.True(result.RepublishAfterTransfer);
        }

        [Fact]
        public void ToSubscriptionModel_IgnoreConfiguredPublishingIntervals_SetsIntervalToNull()
        {
            var settings = new PublishedDataSetSettingsModel
            {
                PublishingInterval = TimeSpan.FromSeconds(1)
            };

            var result = settings.ToSubscriptionModel(null, ignoreConfiguredPublishingIntervals: true);

            Assert.Null(result.PublishingInterval);
        }

        [Fact]
        public void ToSubscriptionModel_NotIgnoreConfiguredPublishingIntervals_KeepsInterval()
        {
            var settings = new PublishedDataSetSettingsModel
            {
                PublishingInterval = TimeSpan.FromSeconds(1)
            };

            var result = settings.ToSubscriptionModel(null, ignoreConfiguredPublishingIntervals: false);

            Assert.Equal(TimeSpan.FromSeconds(1), result.PublishingInterval);
        }

        [Fact]
        public void ToSubscriptionModel_WithFetchBrowsePathOverride_SetsResolveBrowsePathFromRoot()
        {
            PublishedDataSetSettingsModel? settings = null;

            var result = settings.ToSubscriptionModel(true, null);

            Assert.True(result.ResolveBrowsePathFromRoot);
        }

        [Fact]
        public void ToSubscriptionModel_WithFalseOverride_SetsResolveBrowsePathFromRootFalse()
        {
            PublishedDataSetSettingsModel? settings = null;

            var result = settings.ToSubscriptionModel(false, null);

            Assert.False(result.ResolveBrowsePathFromRoot);
        }

        // ── ToMonitoredItems (via PublishedDataSetSourceModel) ────────────────

        [Fact]
        public void ToMonitoredItems_NullVariablesAndEvents_ReturnsEmpty()
        {
            var source = new PublishedDataSetSourceModel
            {
                PublishedVariables = null,
                PublishedEvents = null
            };

            var result = source.ToMonitoredItems(NamespaceFormat.Uri);

            Assert.Empty(result);
        }

        [Fact]
        public void ToMonitoredItems_WithVariableData_ReturnsDataMonitoredItems()
        {
            var source = new PublishedDataSetSourceModel
            {
                PublishedVariables = new PublishedDataItemsModel
                {
                    PublishedData =
                    [
                        new PublishedDataSetVariableModel
                        {
                            Id = "field1",
                            PublishedVariableNodeId = "ns=1;i=1000"
                        }
                    ]
                }
            };

            var result = source.ToMonitoredItems(NamespaceFormat.Uri);

            Assert.Single(result);
            Assert.IsType<DataMonitoredItemModel>(result[0]);
        }

        [Fact]
        public void ToMonitoredItems_WithEventData_ReturnsEventMonitoredItems()
        {
            var source = new PublishedDataSetSourceModel
            {
                PublishedEvents = new PublishedEventItemsModel
                {
                    PublishedData =
                    [
                        new PublishedDataSetEventModel
                        {
                            Id = "event1",
                            EventNotifier = "i=2253"
                        }
                    ]
                }
            };

            var result = source.ToMonitoredItems(NamespaceFormat.Uri);

            Assert.Single(result);
        }

        [Fact]
        public void ToMonitoredItems_WithBothVariableAndEventData_ReturnsCombinedItems()
        {
            var source = new PublishedDataSetSourceModel
            {
                PublishedVariables = new PublishedDataItemsModel
                {
                    PublishedData =
                    [
                        new PublishedDataSetVariableModel
                        {
                            Id = "field1",
                            PublishedVariableNodeId = "ns=1;i=1000"
                        }
                    ]
                },
                PublishedEvents = new PublishedEventItemsModel
                {
                    PublishedData =
                    [
                        new PublishedDataSetEventModel
                        {
                            Id = "event1",
                            EventNotifier = "i=2253"
                        }
                    ]
                }
            };

            var result = source.ToMonitoredItems(NamespaceFormat.Uri);

            Assert.Equal(2, result.Count);
        }

        [Fact]
        public void ToMonitoredItems_EmptyPublishedData_ReturnsEmpty()
        {
            var source = new PublishedDataSetSourceModel
            {
                PublishedVariables = new PublishedDataItemsModel
                {
                    PublishedData = []
                }
            };

            var result = source.ToMonitoredItems(NamespaceFormat.Uri);

            Assert.Empty(result);
        }

        // ── ToMonitoredItemTemplate (variable) ────────────────────────────────

        [Fact]
        public void ToMonitoredItemTemplate_Variable_WithNullNodeId_ReturnsNull()
        {
            var variable = new PublishedDataSetVariableModel
            {
                Id = "field",
                PublishedVariableNodeId = null  // empty/null → null
            };

            var result = variable.ToMonitoredItemTemplate(null, NamespaceFormat.Uri);

            Assert.Null(result);
        }

        [Fact]
        public void ToMonitoredItemTemplate_Variable_WithEmptyNodeId_ReturnsNull()
        {
            var variable = new PublishedDataSetVariableModel
            {
                Id = "field",
                PublishedVariableNodeId = ""
            };

            var result = variable.ToMonitoredItemTemplate(null, NamespaceFormat.Uri);

            Assert.Null(result);
        }

        [Fact]
        public void ToMonitoredItemTemplate_Variable_SetsStartNodeId()
        {
            var variable = new PublishedDataSetVariableModel
            {
                Id = "field1",
                PublishedVariableNodeId = "ns=2;s=MyNode"
            };

            var result = variable.ToMonitoredItemTemplate(null, NamespaceFormat.Uri);

            Assert.NotNull(result);
            Assert.Equal("ns=2;s=MyNode", result.StartNodeId);
        }

        [Fact]
        public void ToMonitoredItemTemplate_Variable_UsesIdAsDataSetFieldId()
        {
            var variable = new PublishedDataSetVariableModel
            {
                Id = "my-field",
                PublishedVariableNodeId = "ns=2;s=MyNode"
            };

            var result = variable.ToMonitoredItemTemplate(null, NamespaceFormat.Uri);

            Assert.NotNull(result);
            Assert.Equal("my-field", result.DataSetFieldId);
        }

        [Fact]
        public void ToMonitoredItemTemplate_Variable_WhenIdNullFallsBackToNodeId()
        {
            var variable = new PublishedDataSetVariableModel
            {
                Id = null,
                PublishedVariableNodeId = "ns=2;s=MyNode"
            };

            var result = variable.ToMonitoredItemTemplate(null, NamespaceFormat.Uri);

            Assert.NotNull(result);
            Assert.Equal("ns=2;s=MyNode", result.DataSetFieldId);
        }

        [Fact]
        public void ToMonitoredItemTemplate_Variable_WithDataChangeTrigger_SetsFilter()
        {
            var variable = new PublishedDataSetVariableModel
            {
                PublishedVariableNodeId = "ns=2;s=MyNode",
                DataChangeTrigger = DataChangeTriggerType.StatusValueTimestamp
            };

            var result = variable.ToMonitoredItemTemplate(null, NamespaceFormat.Uri);

            Assert.NotNull(result);
            Assert.NotNull(result.DataChangeFilter);
            Assert.Equal(DataChangeTriggerType.StatusValueTimestamp,
                result.DataChangeFilter!.DataChangeTrigger);
        }

        [Fact]
        public void ToMonitoredItemTemplate_Variable_WithDeadbandValue_SetsFilter()
        {
            var variable = new PublishedDataSetVariableModel
            {
                PublishedVariableNodeId = "ns=2;s=MyNode",
                DeadbandValue = 1.5,
                DeadbandType = DeadbandType.Absolute
            };

            var result = variable.ToMonitoredItemTemplate(null, NamespaceFormat.Uri);

            Assert.NotNull(result);
            Assert.NotNull(result.DataChangeFilter);
            Assert.Equal(1.5, result.DataChangeFilter!.DeadbandValue);
            Assert.Equal(DeadbandType.Absolute, result.DataChangeFilter.DeadbandType);
        }

        [Fact]
        public void ToMonitoredItemTemplate_Variable_NoFilterFields_FilterIsNull()
        {
            var variable = new PublishedDataSetVariableModel
            {
                PublishedVariableNodeId = "ns=2;s=MyNode"
            };

            var result = variable.ToMonitoredItemTemplate(null, NamespaceFormat.Uri);

            Assert.NotNull(result);
            Assert.Null(result.DataChangeFilter);
        }

        [Fact]
        public void ToMonitoredItemTemplate_Variable_WithTriggering_SetsTriggeredItems()
        {
            var variable = new PublishedDataSetVariableModel
            {
                PublishedVariableNodeId = "ns=2;s=MyNode",
                Triggering = new PublishedDataSetTriggerModel
                {
                    PublishedVariables = new PublishedDataItemsModel
                    {
                        PublishedData = [new PublishedDataSetVariableModel
                        {
                            PublishedVariableNodeId = "ns=2;s=TriggerTarget"
                        }]
                    }
                }
            };

            var result = variable.ToMonitoredItemTemplate(null, NamespaceFormat.Uri,
                includeTriggering: true);

            Assert.NotNull(result);
            Assert.NotNull(result.TriggeredItems);
            Assert.Single(result.TriggeredItems!);
        }

        [Fact]
        public void ToMonitoredItemTemplate_Variable_IncludeTriggeringFalse_NoTriggeredItems()
        {
            var variable = new PublishedDataSetVariableModel
            {
                PublishedVariableNodeId = "ns=2;s=MyNode",
                Triggering = new PublishedDataSetTriggerModel
                {
                    PublishedVariables = new PublishedDataItemsModel
                    {
                        PublishedData = [new PublishedDataSetVariableModel
                        {
                            PublishedVariableNodeId = "ns=2;s=TriggerTarget"
                        }]
                    }
                }
            };

            var result = variable.ToMonitoredItemTemplate(null, NamespaceFormat.Uri,
                includeTriggering: false);

            Assert.NotNull(result);
            Assert.Null(result.TriggeredItems);
        }

        // ── ToMonitoredItemTemplate (event) ───────────────────────────────────

        [Fact]
        public void ToMonitoredItemTemplate_NullEvent_ReturnsNull()
        {
            PublishedDataSetEventModel? evt = null;

            var result = evt.ToMonitoredItemTemplate(null, NamespaceFormat.Uri);

            Assert.Null(result);
        }

        [Fact]
        public void ToMonitoredItemTemplate_Event_WithNullEventNotifier_DefaultsToServer()
        {
            var evt = new PublishedDataSetEventModel
            {
                Id = "evt1",
                EventNotifier = null
            };

            var result = evt.ToMonitoredItemTemplate(null, NamespaceFormat.Uri);

            Assert.NotNull(result);
            Assert.Equal(Opc.Ua.ObjectIds.Server.ToString(), result.StartNodeId);
        }

        [Fact]
        public void ToMonitoredItemTemplate_Event_WithModelChangeHandling_ReturnsAddressSpaceModel()
        {
            var evt = new PublishedDataSetEventModel
            {
                Id = "model-change",
                EventNotifier = "i=2253",
                ModelChangeHandling = new ModelChangeHandlingOptionsModel
                {
                    RebrowseIntervalTimespan = System.TimeSpan.FromMinutes(10)
                }
            };

            var result = evt.ToMonitoredItemTemplate(null, NamespaceFormat.Uri);

            Assert.NotNull(result);
            Assert.IsType<MonitoredAddressSpaceModel>(result);
        }

        [Fact]
        public void ToMonitoredItemTemplate_Event_WithoutModelChangeHandling_ReturnsEventMonitoredItem()
        {
            var evt = new PublishedDataSetEventModel
            {
                Id = "regular-event",
                EventNotifier = "i=2253"
            };

            var result = evt.ToMonitoredItemTemplate(null, NamespaceFormat.Uri);

            Assert.NotNull(result);
            Assert.IsType<EventMonitoredItemModel>(result);
        }

        [Fact]
        public void ToMonitoredItemTemplate_Event_UsesIdAsDataSetFieldId()
        {
            var evt = new PublishedDataSetEventModel
            {
                Id = "my-event-field",
                EventNotifier = "i=2253"
            };

            var result = evt.ToMonitoredItemTemplate(null, NamespaceFormat.Uri);

            Assert.NotNull(result);
            Assert.Equal("my-event-field", result.DataSetFieldId);
        }

        [Fact]
        public void ToMonitoredItemTemplate_Event_WhenIdNullFallsBackToEventNotifier()
        {
            var evt = new PublishedDataSetEventModel
            {
                Id = null,
                EventNotifier = "i=2253"
            };

            var result = evt.ToMonitoredItemTemplate(null, NamespaceFormat.Uri);

            Assert.NotNull(result);
            Assert.Equal("i=2253", result.DataSetFieldId);
        }

        // ── ToMonitoredItems (DataItemsModel / EventItemsModel) ───────────────

        [Fact]
        public void ToMonitoredItemsDataItems_NullDataItems_ReturnsEmpty()
        {
            PublishedDataItemsModel? dataItems = null;

            var result = new System.Collections.Generic.List<BaseMonitoredItemModel>(
                dataItems.ToMonitoredItems(null, NamespaceFormat.Uri));

            Assert.Empty(result);
        }

        [Fact]
        public void ToMonitoredItemsDataItems_NullPublishedData_ReturnsEmpty()
        {
            var dataItems = new PublishedDataItemsModel { PublishedData = null };

            var result = new System.Collections.Generic.List<BaseMonitoredItemModel>(
                dataItems.ToMonitoredItems(null, NamespaceFormat.Uri));

            Assert.Empty(result);
        }

        [Fact]
        public void ToMonitoredItemsEventItems_NullPublishedData_ReturnsEmpty()
        {
            var eventItems = new PublishedEventItemsModel { PublishedData = null };

            var result = new System.Collections.Generic.List<BaseMonitoredItemModel>(
                eventItems.ToMonitoredItems(null, NamespaceFormat.Uri));

            Assert.Empty(result);
        }

        [Fact]
        public void ToMonitoredItemsDataItems_VariableWithNullNodeId_IsSkipped()
        {
            var dataItems = new PublishedDataItemsModel
            {
                PublishedData =
                [
                    new PublishedDataSetVariableModel { PublishedVariableNodeId = null },
                    new PublishedDataSetVariableModel { PublishedVariableNodeId = "ns=2;s=Valid" }
                ]
            };

            var result = new System.Collections.Generic.List<BaseMonitoredItemModel>(
                dataItems.ToMonitoredItems(null, NamespaceFormat.Uri));

            // null-NodeId item is skipped; only the valid one appears
            Assert.Single(result);
        }
    }
}
