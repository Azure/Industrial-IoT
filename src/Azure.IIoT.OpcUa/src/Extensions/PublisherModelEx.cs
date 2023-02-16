// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Azure.IIoT.OpcUa.Api.Models {
    using System;

    /// <summary>
    /// Service model extensions for discovery service
    /// </summary>
    public static class PublisherModelEx {

        /// <summary>
        /// Convert a device id and module to publisher id
        /// </summary>
        /// <param name="deviceId"></param>
        /// <param name="moduleId"></param>
        /// <returns></returns>
        public static string CreatePublisherId(string deviceId, string moduleId) {
            return string.IsNullOrEmpty(moduleId) ? deviceId : $"{deviceId}_module_{moduleId}";
        }

        /// <summary>
        /// Returns device id and optional module from publisher id.
        /// </summary>
        /// <param name="publisherId"></param>
        /// <param name="moduleId"></param>
        /// <returns></returns>
        public static string ParseDeviceId(string publisherId, out string moduleId) {
            if (string.IsNullOrEmpty(publisherId)) {
                moduleId = null;
                return null;
            }
            var components = publisherId.Split(new string[] { "_module_" },
                StringSplitOptions.RemoveEmptyEntries);
            if (components.Length == 2) {
                moduleId = components[1];
                return components[0];
            }
            moduleId = null;
            return publisherId;
        }
    }
}
