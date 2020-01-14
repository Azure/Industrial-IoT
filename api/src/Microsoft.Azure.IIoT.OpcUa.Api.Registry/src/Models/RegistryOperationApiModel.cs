// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IIoT.OpcUa.Api.Registry.Models {
    using Newtonsoft.Json;
    using System.ComponentModel.DataAnnotations;
    using System;

    /// <summary>
    /// Registry operation log model
    /// </summary>
    public class RegistryOperationApiModel {

        /// <summary>
        /// Operation User
        /// </summary>
        [JsonProperty(PropertyName = "authorityId")]
        [Required]
        public string AuthorityId { get; set; }

        /// <summary>
        /// Operation time
        /// </summary>
        [JsonProperty(PropertyName = "time")]
        [Required]
        public DateTime Time { get; set; }
    }
}

