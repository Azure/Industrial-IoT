// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IIoT.OpcUa.Vault.Models {
    using Microsoft.Azure.IIoT.OpcUa.Core.Models;

    /// <summary>
    /// Certificate request model
    /// </summary>
    public sealed class CertificateRequestModel {

        /// <summary>
        /// Numeric index
        /// </summary>
        public uint? Index { get; set; }

        /// <summary>
        /// Public record
        /// </summary>
        public CertificateRequestRecordModel Record { get; set; }

        /// <summary>
        /// Entity info
        /// </summary>
        public EntityInfoModel Entity { get; set; }

        /// <summary>
        /// Signing request
        /// </summary>
        public byte[] SigningRequest { get; set; }

        /// <summary>
        /// Resulting certificate
        /// </summary>
        public X509CertificateModel Certificate { get; set; }

        /// <summary>
        /// Optional private key handle
        /// </summary>
        public byte[] KeyHandle { get; set; }
    }
}
