// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IIoT.OpcUa.Api.Vault.Models {
    using System.Runtime.Serialization;
    using System;


    /// <summary>
    /// Vault operation log model
    /// </summary>
    [DataContract]
    public class VaultOperationContextApiModel {

        /// <summary>
        /// User
        /// </summary>
        [DataMember(Name = "authorityId", Order = 0)]
        public string AuthorityId { get; set; }

        /// <summary>
        /// Operation time
        /// </summary>
        [DataMember(Name = "time", Order = 1)]
        public DateTime Time { get; set; }
    }
}

