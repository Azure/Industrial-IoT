// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IIoT.OpcUa.Api.Vault.Models {
    using Newtonsoft.Json;
    using System;


    /// <summary>
    /// Vault operation log model
    /// </summary>
    public class VaultOperationContextApiModel {

        /// <summary>
        /// User
        /// </summary>
        [JsonProperty(PropertyName = "authorityId")]
        public string AuthorityId { get; set; }

        /// <summary>
        /// Operation time
        /// </summary>
        [JsonProperty(PropertyName = "time")]
        public DateTime Time { get; set; }
    }
}

