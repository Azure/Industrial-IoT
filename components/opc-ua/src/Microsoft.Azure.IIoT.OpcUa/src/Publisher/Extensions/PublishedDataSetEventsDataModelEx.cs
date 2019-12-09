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
        public static PublishedDataSetEventsModel Clone(this PublishedDataSetEventsModel model) {
            if (model == null) {
                return null;
            }
            return new PublishedDataSetEventsModel {
                Id = model.Id,
                MonitoringMode = model.MonitoringMode,
                TriggerId = model.TriggerId,
                DiscardNew = model.DiscardNew,
                EventNotifier = model.EventNotifier,
                Filter = model.Filter.Clone(),
                QueueSize = model.QueueSize,
                BrowsePath = model.BrowsePath,
                SelectedFields = model.SelectedFields?
                    .Select(f => f.Clone())
                    .ToList()
            };
        }
    }
}