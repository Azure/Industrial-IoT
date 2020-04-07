// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IIoT.OpcUa.Api.Registry.Models {

    /// <summary>
    /// Handle event
    /// </summary>
    public static class SupervisorApiModelEx {

        /// <summary>
        /// Update a discover
        /// </summary>
        /// <param name="supervisor"></param>
        /// <param name="update"></param>
        public static SupervisorApiModel Patch(this SupervisorApiModel update,
            SupervisorApiModel supervisor) {
            if (update == null) {
                return supervisor;
            }
            if (supervisor == null) {
                supervisor = new SupervisorApiModel();
            }
            supervisor.Connected = update.Connected;
            supervisor.Id = update.Id;
            supervisor.LogLevel = update.LogLevel;
            supervisor.OutOfSync = update.OutOfSync;
            supervisor.SiteId = update.SiteId;
            return supervisor;
        }
    }
}
