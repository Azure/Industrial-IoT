// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IIoT.OpcUa.Registry.Models {
    using Microsoft.Azure.IIoT.OpcUa.Core.Models;
    using System.Security.Cryptography.X509Certificates;
    using System.Collections.Generic;
    using System.Linq;
    using System;

    /// <summary>
    /// Service model extensions for discovery service
    /// </summary>
    public static class EndpointRegistrationModelEx {

        /// <summary>
        /// Equality comparison
        /// </summary>
        /// <param name="model"></param>
        /// <param name="that"></param>
        /// <returns></returns>
        public static bool IsSameAs(this IEnumerable<EndpointRegistrationModel> model,
            IEnumerable<EndpointRegistrationModel> that) {
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
        public static bool IsSameAs(this EndpointRegistrationModel model,
            EndpointRegistrationModel that) {
            if (model == that) {
                return true;
            }
            if (model == null || that == null) {
                return false;
            }
            return model.Endpoint.HasSameSecurityProperties(that.Endpoint) &&
                model.EndpointUrl == that.EndpointUrl &&
                model.AuthenticationMethods.IsSameAs(that.AuthenticationMethods) &&
                model.SiteId == that.SiteId &&
                model.DiscovererId == that.DiscovererId &&
                model.SupervisorId == that.SupervisorId &&
                model.SecurityLevel == that.SecurityLevel;
        }

        /// <summary>
        /// Deep clone
        /// </summary>
        /// <param name="model"></param>
        /// <returns></returns>
        public static EndpointRegistrationModel Clone(this EndpointRegistrationModel model) {
            if (model == null) {
                return null;
            }
            return new EndpointRegistrationModel {
                Endpoint = model.Endpoint.Clone(),
                EndpointUrl = model.EndpointUrl,
                Id = model.Id,
                AuthenticationMethods = model.AuthenticationMethods?
                    .Select(c => c.Clone()).ToList(),
                SecurityLevel = model.SecurityLevel,
                SiteId = model.SiteId,
                SupervisorId = model.SupervisorId,
                DiscovererId = model.DiscovererId
            };
        }

        /// <summary>
        /// Get security assessment
        /// </summary>
        /// <param name="model"></param>
        public static SecurityAssessment GetSecurityAssessment(
            this EndpointRegistrationModel model) {
            if (model.Endpoint.SecurityMode == SecurityMode.None) {
                return SecurityAssessment.Low;
            }

            if (model.Endpoint.Certificate == null) {
                return SecurityAssessment.Low;
            }

            try {
                using (var cert = new X509Certificate2(model.Endpoint.Certificate)) {
                    var securityProfile = model.Endpoint.SecurityPolicy.Remove(0,
                        model.Endpoint.SecurityPolicy.IndexOf('#') + 1);

                    // TODO

                    var expiryDate = cert.NotAfter;
                    var issuer = cert.Issuer.Extract("CN=", ",");

                    if ((securityProfile == "None") ||
                        (securityProfile == "sha1") ||
                        (cert.PublicKey.Key.KeySize == 1024)) {
                        return SecurityAssessment.Low;
                    }
                    if ((cert.IssuerName.Name == cert.SubjectName.Name) &&
                        (securityProfile != "None")) {
                        return SecurityAssessment.High;
                    }
                    return SecurityAssessment.Medium;
                }
            }
            catch {
                return SecurityAssessment.Low;
            }
        }
    }
}
