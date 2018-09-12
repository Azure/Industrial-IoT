// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IIoT.OpcUa.Models {
    using System.Collections.Generic;
    using System.Linq;
    using System;

    /// <summary>
    /// Service model extensions for discovery service
    /// </summary>
    public static class ApplicationInfoModelEx {

        /// <summary>
        /// Create unique application id
        /// </summary>
        /// <param name="model"></param>
        /// <returns></returns>
        public static string CreateApplicationId(ApplicationInfoModel model) =>
            CreateApplicationId(model.SiteId, model.ApplicationUri,
                model.ApplicationType);

        /// <summary>
        /// Create unique application id
        /// </summary>
        /// <param name="siteOrSupervisorId"></param>
        /// <param name="applicationUri"></param>
        /// <param name="applicationType"></param>
        /// <returns></returns>
        public static string CreateApplicationId(string siteOrSupervisorId,
            string applicationUri, ApplicationType? applicationType) {
            if (string.IsNullOrEmpty(applicationUri)) {
                return null;
            }
            applicationUri = applicationUri.ToLowerInvariant();
            var type = applicationType ?? ApplicationType.Server;
            var id =$"{siteOrSupervisorId ?? ""}-{type}-{applicationUri}";
            var prefix = applicationType == ApplicationType.Client ? "uac" : "uas";
            return prefix + id.ToSha1Hash();
        }

        /// <summary>
        /// Equality comparison
        /// </summary>
        /// <param name="model"></param>
        /// <param name="that"></param>
        /// <returns></returns>
        public static bool IsSameAs(this IEnumerable<ApplicationInfoModel> model,
            IEnumerable<ApplicationInfoModel> that) {
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
        public static bool IsSameAs(this ApplicationInfoModel model,
            ApplicationInfoModel that) {
            if (model == that) {
                return true;
            }
            if (model == null || that == null) {
                return false;
            }
            return
                that.ApplicationUri == model.ApplicationUri &&
                that.ApplicationType == model.ApplicationType;
        }

        /// <summary>
        /// Deep clone
        /// </summary>
        /// <param name="model"></param>
        /// <returns></returns>
        public static ApplicationInfoModel Clone(this ApplicationInfoModel model) {
            if (model == null) {
                return null;
            }
            return new ApplicationInfoModel {
                ApplicationId = model.ApplicationId,
                ApplicationName = model.ApplicationName,
                ApplicationType = model.ApplicationType,
                ApplicationUri = model.ApplicationUri,
                Capabilities = model.Capabilities.ToHashSetSafe(),
                Certificate = model.Certificate,
                DiscoveryProfileUri = model.DiscoveryProfileUri,
                HostAddresses = model.HostAddresses.ToHashSetSafe(),
                DiscoveryUrls = model.DiscoveryUrls.ToHashSetSafe(),
                NotSeenSince = model.NotSeenSince,
                ProductUri = model.ProductUri,
                SiteId = model.SiteId,
                SupervisorId = model.SupervisorId
            };
        }
    }
}
