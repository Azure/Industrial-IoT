// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Azure.IIoT.OpcUa.Publisher.Models
{
    using Azure.IIoT.OpcUa.Publisher.Stack;
    using Azure.IIoT.OpcUa.Publisher.Stack.Models;
    using System;
    using System.Collections.Generic;
    using System.Linq;

    /// <summary>
    /// Data set extensions
    /// </summary>
    public static class DataSetWriterModelEx
    {
        /// <summary>
        /// Create subscription info model from message trigger configuration.
        /// </summary>
        /// <param name="dataSetWriter"></param>
        /// <param name="configuration"></param>
        /// <param name="configure"></param>
        /// <exception cref="ArgumentNullException"></exception>
        /// <exception cref="ArgumentException"></exception>
        public static SubscriptionModel ToSubscriptionModel(
            this DataSetWriterModel dataSetWriter, OpcUaSubscriptionOptions configuration,
            Func<PublishingQueueSettingsModel?, object?> configure)
        {
            if (dataSetWriter.DataSet == null)
            {
                throw new ArgumentException("DataSet missing,", nameof(dataSetWriter));
            }
            return new SubscriptionModel
            {
                Id = ToSubscriptionId(dataSetWriter),
                MonitoredItems = dataSetWriter.DataSet
                    .ToMonitoredItems(configuration, configure),
                Configuration = dataSetWriter.DataSet.DataSetSource
                    .ToSubscriptionConfigurationModel(configuration)
            };
        }

        /// <summary>
        /// Create subscription id.
        /// </summary>
        /// <param name="dataSetWriter"></param>
        /// <exception cref="ArgumentNullException"></exception>
        /// <exception cref="ArgumentException"></exception>
        public static SubscriptionIdentifier ToSubscriptionId(this DataSetWriterModel dataSetWriter)
        {
            ArgumentNullException.ThrowIfNull(dataSetWriter);
            if (dataSetWriter.Id == null)
            {
                throw new ArgumentException("DataSetWriter Id missing.", nameof(dataSetWriter));
            }
            if (dataSetWriter.DataSet?.DataSetSource?.Connection == null)
            {
                throw new ArgumentException("Connection missing from data source", nameof(dataSetWriter));
            }
            var connection = dataSetWriter.DataSet.DataSetSource.Connection;
            return new SubscriptionIdentifier(connection, dataSetWriter.Id);
        }

        /// <summary>
        /// Create subscription
        /// </summary>
        /// <param name="dataSetSource"></param>
        /// <param name="options"></param>
        /// <returns></returns>
        /// <exception cref="ArgumentException"><paramref name="dataSetSource"/></exception>
        private static SubscriptionConfigurationModel ToSubscriptionConfigurationModel(
            this PublishedDataSetSourceModel? dataSetSource, OpcUaSubscriptionOptions options)
        {
            if (dataSetSource == null)
            {
                throw new ArgumentException("DataSet source missing,", nameof(dataSetSource));
            }
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
                EnableImmediatePublishing = dataSetSource.SubscriptionSettings?.EnableImmediatePublishing
                    ?? options.EnableImmediatePublishing ?? false,
                EnableSequentialPublishing = dataSetSource.SubscriptionSettings?.EnableSequentialPublishing
                    ?? options.EnableSequentialPublishing ?? true
            };
        }

        /// <summary>
        /// Convert dataset source to monitored item
        /// </summary>
        /// <param name="dataSet"></param>
        /// <param name="options"></param>
        /// <param name="configure"></param>
        /// <returns></returns>
        /// <exception cref="ArgumentException"></exception>
        private static List<BaseItemModel> ToMonitoredItems(
            this PublishedDataSetModel dataSet, OpcUaSubscriptionOptions options,
            Func<PublishingQueueSettingsModel?, object?> configure)
        {
            var monitoredItems = new List<BaseItemModel>();
            var dataSetSource = dataSet.DataSetSource;
            if (dataSetSource == null)
            {
                throw new ArgumentException("DataSet source missing,", nameof(dataSet));
            }
            dataSetSource.PublishedVariables?.ToMonitoredItems(options, monitoredItems, configure);
            dataSetSource.PublishedEvents?.ToMonitoredItems(options, monitoredItems, configure);
            dataSetSource.PublishedObjects?.ToMonitoredItems(options, monitoredItems, configure);
            dataSet.ExtensionFields?.ToMonitoredItems(monitoredItems, configure);
            if (monitoredItems.Count == 0)
            {
                throw new ArgumentException("DataSet source empty.", nameof(dataSet));
            }
            return monitoredItems;
        }

        /// <summary>
        /// Convert to monitored items
        /// </summary>
        /// <param name="dataItems"></param>
        /// <param name="options"></param>
        /// <param name="items"></param>
        /// <param name="configure"></param>
        /// <returns></returns>
        private static void ToMonitoredItems(
            this PublishedObjectItemsModel dataItems, OpcUaSubscriptionOptions options,
            List<BaseItemModel> items,
            Func<PublishingQueueSettingsModel?, object?> configure)
        {
            if (dataItems?.PublishedData == null)
            {
                return;
            }
            foreach (var publishedData in dataItems.PublishedData
                .Where(o => o.PublishedVariables != null)
                .SelectMany(o => o.PublishedVariables!.PublishedData)
                .OrderBy(b => b.FieldIndex))
            {
                var item = publishedData?.ToMonitoredItemTemplate(options,
                    configure, items.Count);
                if (item != null)
                {
                    items.Add(item);
                }
            }
        }

        /// <summary>
        /// Convert to monitored items
        /// </summary>
        /// <param name="dataItems"></param>
        /// <param name="options"></param>
        /// <param name="items"></param>
        /// <param name="configure"></param>
        /// <param name="includeTriggering"></param>
        /// <returns></returns>
        private static void ToMonitoredItems(
            this PublishedDataItemsModel dataItems, OpcUaSubscriptionOptions options,
            List<BaseItemModel> items,
            Func<PublishingQueueSettingsModel?, object?> configure,
            bool includeTriggering = true)
        {
            if (dataItems?.PublishedData == null)
            {
                return;
            }
            foreach (var publishedData in dataItems.PublishedData
                .OrderBy(b => b.FieldIndex))
            {
                var item = publishedData?.ToMonitoredItemTemplate(options,
                    configure, items.Count, includeTriggering);
                if (item != null)
                {
                    items.Add(item);
                }
            }
        }

        /// <summary>
        /// Convert to extension field items
        /// </summary>
        /// <param name="extensionFields"></param>
        /// <param name="items"></param>
        /// <param name="configure"></param>
        /// <returns></returns>
        private static void ToMonitoredItems(
            this IReadOnlyList<ExtensionFieldModel> extensionFields,
            List<BaseItemModel> items, Func<PublishingQueueSettingsModel?, object?> configure)
        {
            foreach (var extensionField in extensionFields
                .OrderBy(b => b.FieldIndex))
            {
                var item = extensionField.ToMonitoredItemTemplate(configure, items.Count);
                if (item != null)
                {
                    items.Add(item);
                }
            }
        }

        /// <summary>
        /// Convert to monitored items
        /// </summary>
        /// <param name="eventItems"></param>
        /// <param name="options"></param>
        /// <param name="items"></param>
        /// <param name="configure"></param>
        /// <param name="includeTriggering"></param>
        /// <returns></returns>
        private static void ToMonitoredItems(
            this PublishedEventItemsModel eventItems, OpcUaSubscriptionOptions options,
            List<BaseItemModel> items,
            Func<PublishingQueueSettingsModel?, object?> configure,
            bool includeTriggering = true)
        {
            if ((eventItems?.PublishedData) == null)
            {
                return;
            }
            foreach (var publishedData in eventItems.PublishedData)
            {
                var monitoredItem = publishedData?.ToMonitoredItemTemplate(
                    options, configure, items.Count, includeTriggering);
                if (monitoredItem == null)
                {
                    continue;
                }
                items.Add(monitoredItem);
            }
        }

        /// <summary>
        /// Convert to monitored item
        /// </summary>
        /// <param name="publishedEvent"></param>
        /// <param name="options"></param>
        /// <param name="configure"></param>
        /// <param name="order"></param>
        /// <param name="includeTriggering"></param>
        /// <returns></returns>
        private static BaseItemModel? ToMonitoredItemTemplate(
            this PublishedDataSetEventModel publishedEvent, OpcUaSubscriptionOptions options,
            Func<PublishingQueueSettingsModel?, object?> configure, int order,
            bool includeTriggering = true)
        {
            if (publishedEvent == null)
            {
                return null;
            }

            if (publishedEvent.State != null)
            {
                return new ConfigurationErrorItemModel
                {
                    Order = order,
                    Id = publishedEvent.Id,
                    Name = publishedEvent.Name,
                    Context = configure(publishedEvent.Publishing),
                    State = publishedEvent.State
                };
            }

            var eventNotifier = publishedEvent.EventNotifier ?? Opc.Ua.ObjectIds.Server.ToString();
            var fields = publishedEvent.SelectedFields;
            if (publishedEvent.ModelChangeHandling != null)
            {
                return new MonitoredAddressSpaceModel
                {
                    Order = order,
                    Id = publishedEvent.Id,
                    Name = publishedEvent.Name,
                    //
                    // see https://reference.opcfoundation.org/v104/Core/docs/Part4/7.16/
                    // 0 the Server returns the default queue size for Event Notifications
                    // as revisedQueueSize for event monitored items.
                    //
                    QueueSize = options.DefaultQueueSize ?? 0,
                    RebrowsePeriod = publishedEvent.ModelChangeHandling.RebrowseIntervalTimespan
                        ?? options.DefaultRebrowsePeriod ?? TimeSpan.FromHours(12),
                    TriggeredItems = includeTriggering ? null
                        : ToMonitoredItems(publishedEvent.Triggering, options, configure),
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
                Order = order,
                Id = publishedEvent.Id,
                Name = publishedEvent.Name,
                EventFilter = new EventFilterModel
                {
                    SelectClauses = publishedEvent.SelectedFields?
                        .Select(s => s.Clone()!)
                        .Where(s => s != null)
                        .OrderBy(s => s.FieldIndex)
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
                TriggeredItems = includeTriggering ? null
                    : ToMonitoredItems(publishedEvent.Triggering, options, configure),
                Context = configure(publishedEvent.Publishing),
                ConditionHandling = publishedEvent.ConditionHandling.Clone()
            };
        }

        /// <summary>
        /// Convert to monitored item
        /// </summary>
        /// <param name="extensionField"></param>
        /// <param name="configure"></param>
        /// <param name="order"></param>
        /// <returns></returns>
        private static ExtensionFieldItemModel? ToMonitoredItemTemplate(
            this ExtensionFieldModel extensionField,
            Func<PublishingQueueSettingsModel?, object?> configure, int order)
        {
            if (string.IsNullOrEmpty(extensionField.DataSetFieldName))
            {
                return null;
            }
            return new ExtensionFieldItemModel
            {
                Id = extensionField.Id,
                Order = order,
                Name = extensionField.DataSetFieldName,
                Value = extensionField.Value,
                Context = configure(null) // TODO
            };
        }

        /// <summary>
        /// Convert published dataset variable to monitored item
        /// </summary>
        /// <param name="publishedVariable"></param>
        /// <param name="options"></param>
        /// <param name="configure"></param>
        /// <param name="order"></param>
        /// <param name="includeTriggering"></param>
        /// <returns></returns>
        private static BaseItemModel? ToMonitoredItemTemplate(
            this PublishedDataSetVariableModel publishedVariable, OpcUaSubscriptionOptions options,
            Func<PublishingQueueSettingsModel?, object?> configure, int order,
            bool includeTriggering = true)
        {
            if (string.IsNullOrEmpty(publishedVariable.PublishedVariableNodeId))
            {
                return null;
            }

            if (publishedVariable.State != null)
            {
                return new ConfigurationErrorItemModel
                {
                    Order = order,
                    Id = publishedVariable.Id,
                    Name = publishedVariable.DataSetFieldName,
                    Context = configure(publishedVariable.Publishing),
                    State = publishedVariable.State
                };
            }
            return new DataMonitoredItemModel
            {
                Order = order,
                Id = publishedVariable.PublishedVariableNodeId,
                DataSetClassFieldId = publishedVariable.DataSetClassFieldId,
                Name = publishedVariable.DataSetFieldName,
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
                    ?? options.DefaultQueueSize ?? 1,
                RelativePath = publishedVariable.BrowsePath,
                AttributeId = publishedVariable.Attribute,
                IndexRange = publishedVariable.IndexRange,
                MonitoringMode = publishedVariable.MonitoringMode,
                TriggeredItems = includeTriggering ? null
                    : ToMonitoredItems(publishedVariable.Triggering, options, configure),
                Context = configure(publishedVariable.Publishing),
                SamplingInterval = publishedVariable.SamplingIntervalHint
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
        /// <param name="options"></param>
        /// <param name="configure"></param>
        /// <returns></returns>
        private static List<BaseItemModel>? ToMonitoredItems(
            this PublishedDataSetTriggerModel? triggering, OpcUaSubscriptionOptions options,
            Func<PublishingQueueSettingsModel?, object?> configure)
        {
            if (triggering?.PublishedVariables == null && triggering?.PublishedEvents == null)
            {
                return null;
            }
            var monitoredItems = new List<BaseItemModel>();
            if (triggering.PublishedVariables?.PublishedData != null)
            {
                triggering.PublishedVariables.ToMonitoredItems(options,
                    monitoredItems, configure, false);
            }
            if (triggering.PublishedEvents?.PublishedData != null)
            {
                triggering.PublishedEvents.ToMonitoredItems(options,
                    monitoredItems, configure, false);
            }
            return monitoredItems;
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
