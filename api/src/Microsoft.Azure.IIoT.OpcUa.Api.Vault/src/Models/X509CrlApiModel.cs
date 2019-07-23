// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IIoT.OpcUa.Api.Vault.Models {
    using Newtonsoft.Json;
    using Newtonsoft.Json.Linq;

    /// <summary>
    /// A X509 certificate revocation list.
    /// </summary>
    public sealed class X509CrlApiModel {

        /// <summary>
        /// The Issuer name of the revocation list.
        /// </summary>
        [JsonProperty(PropertyName = "issuer",
            NullValueHandling = NullValueHandling.Ignore)]
        public string Issuer { get; set; }

        /// <summary>
        /// The certificate revocation list.
        /// </summary>
        [JsonProperty(PropertyName = "crl")]
        public JToken Crl { get; set; }
    }
}
