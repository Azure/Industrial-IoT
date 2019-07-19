// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IIoT.OpcUa.Api.Vault.Models {
    using Newtonsoft.Json;
    using System.Collections.Generic;

    /// <summary>
    /// Certificate list
    /// </summary>
    public sealed class X509CertificateListApiModel {

        /// <summary>
        /// Certificates
        /// </summary>
        [JsonProperty(PropertyName = "certificates",
            NullValueHandling = NullValueHandling.Ignore)]
        public List<X509CertificateApiModel> Certificates { get; set; }

        /// <summary>
        /// Next link
        /// </summary>
        [JsonProperty(PropertyName = "nextPageLink",
            NullValueHandling = NullValueHandling.Ignore)]
        public string NextPageLink { get; set; }
    }
}
