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
        /// <param name="isPatch"></param>
        public static SupervisorApiModel Patch(this SupervisorApiModel update,
            SupervisorApiModel supervisor, bool isPatch = false) {
            if (supervisor == null) {
                return update;
            }
            if (!isPatch || update.Connected != null) {
                supervisor.Connected = update.Connected;
            }
            if (!isPatch || update.Id != null) {
                supervisor.Id = update.Id;
            }
            if (!isPatch || update.LogLevel != null) {
                supervisor.LogLevel = update.LogLevel;
            }
            if (!isPatch || update.OutOfSync != null) {
                supervisor.OutOfSync = update.OutOfSync;
            }
            if (!isPatch || update.SiteId != null) {
                supervisor.SiteId = update.SiteId;
            }
            return supervisor;
        }
    }
}
