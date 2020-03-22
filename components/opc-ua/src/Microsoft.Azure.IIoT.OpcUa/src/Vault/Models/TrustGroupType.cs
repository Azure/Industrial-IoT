// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IIoT.OpcUa.Vault.Models {
    /// <summary>
    /// Trust group types
    /// </summary>
    public enum TrustGroupType {

        /// <summary>
        /// Application certificate
        /// </summary>
        ApplicationInstanceCertificate,

        /// <summary>
        /// Https certificate type
        /// </summary>
        HttpsCertificate,

        /// <summary>
        /// User credential certificate type
        /// </summary>
        UserCredentialCertificate
    }
}
