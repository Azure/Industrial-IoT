// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Azure.IIoT.OpcUa.Publisher.Models
{
    using Azure.IIoT.OpcUa.Publisher.Stack;
    using Azure.IIoT.OpcUa.Publisher.Stack.Models;

    /// <summary>
    /// Variable extensions
    /// </summary>
    public static class PublishedDataSetVariableModelEx
    {
        /// <summary>
        /// Convert published dataset variable to monitored item
        /// </summary>
        /// <param name="publishedVariable"></param>
        /// <param name="options"></param>
        /// <param name="displayName"></param>
        /// <returns></returns>
        public static DataMonitoredItemModel? ToMonitoredItem(
            this PublishedDataSetVariableModel publishedVariable,
            OpcUaSubscriptionOptions options, string? displayName = null)
        {
            if (string.IsNullOrEmpty(publishedVariable.PublishedVariableNodeId))
            {
                return null;
            }
            return new DataMonitoredItemModel
            {
                DataSetFieldId = publishedVariable.Id ?? publishedVariable.PublishedVariableNodeId,
                DataSetClassFieldId = publishedVariable.DataSetClassFieldId,
                DataSetFieldName = displayName ?? publishedVariable.PublishedVariableDisplayName
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
