// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IIoT.App.Data {
    using Microsoft.Azure.IIoT.App.Models;
    using Microsoft.Azure.IIoT.OpcUa.Api.Registry.Models;
    using System.Collections.Generic;
    using System.Linq;

    /// <summary>
    /// Handle event
    /// </summary>
    public static class DiscovererInfoEx {

        /// <summary>
        /// Update a list of discoverers from a received event
        /// </summary>
        /// <param name="results"></param>
        /// <param name="ev"></param>
        public static void Update(this IList<DiscovererInfo> results,
            DiscovererEventApiModel ev) {
            var discoverer = results.FirstOrDefault(e => e.DiscovererModel.Id == ev.Id);
            if (discoverer == null &&
                ev.EventType != DiscovererEventType.New) {
                return;
            }
            switch (ev.EventType) {
                case DiscovererEventType.New:
                    if (discoverer == null) {
                        // Add if not already in list
                        results.Add(new DiscovererInfo {
                            DiscovererModel = ev.Discoverer
                        });
                    }
                    break;
                case DiscovererEventType.Updated:
                    ev.Discoverer.Patch(discoverer.DiscovererModel);
                    break;
                case DiscovererEventType.Deleted:
                    results.Remove(discoverer);
                    break;
            }
        }
    }
}
