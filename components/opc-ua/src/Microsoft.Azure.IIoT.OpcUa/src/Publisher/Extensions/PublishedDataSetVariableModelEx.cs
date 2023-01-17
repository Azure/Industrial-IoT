// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IIoT.OpcUa.Publisher.Models {
    using System.Linq;

    /// <summary>
    /// Events extensions
    /// </summary>
    public static class PublishedDataSetVariableModelEx {

        /// <summary>
        /// Clone
        /// </summary>
        /// <param name="model"></param>
        /// <returns></returns>
        public static PublishedDataSetVariableModel Clone(this PublishedDataSetVariableModel model) {
            if (model == null) {
                return null;
            }
            return new PublishedDataSetVariableModel {
                Id = model.Id,
                DiscardNew = model.DiscardNew,
                Attribute = model.Attribute,
                DataSetClassFieldId = model.DataSetClassFieldId,
                DataChangeTrigger = model.DataChangeTrigger,
                DeadbandType = model.DeadbandType,
                DeadbandValue = model.DeadbandValue,
                IndexRange = model.IndexRange,
                MetaDataProperties = model.MetaDataProperties?.ToList(),
                PublishedVariableNodeId = model.PublishedVariableNodeId,
                PublishedVariableDisplayName = model.PublishedVariableDisplayName,
                SamplingInterval = model.SamplingInterval,
                SkipFirst = model.SkipFirst,
                SubstituteValue = model.SubstituteValue?.Copy(),
                QueueSize = model.QueueSize,
                HeartbeatInterval = model.HeartbeatInterval,
                BrowsePath = model.BrowsePath,
                MonitoringMode = model.MonitoringMode,
                TriggerId = model.TriggerId
            };
        }
    }
}