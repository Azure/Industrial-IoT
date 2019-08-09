// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IIoT.OpcUa.Registry.Models {
    using Newtonsoft.Json.Linq;
    using System.Collections.Generic;
    using System.Linq;

    /// <summary>
    /// Endpoint model extensions
    /// </summary>
    public static class EndpointModelEx {

        /// <summary>
        /// Equality comparison
        /// </summary>
        /// <param name="model"></param>
        /// <param name="that"></param>
        /// <returns></returns>
        public static bool IsSameAs(this EndpointModel model, EndpointModel that) {
            if (model == that) {
                return true;
            }
            if (model == null || that == null) {
                return false;
            }
            if (!that.HasSameSecurityProperties(model)) {
                return false;
            }
            if (!that.GetAllUrls().SequenceEqualsSafe(model.GetAllUrls())) {
                return false;
            }
            return true;
        }

        /// <summary>
        /// Equality comparison
        /// </summary>
        /// <param name="model"></param>
        /// <param name="that"></param>
        /// <returns></returns>
        public static bool HasSameSecurityProperties(this EndpointModel model, EndpointModel that) {
            if (model == that) {
                return true;
            }
            if (model == null || that == null) {
                return false;
            }
            if (!that.User.IsSameAs(model.User)) {
                return false;
            }
            if (!that.Certificate.SequenceEqualsSafe(model.Certificate)) {
                return false;
            }
            if (that.SecurityPolicy!= model.SecurityPolicy) {
                return false;
            }
            if ((that.SecurityMode ?? SecurityMode.Best) !=
                    (model.SecurityMode ?? SecurityMode.Best)) {
                return false;
            }
            return true;
        }

        /// <summary>
        /// Create unique hash
        /// </summary>
        /// <param name="endpoint"></param>
        /// <returns></returns>
        public static int CreateConsistentHash(this EndpointModel endpoint) {
            var hashCode = -1971667340;
            hashCode = (hashCode * -1521134295) +
                endpoint.GetAllUrls().SequenceGetHashSafe();
            hashCode = (hashCode * -1521134295) +
                endpoint.Certificate.SequenceGetHashSafe();
            hashCode = (hashCode * -1521134295) +
                EqualityComparer<string>.Default.GetHashCode(endpoint.SecurityPolicy);
            hashCode = (hashCode * -1521134295) +
                EqualityComparer<SecurityMode?>.Default.GetHashCode(
                    endpoint.SecurityMode ?? SecurityMode.Best);
            hashCode = (hashCode * -1521134295) +
                EqualityComparer<CredentialType?>.Default.GetHashCode(
                    endpoint.User?.Type ?? CredentialType.None);
            hashCode = (hashCode * -1521134295) +
                JToken.EqualityComparer.GetHashCode(endpoint.User?.Value);
            return hashCode;
        }

        /// <summary>
        /// Get all urls
        /// </summary>
        /// <param name="model"></param>
        /// <returns></returns>
        public static IEnumerable<string> GetAllUrls(this EndpointModel model) {
            if (model == null) {
                return null;
            }
            var all = model.Url.YieldReturn();
            if (model.AlternativeUrls != null) {
                all = all.Concat(model.AlternativeUrls);
            }
            return all;
        }

        /// <summary>
        /// Create Union with endpoint
        /// </summary>
        /// <param name="endpoint"></param>
        /// <param name="model"></param>
        public static void UnionWith(this EndpointModel model,
            EndpointModel endpoint) {
            if (endpoint == null) {
                return;
            }

            if (model.AlternativeUrls == null) {
                model.AlternativeUrls = endpoint.AlternativeUrls;
            }
            else {
                model.AlternativeUrls = model.AlternativeUrls.MergeWith(
                    endpoint.AlternativeUrls);
            }
            if (model.Url == null) {
                model.Url = endpoint.Url;
            }
            else {
                model.AlternativeUrls.Add(endpoint.Url);
            }
            model.AlternativeUrls.Remove(model.Url);
        }

        /// <summary>
        /// Deep clone
        /// </summary>
        /// <param name="model"></param>
        /// <returns></returns>
        public static EndpointModel Clone(this EndpointModel model) {
            if (model == null) {
                return null;
            }
            return new EndpointModel {
                Certificate = model.Certificate?.ToArray(),
                AlternativeUrls = model.AlternativeUrls.ToHashSetSafe(),
                SecurityMode = model.SecurityMode,
                SecurityPolicy = model.SecurityPolicy,
                User = model.User.Clone(),
                Url = model.Url
            };
        }
    }
}
