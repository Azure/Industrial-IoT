// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IIoT.OpcUa.Vault.Models {
    using Microsoft.Azure.IIoT.OpcUa.Core.Models;
    using System.Collections.Generic;

    /// <summary>
    /// Certificate chain list
    /// </summary>
    public sealed class X509CertificateChainListModel {

        /// <summary>
        /// Certificate collection
        /// </summary>
        public List<X509CertificateChainModel> Chains { get; set; }

        /// <summary>
        /// Next link
        /// </summary>
        public string NextPageLink { get; set; }
    }
}
