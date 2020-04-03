// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------


namespace Microsoft.Azure.IIoT.OpcUa.Api.Vault.Models {
    using System.Runtime.Serialization;
    using System.Collections.Generic;

    /// <summary>
    /// Response model
    /// </summary>
    [DataContract]
    public sealed class CertificateRequestQueryResponseApiModel {

        /// <summary>
        /// The query result.
        /// </summary>
        [DataMember(Name = "requests", Order = 0,
            EmitDefaultValue = false)]
        public List<CertificateRequestRecordApiModel> Requests { get; set; }

        /// <summary>
        /// Link to the next page of results.
        /// </summary>
        [DataMember(Name = "nextPageLink", Order = 1,
            EmitDefaultValue = false)]
        public string NextPageLink { get; set; }
    }
}
