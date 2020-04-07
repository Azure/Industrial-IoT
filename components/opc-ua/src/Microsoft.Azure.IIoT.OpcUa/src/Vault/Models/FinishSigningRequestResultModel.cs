// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IIoT.OpcUa.Vault.Models {
    using Microsoft.Azure.IIoT.OpcUa.Core.Models;

    /// <summary>
    /// Finish signing request result
    /// </summary>
    public sealed class FinishSigningRequestResultModel {

        /// <summary>
        /// Certificate type
        /// </summary>
        public CertificateRequestRecordModel Request { get; set; }

        /// <summary>
        /// Signed cert
        /// </summary>
        public X509CertificateModel Certificate { get; set; }
    }
}

