// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IIoT.OpcUa.Vault.Models {
    using System.Collections.Generic;

    /// <summary>
    /// Certificate request list model
    /// </summary>
    public sealed class CertificateRequestListModel {

        /// <summary>
        /// Requests
        /// </summary>
        public List<CertificateRequestModel> Requests { get; set; }

        /// <summary>
        /// Continuation token
        /// </summary>
        public string NextPageLink { get; set; }
    }
}
