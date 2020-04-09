// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IIoT.OpcUa.Vault.Models {
    /// <summary>
    /// The certificate request type.
    /// </summary>
    public enum CertificateRequestType {

        /// <summary>
        /// Signing request
        /// </summary>
        SigningRequest,

        /// <summary>
        /// Key pair request
        /// </summary>
        KeyPairRequest,
    }
}

