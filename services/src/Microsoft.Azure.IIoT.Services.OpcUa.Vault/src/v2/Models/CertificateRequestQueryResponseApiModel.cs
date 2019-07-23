// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------


namespace Microsoft.Azure.IIoT.Services.OpcUa.Vault.v2.Models {
    using Microsoft.Azure.IIoT.OpcUa.Vault.Models;
    using Newtonsoft.Json;
    using System.Collections.Generic;
    using System.Linq;

    /// <summary>
    /// Response model
    /// </summary>
    public sealed class CertificateRequestQueryResponseApiModel {

        /// <summary>
        /// Create model
        /// </summary>
        /// <param name="model"></param>
        public CertificateRequestQueryResponseApiModel(CertificateRequestQueryResultModel model) {
            Requests = model.Requests?
                .Select(r => new CertificateRequestRecordApiModel(r))
                .ToList();
            NextPageLink = model.NextPageLink;
        }

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
