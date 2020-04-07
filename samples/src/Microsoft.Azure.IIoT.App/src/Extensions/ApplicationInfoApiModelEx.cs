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
    public static class ApplicationInfoApiModelEx {

        /// <summary>
        /// Update a list of applications from a received event
        /// </summary>
        /// <param name="results"></param>
        /// <param name="ev"></param>
        public static void Update(this IList<ApplicationInfoApiModel> results,
            ApplicationEventApiModel ev) {
            var application = results.FirstOrDefault(e => e.ApplicationId == ev.Id);
            if (application == null &&
                ev.EventType != ApplicationEventType.New &&
                ev.EventType != ApplicationEventType.Enabled) {
                return;
            }
            switch (ev.EventType) {
                case ApplicationEventType.New:
                case ApplicationEventType.Enabled:
                    if (application == null) {
                        // Add if not already in list
                        results.Add(ev.Application);
                    }
                    break;
                case ApplicationEventType.Updated:
                    ev.Application.Patch(application);
                    break;
                case ApplicationEventType.Disabled:
                case ApplicationEventType.Deleted:
                    results.Remove(application);
                    break;
            }
        }
    }
}
