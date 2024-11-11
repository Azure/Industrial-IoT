// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Azure.IIoT.OpcUa.Publisher.Models
{
    using Azure.IIoT.OpcUa.Publisher.Stack.Models;
    using Furly.Extensions.Serializers;
    using System.Collections.Generic;
    using System.Linq;

    /// <summary>
    /// Source model extensions
    /// </summary>
    public static class PublishedDataSetSourceModelEx
    {
        /// <summary>
        /// Create subscription
        /// </summary>
        /// <param name="subscriptionSettings"></param>
        /// <param name="fetchBrowsePathFromRootOverride"></param>
        /// <param name="ignoreConfiguredPublishingIntervals"></param>
        /// <returns></returns>
        public static SubscriptionModel ToSubscriptionModel(
            this PublishedDataSetSettingsModel? subscriptionSettings,
            bool? fetchBrowsePathFromRootOverride, bool? ignoreConfiguredPublishingIntervals)
        {
            return new SubscriptionModel
            {
                Priority = subscriptionSettings?.Priority,
                MaxNotificationsPerPublish = subscriptionSettings?.MaxNotificationsPerPublish,
                LifetimeCount = subscriptionSettings?.LifeTimeCount,
                KeepAliveCount = subscriptionSettings?.MaxKeepAliveCount,
                PublishingInterval = ignoreConfiguredPublishingIntervals == true ?
                       null : subscriptionSettings?.PublishingInterval,
                UseDeferredAcknoledgements = subscriptionSettings?.UseDeferredAcknoledgements,
                EnableImmediatePublishing = subscriptionSettings?.EnableImmediatePublishing,
                EnableSequentialPublishing = subscriptionSettings?.EnableSequentialPublishing,
                RepublishAfterTransfer = subscriptionSettings?.RepublishAfterTransfer,
                MonitoredItemWatchdogTimeout = subscriptionSettings?.MonitoredItemWatchdogTimeout,
                WatchdogCondition = subscriptionSettings?.MonitoredItemWatchdogCondition,
                WatchdogBehavior = subscriptionSettings?.WatchdogBehavior,
                ResolveBrowsePathFromRoot = fetchBrowsePathFromRootOverride
            };
        }

        /// <summary>
        /// Convert dataset source to monitored item
        /// </summary>
        /// <param name="dataSetSource"></param>
        /// <param name="namespaceFormat"></param>
        /// <returns></returns>
        public static IReadOnlyList<BaseMonitoredItemModel> ToMonitoredItems(
            this PublishedDataSetSourceModel dataSetSource, NamespaceFormat namespaceFormat)
        {
            var monitoredItems = Enumerable.Empty<BaseMonitoredItemModel>();
            if (dataSetSource.PublishedVariables?.PublishedData != null)
            {
                monitoredItems = monitoredItems
                    .Concat(dataSetSource.PublishedVariables
                        .ToMonitoredItems(dataSetSource.SubscriptionSettings, namespaceFormat));
            }
            if (dataSetSource.PublishedEvents?.PublishedData != null)
            {
                monitoredItems = monitoredItems
                    .Concat(dataSetSource.PublishedEvents
                        .ToMonitoredItems(dataSetSource.SubscriptionSettings, namespaceFormat));
            }
            return monitoredItems.ToList();
        }

        /// <summary>
        /// Convert to monitored items including heartbeat handling.
        /// </summary>
        /// <param name="dataItems"></param>
        /// <param name="settings"></param>
        /// <param name="namespaceFormat"></param>
        /// <param name="includeTriggering"></param>
        /// <returns></returns>
        internal static IEnumerable<BaseMonitoredItemModel> ToMonitoredItems(
            this PublishedDataItemsModel dataItems, PublishedDataSetSettingsModel? settings,
            NamespaceFormat namespaceFormat, bool includeTriggering = true)
        {
            if (dataItems?.PublishedData != null)
            {
                foreach (var publishedData in dataItems.PublishedData)
                {
                    var item = publishedData?.ToMonitoredItemTemplate(settings,
                        namespaceFormat, includeTriggering);
                    if (item != null)
                    {
                        yield return item;
                    }
                }
            }
        }

        /// <summary>
        /// Convert to monitored items
        /// </summary>
        /// <param name="eventItems"></param>
        /// <param name="settings"></param>
        /// <param name="namespaceFormat"></param>
        /// <param name="includeTriggering"></param>
        /// <returns></returns>
        internal static IEnumerable<BaseMonitoredItemModel> ToMonitoredItems(
            this PublishedEventItemsModel eventItems, PublishedDataSetSettingsModel? settings,
            NamespaceFormat namespaceFormat, bool includeTriggering = true)
        {
            if (eventItems?.PublishedData != null)
            {
                foreach (var publishedData in eventItems.PublishedData)
                {
                    var monitoredItem = publishedData?.ToMonitoredItemTemplate(settings,
                        namespaceFormat, includeTriggering);
                    if (monitoredItem == null)
                    {
                        continue;
                    }
                    yield return monitoredItem;
                }
            }
        }

        /// <summary>
        /// Convert to monitored item
        /// </summary>
        /// <param name="publishedEvent"></param>
        /// <param name="settings"></param>
        /// <param name="namespaceFormat"></param>
        /// <param name="includeTriggering"></param>
        /// <returns></returns>
        internal static BaseMonitoredItemModel? ToMonitoredItemTemplate(
            this PublishedDataSetEventModel publishedEvent, PublishedDataSetSettingsModel? settings,
            NamespaceFormat namespaceFormat, bool includeTriggering = true)
        {
            if (publishedEvent == null)
            {
                return null;
            }

            var eventNotifier = publishedEvent.EventNotifier
                ?? Opc.Ua.ObjectIds.Server.ToString();

            if (publishedEvent.ModelChangeHandling != null)
            {
                return new MonitoredAddressSpaceModel
                {
                    DataSetFieldId = publishedEvent.Id
                        ?? eventNotifier,
                    DataSetFieldName = publishedEvent.PublishedEventName
                        ?? string.Empty,
                    FetchDataSetFieldName = publishedEvent.ReadEventNameFromNode
                        ?? settings?.ResolveDisplayName,
                    RebrowsePeriod =
                        publishedEvent.ModelChangeHandling.RebrowseIntervalTimespan,
                    TriggeredItems = includeTriggering ? null : ToMonitoredItems(
                        publishedEvent.Triggering, settings, namespaceFormat),
                    AttributeId = null,
                    DiscardNew = false,
                    MonitoringMode = publishedEvent.MonitoringMode,
                    StartNodeId = eventNotifier,
                    NamespaceFormat = namespaceFormat,
                    RootNodeId = Opc.Ua.ObjectIds.RootFolder.ToString()
                };
            }

            return new EventMonitoredItemModel
            {
                DataSetFieldId = publishedEvent.Id
                    ?? eventNotifier,
                DataSetFieldName = publishedEvent.PublishedEventName
                    ?? string.Empty,
                EventFilter = new EventFilterModel
                {
                    SelectClauses = publishedEvent.SelectedFields?
                        .Select(s => s.Clone()!)
                        .Where(s => s != null)
                        .ToArray(),
                    WhereClause = publishedEvent.Filter?.Clone(),
                    TypeDefinitionId = publishedEvent.TypeDefinitionId
                },
                DiscardNew = publishedEvent.DiscardNew,
                QueueSize = publishedEvent.QueueSize,
                AttributeId = null,
                MonitoringMode = publishedEvent.MonitoringMode,
                StartNodeId = eventNotifier,
                RelativePath = publishedEvent.BrowsePath,
                NamespaceFormat = namespaceFormat,
                FetchDataSetFieldName = publishedEvent.ReadEventNameFromNode
                    ?? settings?.ResolveDisplayName,
                TriggeredItems = includeTriggering ? null : ToMonitoredItems(
                    publishedEvent.Triggering, settings, namespaceFormat),
                ConditionHandling = publishedEvent.ConditionHandling.Clone()
            };
        }

        /// <summary>
        /// Convert published dataset variable to monitored item
        /// </summary>
        /// <param name="publishedVariable"></param>
        /// <param name="settings"></param>
        /// <param name="namespaceFormat"></param>
        /// <param name="includeTriggering"></param>
        /// <returns></returns>
        internal static DataMonitoredItemModel? ToMonitoredItemTemplate(
            this PublishedDataSetVariableModel publishedVariable, PublishedDataSetSettingsModel? settings,
            NamespaceFormat namespaceFormat, bool includeTriggering = true)
        {
            if (string.IsNullOrEmpty(publishedVariable.PublishedVariableNodeId))
            {
                return null;
            }
            return new DataMonitoredItemModel
            {
                DataSetFieldId = publishedVariable.Id
                    ?? publishedVariable.PublishedVariableNodeId,
                DataSetClassFieldId = publishedVariable.DataSetClassFieldId,
                DataSetFieldName = publishedVariable.PublishedVariableDisplayName
                    ?? string.Empty,
                DataChangeFilter = ToDataChangeFilter(publishedVariable),
                SamplingUsingCyclicRead = publishedVariable.SamplingUsingCyclicRead,
                CyclicReadMaxAge = publishedVariable.CyclicReadMaxAge,
                SkipFirst = publishedVariable.SkipFirst,
                DiscardNew = publishedVariable.DiscardNew,
                RegisterRead = publishedVariable.RegisterNodeForSampling,
                StartNodeId = publishedVariable.PublishedVariableNodeId,
                QueueSize = publishedVariable.ServerQueueSize,
                RelativePath = publishedVariable.BrowsePath,
                AttributeId = publishedVariable.Attribute,
                IndexRange = publishedVariable.IndexRange,
                MonitoringMode = publishedVariable.MonitoringMode,
                FetchDataSetFieldName = publishedVariable.ReadDisplayNameFromNode
                    ?? settings?.ResolveDisplayName,
                SamplingInterval = publishedVariable.SamplingIntervalHint
                    ?? settings?.DefaultSamplingInterval,
                HeartbeatInterval = publishedVariable.HeartbeatInterval
                    ?? settings?.DefaultHeartbeatInterval,
                HeartbeatBehavior = publishedVariable.HeartbeatBehavior
                    ?? settings?.DefaultHeartbeatBehavior,
                AggregateFilter = null,
                AutoSetQueueSize = null,
                NamespaceFormat = namespaceFormat,
                TriggeredItems = includeTriggering ? null : ToMonitoredItems(
                    publishedVariable.Triggering, settings, namespaceFormat)
            };
        }

        /// <summary>
        /// Convert triggering to monitored items
        /// </summary>
        /// <param name="triggering"></param>
        /// <param name="settings"></param>
        /// <param name="namespaceFormat"></param>
        /// <returns></returns>
        private static List<BaseMonitoredItemModel>? ToMonitoredItems(
            this PublishedDataSetTriggerModel? triggering, PublishedDataSetSettingsModel? settings,
            NamespaceFormat namespaceFormat)
        {
            if (triggering?.PublishedVariables == null && triggering?.PublishedEvents == null)
            {
                return null;
            }
            var monitoredItems = Enumerable.Empty<BaseMonitoredItemModel>();
            if (triggering.PublishedVariables?.PublishedData != null)
            {
                monitoredItems = monitoredItems
                    .Concat(triggering.PublishedVariables
                        .ToMonitoredItems(settings, namespaceFormat, false));
            }
            if (triggering.PublishedEvents?.PublishedData != null)
            {
                monitoredItems = monitoredItems
                    .Concat(triggering.PublishedEvents
                        .ToMonitoredItems(settings, namespaceFormat, false));
            }
            return monitoredItems.ToList();
        }

        /// <summary>
        /// Convert to data change filter
        /// </summary>
        /// <param name="publishedVariable"></param>
        /// <returns></returns>
        private static DataChangeFilterModel? ToDataChangeFilter(
            this PublishedDataSetVariableModel publishedVariable)
        {
            if (publishedVariable.DataChangeTrigger == null &&
                publishedVariable.DeadbandType == null &&
                publishedVariable.DeadbandValue == null)
            {
                return null;
            }
            return new DataChangeFilterModel
            {
                DataChangeTrigger = publishedVariable.DataChangeTrigger,
                DeadbandType = publishedVariable.DeadbandType,
                DeadbandValue = publishedVariable.DeadbandValue
            };
        }
    }
}
