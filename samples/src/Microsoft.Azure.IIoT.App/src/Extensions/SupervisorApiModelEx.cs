// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IIoT.App.Extensions
{
    using global::Azure.IIoT.OpcUa.Services.Sdk;
    using global::Azure.IIoT.OpcUa.Models;
    using System.Collections.Generic;
    using System.Linq;

    /// <summary>
    /// Handle event
    /// </summary>
    public static class SupervisorModelEx
    {
        /// <summary>
        /// Update a list of supervisors from a received event
        /// </summary>
        /// <param name="results"></param>
        /// <param name="ev"></param>
        public static void Update(this IList<SupervisorModel> results,
            SupervisorEventModel ev)
        {
            var supervisor = results.FirstOrDefault(e => e.Id == ev.Id);
            if (supervisor == null &&
                ev.EventType != SupervisorEventType.New)
            {
                return;
            }
            switch (ev.EventType)
            {
                case SupervisorEventType.New:
                    if (supervisor == null)
                    {
                        // Add if not already in list
                        results.Add(ev.Supervisor);
                    }
                    break;
                case SupervisorEventType.Updated:
                    ev.Supervisor.Patch(supervisor);
                    break;
                case SupervisorEventType.Deleted:
                    results.Remove(supervisor);
                    break;
            }
        }
    }
}
