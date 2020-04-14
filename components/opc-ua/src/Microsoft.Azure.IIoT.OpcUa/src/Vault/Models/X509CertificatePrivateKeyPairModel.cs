// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IIoT.OpcUa.Vault.Models {
    using Microsoft.Azure.IIoT.OpcUa.Core.Models;

    /// <summary>
    /// Certificate private key pair model
    /// </summary>
    public sealed class X509CertificatePrivateKeyPairModel {

        /// <summary>
        /// Certificate
        /// </summary>
        public X509CertificateModel Certificate { get; set; }

        /// <summary>
        /// Private key
        /// </summary>
        public PrivateKeyModel PrivateKey { get; set; }
    }
}
