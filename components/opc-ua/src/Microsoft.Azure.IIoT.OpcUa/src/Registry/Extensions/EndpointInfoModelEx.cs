// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IIoT.OpcUa.Registry.Models {
    using Microsoft.Azure.IIoT.OpcUa.Core.Models;
    using System.Collections.Generic;
    using System.Linq;
    using System;

    /// <summary>
    /// Service model extensions for discovery service
    /// </summary>
    public static class EndpointInfoModelEx {

        /// <summary>
        /// Create unique endpoint
        /// </summary>
        /// <param name="applicationId"></param>
        /// <param name="url"></param>
        /// <param name="mode"></param>
        /// <param name="securityPolicy"></param>
        /// <returns></returns>
        public static string CreateEndpointId(string applicationId, string url,
            SecurityMode? mode, string securityPolicy) {
            if (applicationId == null || url == null) {
                return null;
            }

            url = url.ToLowerInvariant();

            if (!mode.HasValue || mode.Value == SecurityMode.None) {
                mode = SecurityMode.Best;
            }
            securityPolicy = securityPolicy?.ToLowerInvariant() ?? "";

            var id = $"{url}-{applicationId}-{mode}-{securityPolicy}";
            return "uat" + id.ToSha1Hash();
        }

        /// <summary>
        /// Checks whether the identifier is an endpoint id
        /// </summary>
        /// <param name="endpointId"></param>
        /// <returns></returns>
        public static bool IsEndpointId(string endpointId) {
            if (string.IsNullOrWhiteSpace(endpointId)) {
                return false;
            }
            if (!endpointId.StartsWith("uat")) {
                return false;
            }
            return endpointId.Substring(3).IsBase16();
        }

        /// <summary>
        /// Equality comparison
        /// </summary>
        /// <param name="model"></param>
        /// <param name="that"></param>
        /// <returns></returns>
        public static bool IsSameAs(this IEnumerable<EndpointInfoModel> model,
            IEnumerable<EndpointInfoModel> that) {
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
        public static bool IsSameAs(this EndpointInfoModel model,
            EndpointInfoModel that) {
            if (model == that) {
                return true;
            }
            if (model == null || that == null) {
                return false;
            }
            return
                that.Registration.IsSameAs(model.Registration);
        }

        /// <summary>
        /// Is activated
        /// </summary>
        /// <param name="model"></param>
        /// <returns></returns>
        public static bool IsTwinActivated(this EndpointInfoModel model) {
            if (model == null) {
                return false;
            }
            return
                model.ActivationState == EndpointActivationState.Activated ||
                model.ActivationState == EndpointActivationState.ActivatedAndConnected;
        }

        /// <summary>
        /// Is connected
        /// </summary>
        /// <param name="model"></param>
        /// <returns></returns>
        public static bool IsTwinConnected(this EndpointInfoModel model) {
            if (model == null) {
                return false;
            }
            return
                model.ActivationState == EndpointActivationState.ActivatedAndConnected;
        }

        /// <summary>
        /// Deep clone
        /// </summary>
        /// <param name="model"></param>
        /// <returns></returns>
        public static EndpointInfoModel Clone(this EndpointInfoModel model) {
            if (model == null) {
                return null;
            }
            return new EndpointInfoModel {
                ApplicationId = model.ApplicationId,
                OutOfSync = model.OutOfSync,
                Registration = model.Registration.Clone()
            };
        }
    }
}
