// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IIoT.OpcUa.Publisher.Models {
    using Microsoft.Azure.IIoT.OpcUa.Core.Models;
    using System.Linq;

    /// <summary>
    /// Events extensions
    /// </summary>
    public static class PublishedDataSetEventsDataModelEx {

        /// <summary>
        /// Clone
        /// </summary>
        /// <param name="model"></param>
        /// <returns></returns>
        public static PublishedDataSetEventModel Clone(this PublishedDataSetEventModel model) {
            if (model == null) {
                return null;
            }
            return new PublishedDataSetEventModel {
                Id = model.Id,
                MonitoringMode = model.MonitoringMode,
                TriggerId = model.TriggerId,
                DiscardNew = model.DiscardNew,
                EventNotifier = model.EventNotifier,
                WhereClause = model.WhereClause.Clone(),
                QueueSize = model.QueueSize,
                BrowsePath = model.BrowsePath,
                SelectClauses = model.SelectClauses?
                    .Select(f => f.Clone())
                    .ToList(),
                ConditionHandling = model.ConditionHandling.Clone(),
                TypeDefinitionId = model.TypeDefinitionId
            };
        }
    }
}