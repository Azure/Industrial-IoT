// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace I40.DINPVSxx.Models {
    using Newtonsoft.Json;
    using System.ComponentModel.DataAnnotations;

    /// <summary>
    /// An endpoint defines how to execute an operation.
    /// </summary>
    public class Endpoint {

        /// <summary>
        /// Adress to be used to execute an operation.
        /// </summary>
        [JsonProperty(PropertyName = "endpointAdress")]
        [Required]
        public string EndpointAddress { get; set; }
    }
}