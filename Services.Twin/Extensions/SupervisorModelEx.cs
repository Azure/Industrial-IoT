// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IIoT.OpcTwin.Services.Models {
    using System;

    /// <summary>
    /// Service model extensions for discovery service
    /// </summary>
    public static class SupervisorModelEx {

        /// <summary>
        /// Convert a device id and module to supervisor id
        /// </summary>
        /// <param name="deviceId"></param>
        /// <param name="moduleId"></param>
        /// <returns></returns>
        public static string CreateSupervisorId(string deviceId, string moduleId) =>
            string.IsNullOrEmpty(moduleId) ? deviceId : $"{deviceId}_module_{moduleId}";

        /// <summary>
        /// Returns device id and optional module from supervisor id.
        /// </summary>
        /// <param name="supervisorId"></param>
        /// <param name="moduleId"></param>
        /// <returns></returns>
        public static string ParseDeviceId(string supervisorId, out string moduleId) {
            var components = supervisorId.Split(new string[] { "_module_" },
                StringSplitOptions.RemoveEmptyEntries);
            if (components.Length == 2) {
                moduleId = components[1];
                return components[0];
            }
            moduleId = null;
            return supervisorId;
        }
    }
}
