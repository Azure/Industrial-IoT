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

        /// <summary>
        /// Equality comparison
        /// </summary>
        /// <param name="model"></param>
        /// <param name="that"></param>
        /// <returns></returns>
        public static bool IsSameAs(this IEnumerable<PublisherModel> model,
            IEnumerable<PublisherModel> that) {
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
        public static bool IsSameAs(this PublisherModel model,
            PublisherModel that) {
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
        public static PublisherModel Clone(this PublisherModel model) {
            if (model == null) {
                return null;
            }
            return new PublisherModel {
                LogLevel = model.LogLevel,
                Connected = model.Connected,
                Id = model.Id,
                OutOfSync = model.OutOfSync,
                SiteId = model.SiteId
            };
        }
    }
}
