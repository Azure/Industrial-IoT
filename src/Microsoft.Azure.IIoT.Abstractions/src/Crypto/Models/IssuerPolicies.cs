// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IIoT.Crypto.Models {
    using System;

    /// <summary>
    /// Certificate issuer policies
    /// </summary>
    public sealed class IssuerPolicies {

        /// <summary>
        /// The signature type to use for signing.
        /// </summary>
        public SignatureType? SignatureType { get; set; }

        /// <summary>
        /// The lifetime of any issued certificates.
        /// </summary>
        public TimeSpan? IssuedLifetime { get; set; }
    }
}
