// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IIoT.Crypto {
    using Microsoft.Azure.IIoT.Crypto.Models;

    /// <summary>
    /// Issuer policies extensions
    /// </summary>
    public static class IssuerPoliciesEx {

        /// <summary>
        /// Clone issuer policies
        /// </summary>
        /// <param name="policies"></param>
        /// <returns></returns>
        public static IssuerPolicies Clone(this IssuerPolicies policies) {
            if (policies == null) {
                return null;
            }
            return new IssuerPolicies {
                IssuedLifetime = policies.IssuedLifetime,
                SignatureType = policies.SignatureType
            };
        }

        /// <summary>
        /// Compare
        /// </summary>
        /// <param name="policies"></param>
        /// <param name="other"></param>
        /// <returns></returns>
        public static bool SameAs(this IssuerPolicies policies, IssuerPolicies other) {

            if (policies == null) {
                return other == null;
            }
            if (other == null) {
                return false;
            }

            if (policies.IssuedLifetime != other.IssuedLifetime) {
                return false;
            }
            if (policies.SignatureType != other.SignatureType) {
                return false;
            }
            return true;
        }

    }
}

