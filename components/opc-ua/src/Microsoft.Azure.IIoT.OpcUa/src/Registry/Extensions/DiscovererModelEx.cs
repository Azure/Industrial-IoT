// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IIoT.OpcUa.Registry.Models {
    using System;
    using System.Collections.Generic;
    using System.Linq;

    /// <summary>
    /// Service model extensions for discovery service
    /// </summary>
    public static class DiscovererModelEx {

        /// <summary>
        /// Convert a device id and module to discoverer id
        /// </summary>
        /// <param name="deviceId"></param>
        /// <param name="moduleId"></param>
        /// <returns></returns>
        public static string CreateDiscovererId(string deviceId, string moduleId) {
            return string.IsNullOrEmpty(moduleId) ? deviceId : $"{deviceId}_module_{moduleId}";
        }

        /// <summary>
        /// Returns device id and optional module from discoverer id.
        /// </summary>
        /// <param name="discovererId"></param>
        /// <param name="moduleId"></param>
        /// <returns></returns>
        public static string ParseDeviceId(string discovererId, out string moduleId) {
            if (string.IsNullOrEmpty(discovererId)) {
                moduleId = null;
                return null;
            }
            var components = discovererId.Split(new string[] { "_module_" },
                StringSplitOptions.RemoveEmptyEntries);
            if (components.Length == 2) {
                moduleId = components[1];
                return components[0];
            }
            moduleId = null;
            return discovererId;
        }

        /// <summary>
        /// Equality comparison
        /// </summary>
        /// <param name="model"></param>
        /// <param name="that"></param>
        /// <returns></returns>
        public static bool IsSameAs(this IEnumerable<DiscovererModel> model,
            IEnumerable<DiscovererModel> that) {
            if (model == that) {
                return true;
            }
            if (model == null || that == null) {
                return false;
            }
            if (model.Count() != that.Count()) {
                return false;
            }
            return model.All(a => that.Any(b => b.IsSameAs(a)));
        }

        /// <summary>
        /// Equality comparison
        /// </summary>
        /// <param name="model"></param>
        /// <param name="that"></param>
        /// <returns></returns>
        public static bool IsSameAs(this DiscovererModel model,
            DiscovererModel that) {
            if (model == that) {
                return true;
            }
            if (model == null || that == null) {
                return false;
            }
            return that.Id == model.Id;
        }

        /// <summary>
        /// Deep clone
        /// </summary>
        /// <param name="model"></param>
        /// <returns></returns>
        public static DiscovererModel Clone(this DiscovererModel model) {
            if (model == null) {
                return null;
            }
            return new DiscovererModel {
                Connected = model.Connected,
                Discovery = model.Discovery,
                RequestedMode = model.RequestedMode,
                DiscoveryConfig = model.DiscoveryConfig.Clone(),
                RequestedConfig = model.RequestedConfig.Clone(),
                Id = model.Id,
                OutOfSync = model.OutOfSync,
                SiteId = model.SiteId
            };
        }
    }
}
