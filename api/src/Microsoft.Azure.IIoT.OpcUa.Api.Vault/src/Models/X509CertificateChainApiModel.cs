// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IIoT.OpcUa.Api.Vault.Models {
    using Newtonsoft.Json;
    using System.Collections.Generic;

    /// <summary>
    /// Certificate chain
    /// </summary>
    public sealed class X509CertificateChainApiModel {

        /// <summary>
        /// Chain
        /// </summary>
        [JsonProperty(PropertyName = "chain",
            NullValueHandling = NullValueHandling.Ignore)]
        public List<X509CertificateApiModel> Chain { get; set; }
    }
}
