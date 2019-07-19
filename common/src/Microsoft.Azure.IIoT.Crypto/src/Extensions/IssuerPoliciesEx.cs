// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IIoT.Crypto {
    using Microsoft.Azure.IIoT.Crypto.Models;
    using System;

    /// <summary>
    /// Issuer policies extensions
    /// </summary>
    public static class IssuerPoliciesEx {

        /// <summary>
        /// Validate issuer policies
        /// </summary>
        /// <param name="policies"></param>
        /// <param name="parent"></param>
        /// <param name="keyParams"></param>
        /// <returns></returns>
        public static IssuerPolicies Validate(this IssuerPolicies policies,
            IssuerPolicies parent = null, CreateKeyParams keyParams = null) {
            if (policies == null) {
                policies = new IssuerPolicies();
            }
            if (policies.IssuedLifetime == null) {
                policies.IssuedLifetime = parent?.IssuedLifetime != null ?
                    parent.IssuedLifetime.Value / 2 : TimeSpan.FromDays(1);
            }
            if (policies.SignatureType == null) {
                policies.SignatureType = keyParams.Type == KeyType.RSA ?
                    SignatureType.RS256 : SignatureType.ES256;
            }
            if (parent != null) {
                if (policies.IssuedLifetime > parent.IssuedLifetime) {
                    throw new ArgumentException(
                        "Issued lifetime cannot be greater than parent issuer policy");
                }
            }
            if (keyParams != null) {
                if (policies.SignatureType.Value.IsRSA() && keyParams.Type != KeyType.RSA) {
                    throw new ArgumentException(
                        "Cannot create rsa signature with mismatch key");
                }
                if (policies.SignatureType.Value.IsECC() && keyParams.Type != KeyType.ECC) {
                    throw new ArgumentException(
                        "Cannot create ecc signature with mismatch key");
                }
            }
            return policies;
        }
    }
}
