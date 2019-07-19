// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------


namespace Microsoft.Azure.IIoT.OpcUa.Api.Vault.Models {
    using Newtonsoft.Json;
    using System.Collections.Generic;

    /// <summary>
    /// Response model
    /// </summary>
    public sealed class CertificateRequestQueryResponseApiModel {

        /// <summary>
        /// The query result.
        /// </summary>
        [JsonProperty(PropertyName = "requests",
            NullValueHandling = NullValueHandling.Ignore)]
        public List<CertificateRequestRecordApiModel> Requests { get; set; }

        /// <summary>
        /// Link to the next page of results.
        /// </summary>
        [JsonProperty(PropertyName = "nextPageLink",
            NullValueHandling = NullValueHandling.Ignore)]
        public string NextPageLink { get; set; }
    }
}
