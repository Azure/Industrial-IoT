// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IIoT.OpcUa.Api.Vault.Models {
    using System.Runtime.Serialization;
    using System.Collections.Generic;

    /// <summary>
    /// Trust group identifier list model
    /// </summary>
    [DataContract]
    public sealed class TrustGroupListApiModel {

        /// <summary>
        /// Groups
        /// </summary>
        [DataMember(Name = "groups", Order = 0,
            EmitDefaultValue = false)]
        public List<string> Groups { get; set; }

        /// <summary>
        /// Next link
        /// </summary>
        [DataMember(Name = "nextPageLink", Order = 1,
            EmitDefaultValue = false)]
        public string NextPageLink { get; set; }
    }
}
