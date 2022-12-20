// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IIoT.OpcUa.Publisher.Models {
    using Microsoft.Azure.IIoT.OpcUa.Protocol.Models;
    using Microsoft.Azure.IIoT.OpcUa.Core.Models;

    /// <summary>
    /// Variable extensions
    /// </summary>
    public static class PublishedDataSetVariableModelEx {

        /// <summary>
        /// Convert published dataset variable to monitored item
        /// </summary>
        /// <param name="publishedVariable"></param>
        /// <param name="displayName"></param>
        /// <returns></returns>
        public static DataMonitoredItemModel ToMonitoredItem(
            this PublishedDataSetVariableModel publishedVariable,
            string displayName = null) {
            if (string.IsNullOrEmpty(publishedVariable?.PublishedVariableNodeId)) {
                return null;
            }
            return new DataMonitoredItemModel {
                Id = publishedVariable.Id,
                DataSetClassFieldId = publishedVariable.DataSetClassFieldId,
                DisplayName = displayName ?? publishedVariable.PublishedVariableDisplayName,
                DataChangeFilter = ToDataChangeFilter(publishedVariable),
                AggregateFilter = null,
                SkipFirst = publishedVariable.SkipFirst ?? false,
                DiscardNew = publishedVariable.DiscardNew,
                StartNodeId = publishedVariable.PublishedVariableNodeId,

                //
                // see https://reference.opcfoundation.org/v104/Core/docs/Part4/7.16/
                // 0 or 1 the Server returns the default queue size which shall be 1
                // as revisedQueueSize for data monitored items. The queue has a single
                // entry, effectively disabling queuing. This is the default behavior
                // since beginning of publisher time.
                //
                QueueSize = publishedVariable.QueueSize ?? 1,
                RelativePath = publishedVariable.BrowsePath,
                AttributeId = publishedVariable.Attribute,
                IndexRange = publishedVariable.IndexRange,
                TriggerId = publishedVariable.TriggerId,
                MonitoringMode = publishedVariable.MonitoringMode,
                SamplingInterval = publishedVariable.SamplingInterval,
                HeartbeatInterval = publishedVariable.HeartbeatInterval
            };
        }

        /// <summary>
        /// Convert to data change filter
        /// </summary>
        /// <param name="publishedVariable"></param>
        /// <returns></returns>
        private static DataChangeFilterModel ToDataChangeFilter(
            this PublishedDataSetVariableModel publishedVariable) {
            if (publishedVariable?.DataChangeTrigger == null &&
                publishedVariable?.DeadbandType == null &&
                publishedVariable?.DeadbandValue == null) {
                return null;
            }
            return new DataChangeFilterModel {
                DataChangeTrigger = publishedVariable.DataChangeTrigger,
                DeadbandType = publishedVariable.DeadbandType,
                DeadbandValue = publishedVariable.DeadbandValue,
            };
        }
    }
}