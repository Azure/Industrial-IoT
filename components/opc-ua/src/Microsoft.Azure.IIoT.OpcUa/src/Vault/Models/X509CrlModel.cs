// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IIoT.OpcUa.Vault.Models {
    using Newtonsoft.Json.Linq;

    /// <summary>
    /// A X509 certificate revocation list.
    /// </summary>
    public sealed class X509CrlModel {

        /// <summary>
        /// The Issuer name of the revocation list.
        /// </summary>
        public string Issuer { get; set; }

        /// <summary>
        /// The X509 certificate revocation list.
        /// </summary>
        public JToken Crl { get; set; }
    }
}
