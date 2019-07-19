// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IIoT.OpcUa.Api.Vault.Models {
    using Newtonsoft.Json;

    /// <summary>
    /// Finish request results
    /// </summary>
    public sealed class FinishSigningRequestResponseApiModel {

        /// <summary>
        /// Request
        /// </summary>
        [JsonProperty(PropertyName = "request",
            NullValueHandling = NullValueHandling.Ignore)]
        public CertificateRequestRecordApiModel Request { get; set; }

        /// <summary>
        /// Signed certificate
        /// </summary>
        [JsonProperty(PropertyName = "certificate",
            NullValueHandling = NullValueHandling.Ignore)]
        public X509CertificateApiModel Certificate { get; set; }
    }
}
