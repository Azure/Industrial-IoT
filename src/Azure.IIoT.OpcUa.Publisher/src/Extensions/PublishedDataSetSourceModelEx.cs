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
        /// <param name="configuration"></param>
        /// <returns></returns>
        /// <exception cref="ArgumentNullException"><paramref name="dataSetSource"/> is <c>null</c>.</exception>
        public static SubscriptionConfigurationModel ToSubscriptionConfigurationModel(
            this PublishedDataSetSourceModel dataSetSource, DataSetMetaDataModel? dataSetMetaData,
            OpcUaSubscriptionOptions configuration)
        {
            return new SubscriptionConfigurationModel
            {
                Priority = dataSetSource.SubscriptionSettings?.Priority,
                MaxNotificationsPerPublish = dataSetSource.SubscriptionSettings?.MaxNotificationsPerPublish,
                LifetimeCount = dataSetSource.SubscriptionSettings?.LifeTimeCount
                    ?? configuration?.DefaultLifeTimeCount,
                KeepAliveCount = dataSetSource.SubscriptionSettings?.MaxKeepAliveCount
                    ?? configuration?.DefaultKeepAliveCount,
                PublishingInterval = dataSetSource.SubscriptionSettings?.PublishingInterval
                    ?? configuration?.DefaultPublishingInterval,
                ResolveDisplayName = dataSetSource.SubscriptionSettings?.ResolveDisplayName
                    ?? configuration?.ResolveDisplayName,
                UseDeferredAcknoledgements = dataSetSource.SubscriptionSettings?.UseDeferredAcknoledgements
                    ?? configuration?.UseDeferredAcknoledgements,
                AsyncMetaDataLoadThreshold = dataSetSource.SubscriptionSettings?.AsyncMetaDataLoadThreshold
                    ?? configuration?.AsyncMetaDataLoadThreshold,
                EnableImmediatePublishing = dataSetSource.SubscriptionSettings?.EnableImmediatePublishing
                    ?? configuration?.EnableImmediatePublishing ?? false,
                MetaData = configuration?.DisableDataSetMetaData == true
                    ? null : dataSetMetaData
            };
        }

        /// <summary>
        /// Convert dataset source to monitored item
        /// </summary>
        /// <param name="dataSetSource"></param>
        /// <param name="options"></param>
        /// <param name="extensionFields"></param>
        /// <returns></returns>
        public static List<BaseMonitoredItemModel> ToMonitoredItems(this PublishedDataSetSourceModel dataSetSource,
            OpcUaSubscriptionOptions options, IDictionary<string, VariantValue>? extensionFields = null)
        {
            var monitoredItems = Enumerable.Empty<BaseMonitoredItemModel>();
            if (dataSetSource.PublishedVariables?.PublishedData != null)
            {
                monitoredItems = monitoredItems
                    .Concat(dataSetSource.PublishedVariables.ToMonitoredItems(options));
            }
            if (dataSetSource.PublishedEvents?.PublishedData != null)
            {
                monitoredItems = monitoredItems
                    .Concat(dataSetSource.PublishedEvents.ToMonitoredItems(options));
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
        /// <param name="options"></param>
        /// <returns></returns>
        internal static IEnumerable<BaseMonitoredItemModel> ToMonitoredItems(
            this PublishedDataItemsModel dataItems, OpcUaSubscriptionOptions options)
        {
            if (dataItems?.PublishedData != null)
            {
                foreach (var publishedData in dataItems.PublishedData)
                {
                    var item = publishedData?.ToMonitoredItemTemplate(options);
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
        /// <param name="options"></param>
        /// <returns></returns>
        internal static IEnumerable<BaseMonitoredItemModel> ToMonitoredItems(
            this PublishedEventItemsModel eventItems, OpcUaSubscriptionOptions options)
        {
            if (eventItems?.PublishedData != null)
            {
                foreach (var publishedData in eventItems.PublishedData)
                {
                    var monitoredItem = publishedData?.ToMonitoredItemTemplate(options);
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
        /// <param name="options"></param>
        /// <returns></returns>
        internal static EventMonitoredItemModel? ToMonitoredItemTemplate(
            this PublishedDataSetEventModel publishedEvent,
            OpcUaSubscriptionOptions options)
        {
            if (publishedEvent == null)
            {
                return null;
            }

            var eventNotifier = publishedEvent.EventNotifier ?? Opc.Ua.ObjectIds.Server.ToString();
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
                DiscardNew = publishedEvent.DiscardNew
                    ?? options?.DefaultDiscardNew,

                //
                // see https://reference.opcfoundation.org/v104/Core/docs/Part4/7.16/
                // 0 the Server returns the default queue size for Event Notifications
                // as revisedQueueSize for event monitored items.
                //
                QueueSize = publishedEvent.QueueSize
                    ?? options?.DefaultQueueSize ?? 0,
                AttributeId = null,
                MonitoringMode = publishedEvent.MonitoringMode,
                StartNodeId = eventNotifier,
                RelativePath = publishedEvent.BrowsePath,
                FetchDataSetFieldName = publishedEvent.ReadEventNameFromNode
                    ?? options?.ResolveDisplayName,
                ConditionHandling = publishedEvent.ConditionHandling.Clone()
            };
        }

        /// <summary>
        /// Convert to monitored item
        /// </summary>
        /// <param name="extensionField"></param>
        /// <returns></returns>
        internal static ExtensionFieldModel? ToMonitoredItemTemplate(
            this KeyValuePair<string, VariantValue> extensionField)
        {
            if (string.IsNullOrEmpty(extensionField.Key))
            {
                return null;
            }
            return new ExtensionFieldModel
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
        /// <param name="options"></param>
        /// <returns></returns>
        internal static DataMonitoredItemModel? ToMonitoredItemTemplate(
            this PublishedDataSetVariableModel publishedVariable,
            OpcUaSubscriptionOptions options)
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
                    ?? options?.DefaultSamplingUsingCyclicRead ?? false,
                SkipFirst = publishedVariable.SkipFirst
                    ?? options?.DefaultSkipFirst ?? false,
                DiscardNew = publishedVariable.DiscardNew
                    ?? options?.DefaultDiscardNew,
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
                QueueSize = publishedVariable.QueueSize
                    ?? options?.DefaultQueueSize ?? 1,
                RelativePath = publishedVariable.BrowsePath,
                AttributeId = publishedVariable.Attribute,
                IndexRange = publishedVariable.IndexRange,
                MonitoringMode = publishedVariable.MonitoringMode,
                FetchDataSetFieldName = publishedVariable.ReadDisplayNameFromNode
                    ?? options?.ResolveDisplayName,
                SamplingInterval = publishedVariable.SamplingIntervalHint
                    ?? options?.DefaultSamplingInterval,
                HeartbeatInterval = publishedVariable.HeartbeatInterval
                    ?? options?.DefaultHeartbeatInterval,
                HeartbeatBehavior = publishedVariable.HeartbeatBehavior
                    ?? options?.DefaultHeartbeatBehavior,
                AggregateFilter = null
            };
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
