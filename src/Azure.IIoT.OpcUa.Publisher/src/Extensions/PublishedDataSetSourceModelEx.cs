// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Azure.IIoT.OpcUa.Publisher.Models
{
    using Azure.IIoT.OpcUa.Publisher.Stack;
    using Azure.IIoT.OpcUa.Publisher.Stack.Models;
    using Furly.Extensions.Serializers;
    using System;
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
        /// <param name="dataSetSource"></param>
        /// <param name="dataSetMetaData"></param>
        /// <param name="options"></param>
        /// <param name="fetchBrowsePathFromRootOverride"></param>
        /// <returns></returns>
        public static SubscriptionConfigurationModel ToSubscriptionConfigurationModel(
            this PublishedDataSetSourceModel dataSetSource, DataSetMetaDataModel? dataSetMetaData,
            OpcUaSubscriptionOptions options, bool? fetchBrowsePathFromRootOverride)
        {
            return new SubscriptionConfigurationModel
            {
                Priority = dataSetSource.SubscriptionSettings?.Priority,
                MaxNotificationsPerPublish = dataSetSource.SubscriptionSettings?.MaxNotificationsPerPublish,
                LifetimeCount = dataSetSource.SubscriptionSettings?.LifeTimeCount
                    ?? options.DefaultLifeTimeCount,
                KeepAliveCount = dataSetSource.SubscriptionSettings?.MaxKeepAliveCount
                    ?? options.DefaultKeepAliveCount,
                PublishingInterval = dataSetSource.SubscriptionSettings?.PublishingInterval
                    ?? options.DefaultPublishingInterval,
                UseDeferredAcknoledgements = dataSetSource.SubscriptionSettings?.UseDeferredAcknoledgements
                    ?? options.UseDeferredAcknoledgements,
                AsyncMetaDataLoadThreshold = dataSetSource.SubscriptionSettings?.AsyncMetaDataLoadThreshold
                    ?? options.AsyncMetaDataLoadThreshold,
                EnableImmediatePublishing = dataSetSource.SubscriptionSettings?.EnableImmediatePublishing
                    ?? options.EnableImmediatePublishing ?? false,
                EnableSequentialPublishing = dataSetSource.SubscriptionSettings?.EnableSequentialPublishing
                    ?? options.EnableSequentialPublishing ?? true,
                RepublishAfterTransfer = dataSetSource.SubscriptionSettings?.RepublishAfterTransfer
                    ?? options.DefaultRepublishAfterTransfer ?? true,
                MonitoredItemWatchdogTimeout = dataSetSource.SubscriptionSettings?.MonitoredItemWatchdogTimeout
                    ?? options.DefaultMonitoredItemWatchdogTimeout,
                WatchdogCondition = dataSetSource.SubscriptionSettings?.MonitoredItemWatchdogCondition
                    ?? options.DefaultMonitoredItemWatchdogCondition,
                WatchdogBehavior = dataSetSource.SubscriptionSettings?.WatchdogBehavior
                    ?? options.DefaultWatchdogBehavior,
                ResolveBrowsePathFromRoot = fetchBrowsePathFromRootOverride
                    ?? options.FetchOpcBrowsePathFromRoot ?? false,
                MetaData = options.DisableDataSetMetaData == true
                    ? null : dataSetMetaData
            };
        }

        /// <summary>
        /// Convert dataset source to monitored item
        /// </summary>
        /// <param name="dataSetSource"></param>
        /// <param name="options"></param>
        /// <param name="configure"></param>
        /// <param name="extensionFields"></param>
        /// <returns></returns>
        public static IReadOnlyList<BaseMonitoredItemModel> ToMonitoredItems(
            this PublishedDataSetSourceModel dataSetSource, OpcUaSubscriptionOptions options,
            Func<PublishingQueueSettingsModel?, object?> configure,
            IDictionary<string, VariantValue>? extensionFields = null)
        {
            var monitoredItems = Enumerable.Empty<BaseMonitoredItemModel>();
            if (dataSetSource.PublishedVariables?.PublishedData != null)
            {
                monitoredItems = monitoredItems
                    .Concat(dataSetSource.PublishedVariables
                        .ToMonitoredItems(dataSetSource.SubscriptionSettings, options, configure));
            }
            if (dataSetSource.PublishedEvents?.PublishedData != null)
            {
                monitoredItems = monitoredItems
                    .Concat(dataSetSource.PublishedEvents
                        .ToMonitoredItems(dataSetSource.SubscriptionSettings, options, configure));
            }
            if (extensionFields != null)
            {
                monitoredItems = monitoredItems
                    .Concat(extensionFields.ToMonitoredItems());
            }
            return monitoredItems.ToList();
        }

        /// <summary>
        /// Convert to monitored items including heartbeat handling.
        /// </summary>
        /// <param name="dataItems"></param>
        /// <param name="settings"></param>
        /// <param name="options"></param>
        /// <param name="configure"></param>
        /// <param name="includeTriggering"></param>
        /// <returns></returns>
        internal static IEnumerable<BaseMonitoredItemModel> ToMonitoredItems(
            this PublishedDataItemsModel dataItems, PublishedDataSetSettingsModel? settings,
            OpcUaSubscriptionOptions options, Func<PublishingQueueSettingsModel?, object?> configure,
            bool includeTriggering = true)
        {
            if (dataItems?.PublishedData != null)
            {
                foreach (var publishedData in dataItems.PublishedData)
                {
                    var item = publishedData?.ToMonitoredItemTemplate(settings,
                        options, configure, includeTriggering);
                    if (item != null)
                    {
                        yield return item;
                    }
                }
            }
        }

        /// <summary>
        /// Convert to extension field items
        /// </summary>
        /// <param name="extensionFields"></param>
        /// <returns></returns>
        internal static IEnumerable<BaseMonitoredItemModel> ToMonitoredItems(
            this IDictionary<string, VariantValue> extensionFields)
        {
            foreach (var extensionField in extensionFields)
            {
                var item = extensionField.ToMonitoredItemTemplate();
                if (item != null)
                {
                    yield return item;
                }
            }
        }

        /// <summary>
        /// Convert to monitored items
        /// </summary>
        /// <param name="eventItems"></param>
        /// <param name="settings"></param>
        /// <param name="options"></param>
        /// <param name="configure"></param>
        /// <param name="includeTriggering"></param>
        /// <returns></returns>
        internal static IEnumerable<BaseMonitoredItemModel> ToMonitoredItems(
            this PublishedEventItemsModel eventItems, PublishedDataSetSettingsModel? settings,
            OpcUaSubscriptionOptions options, Func<PublishingQueueSettingsModel?, object?> configure,
            bool includeTriggering = true)
        {
            if (eventItems?.PublishedData != null)
            {
                foreach (var publishedData in eventItems.PublishedData)
                {
                    var monitoredItem = publishedData?.ToMonitoredItemTemplate(settings,
                        options, configure, includeTriggering);
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
        /// <param name="options"></param>
        /// <param name="configure"></param>
        /// <param name="includeTriggering"></param>
        /// <returns></returns>
        internal static BaseMonitoredItemModel? ToMonitoredItemTemplate(
            this PublishedDataSetEventModel publishedEvent, PublishedDataSetSettingsModel? settings,
            OpcUaSubscriptionOptions options, Func<PublishingQueueSettingsModel?, object?> configure,
            bool includeTriggering = true)
        {
            if (publishedEvent == null)
            {
                return null;
            }

            var eventNotifier = publishedEvent.EventNotifier ?? Opc.Ua.ObjectIds.Server.ToString();

            if (publishedEvent.ModelChangeHandling != null)
            {
                return new MonitoredAddressSpaceModel
                {
                    DataSetFieldId = publishedEvent.Id ?? eventNotifier,
                    DataSetFieldName = publishedEvent.PublishedEventName ?? string.Empty,
                    //
                    // see https://reference.opcfoundation.org/v104/Core/docs/Part4/7.16/
                    // 0 the Server returns the default queue size for Event Notifications
                    // as revisedQueueSize for event monitored items.
                    //
                    QueueSize = options.DefaultQueueSize ?? 0,
                    FetchDataSetFieldName = publishedEvent.ReadEventNameFromNode
                        ?? settings?.ResolveDisplayName
                        ?? options.ResolveDisplayName,
                    RebrowsePeriod = publishedEvent.ModelChangeHandling.RebrowseIntervalTimespan
                        ?? options.DefaultRebrowsePeriod ?? TimeSpan.FromHours(12),
                    TriggeredItems = includeTriggering ? null : ToMonitoredItems(
                        publishedEvent.Triggering, settings, options, configure),
                    AttributeId = null,
                    DiscardNew = false,
                    MonitoringMode = publishedEvent.MonitoringMode ?? MonitoringMode.Reporting,
                    StartNodeId = eventNotifier,
                    Context = configure(publishedEvent.Publishing),
                    RootNodeId = Opc.Ua.ObjectIds.RootFolder.ToString()
                };
            }

            return new EventMonitoredItemModel
            {
                DataSetFieldId = publishedEvent.Id ?? eventNotifier,
                DataSetFieldName = publishedEvent.PublishedEventName ?? string.Empty,
                EventFilter = new EventFilterModel
                {
                    SelectClauses = publishedEvent.SelectedFields?
                        .Select(s => s.Clone()!)
                        .Where(s => s != null)
                        .ToList(),
                    WhereClause = publishedEvent.Filter?.Clone(),
                    TypeDefinitionId = publishedEvent.TypeDefinitionId
                },
                DiscardNew = publishedEvent.DiscardNew ?? options.DefaultDiscardNew,

                //
                // see https://reference.opcfoundation.org/v104/Core/docs/Part4/7.16/
                // 0 the Server returns the default queue size for Event Notifications
                // as revisedQueueSize for event monitored items.
                //
                QueueSize = publishedEvent.QueueSize
                    ?? options.DefaultQueueSize ?? 0,
                AttributeId = null,
                MonitoringMode = publishedEvent.MonitoringMode,
                StartNodeId = eventNotifier,
                RelativePath = publishedEvent.BrowsePath,
                FetchDataSetFieldName = publishedEvent.ReadEventNameFromNode
                    ?? settings?.ResolveDisplayName
                    ?? options.ResolveDisplayName,
                TriggeredItems = includeTriggering ? null : ToMonitoredItems(
                    publishedEvent.Triggering, settings, options, configure),
                Context = configure(publishedEvent.Publishing),
                ConditionHandling = publishedEvent.ConditionHandling.Clone()
            };
        }

        /// <summary>
        /// Convert to monitored item
        /// </summary>
        /// <param name="extensionField"></param>
        /// <returns></returns>
        internal static ExtensionFieldItemModel? ToMonitoredItemTemplate(
            this KeyValuePair<string, VariantValue> extensionField)
        {
            if (string.IsNullOrEmpty(extensionField.Key))
            {
                return null;
            }
            return new ExtensionFieldItemModel
            {
                DataSetFieldName = extensionField.Key,
                Value = extensionField.Value,
                StartNodeId = string.Empty
            };
        }

        /// <summary>
        /// Convert published dataset variable to monitored item
        /// </summary>
        /// <param name="publishedVariable"></param>
        /// <param name="settings"></param>
        /// <param name="options"></param>
        /// <param name="configure"></param>
        /// <param name="includeTriggering"></param>
        /// <returns></returns>
        internal static DataMonitoredItemModel? ToMonitoredItemTemplate(
            this PublishedDataSetVariableModel publishedVariable,
            PublishedDataSetSettingsModel? settings, OpcUaSubscriptionOptions options,
            Func<PublishingQueueSettingsModel?, object?> configure, bool includeTriggering = true)
        {
            if (string.IsNullOrEmpty(publishedVariable.PublishedVariableNodeId))
            {
                return null;
            }
            return new DataMonitoredItemModel
            {
                DataSetFieldId = publishedVariable.Id ?? publishedVariable.PublishedVariableNodeId,
                DataSetClassFieldId = publishedVariable.DataSetClassFieldId,
                DataSetFieldName = publishedVariable.PublishedVariableDisplayName
                    ?? string.Empty,
                DataChangeFilter = ToDataChangeFilter(publishedVariable, options),
                SamplingUsingCyclicRead = publishedVariable.SamplingUsingCyclicRead
                    ?? options.DefaultSamplingUsingCyclicRead ?? false,
                SkipFirst = publishedVariable.SkipFirst
                    ?? options.DefaultSkipFirst ?? false,
                DiscardNew = publishedVariable.DiscardNew
                    ?? options.DefaultDiscardNew,
                RegisterRead = publishedVariable.RegisterNodeForSampling
                    ?? false,
                StartNodeId = publishedVariable.PublishedVariableNodeId,

                //
                // see https://reference.opcfoundation.org/v104/Core/docs/Part4/7.16/
                // 0 or 1 the Server returns the default queue size which shall be 1
                // as revisedQueueSize for data monitored items. The queue has a single
                // entry, effectively disabling queuing. This is the default behavior
                // since beginning of publisher time.
                //
                QueueSize = publishedVariable.ServerQueueSize
                    ?? options.DefaultQueueSize
                    ?? 1,
                RelativePath = publishedVariable.BrowsePath,
                AttributeId = publishedVariable.Attribute,
                IndexRange = publishedVariable.IndexRange,
                MonitoringMode = publishedVariable.MonitoringMode,
                TriggeredItems = includeTriggering ? null : ToMonitoredItems(
                    publishedVariable.Triggering, settings, options, configure),
                Context = configure(publishedVariable.Publishing),
                FetchDataSetFieldName = publishedVariable.ReadDisplayNameFromNode
                    ?? settings?.ResolveDisplayName
                    ?? options.ResolveDisplayName,
                SamplingInterval = publishedVariable.SamplingIntervalHint
                    ?? settings?.DefaultSamplingInterval
                    ?? options.DefaultSamplingInterval,
                HeartbeatInterval = publishedVariable.HeartbeatInterval
                    ?? options.DefaultHeartbeatInterval,
                HeartbeatBehavior = publishedVariable.HeartbeatBehavior
                    ?? options.DefaultHeartbeatBehavior,
                AggregateFilter = null
            };
        }

        /// <summary>
        /// Convert triggering to monitored items
        /// </summary>
        /// <param name="triggering"></param>
        /// <param name="settings"></param>
        /// <param name="options"></param>
        /// <param name="configure"></param>
        /// <returns></returns>
        private static List<BaseMonitoredItemModel>? ToMonitoredItems(
            this PublishedDataSetTriggerModel? triggering, PublishedDataSetSettingsModel? settings,
            OpcUaSubscriptionOptions options, Func<PublishingQueueSettingsModel?, object?> configure)
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
                        .ToMonitoredItems(settings, options, configure, false));
            }
            if (triggering.PublishedEvents?.PublishedData != null)
            {
                monitoredItems = monitoredItems
                    .Concat(triggering.PublishedEvents
                        .ToMonitoredItems(settings, options, configure, false));
            }
            return monitoredItems.ToList();
        }

        /// <summary>
        /// Convert to data change filter
        /// </summary>
        /// <param name="publishedVariable"></param>
        /// <param name="options"></param>
        /// <returns></returns>
        private static DataChangeFilterModel? ToDataChangeFilter(
            this PublishedDataSetVariableModel publishedVariable,
            OpcUaSubscriptionOptions options)
        {
            if (publishedVariable.DataChangeTrigger == null &&
                publishedVariable.DeadbandType == null &&
                publishedVariable.DeadbandValue == null)
            {
                return null;
            }
            return new DataChangeFilterModel
            {
                DataChangeTrigger = publishedVariable.DataChangeTrigger
                    ?? options.DefaultDataChangeTrigger
                    ?? DataChangeTriggerType.StatusValue,
                DeadbandType = publishedVariable.DeadbandType,
                DeadbandValue = publishedVariable.DeadbandValue
            };
        }
    }
}
