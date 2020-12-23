// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IIoT.App.Data {
    using Microsoft.Azure.IIoT.OpcUa.Api.Registry.Models;
    using System.Collections.Generic;
    using System.Linq;

    /// <summary>
    /// Handle event
    /// </summary>
    public static class PublisherApiModelEx {

        /// <summary>
        /// Update a list of publishers from a received event
        /// </summary>
        /// <param name="results"></param>
        /// <param name="ev"></param>
        public static void Update(this IList<PublisherApiModel> results,
            PublisherEventApiModel ev) {
            var publisher = results.FirstOrDefault(e => e.Id == ev.Id);
            if (publisher == null &&
                ev.EventType != PublisherEventType.New) {
                return;
            }
            switch (ev.EventType) {
                case PublisherEventType.New:
                    if (publisher == null) {
                        // Add if not already in list
                        results.Add(ev.Publisher);
                    }
                    break;
                case PublisherEventType.Updated:
                    ev.Publisher.Patch(publisher);
                    break;
                case PublisherEventType.Deleted:
                    results.Remove(publisher);
                    break;
            }
        }
    }
}
