// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace I40.DINPVSxx.Models {
    using Newtonsoft.Json;
    using System.Collections.Generic;
    using System.ComponentModel.DataAnnotations;

    /// <summary>
    /// Operation property of a service
    /// </summary>
    public class Operation : Property {

        /// <summary>
        /// Endpoint
        /// </summary>
        [JsonProperty(PropertyName = "endpoint")]
        [Required]
        public Endpoint Endpoint { get; set; }

        /// <summary>
        /// [0..*] input parameters
        /// </summary>
        [JsonProperty(PropertyName = "in",
            NullValueHandling = NullValueHandling.Ignore,
            ItemIsReference = false)]
        public List<OperationParameter> In { get; set; }
        // TODO: Marked as ref

        /// <summary>
        /// [0..*] output parameters
        /// </summary>
        [JsonProperty(PropertyName = "out",
            NullValueHandling = NullValueHandling.Ignore,
            ItemIsReference = false)]
        public List<OperationParameter> Out { get; set; }
        // TODO: Marked as ref

        /// <summary>
        /// [0..*] Operations called in order
        /// </summary>
        [JsonProperty(PropertyName = "calls",
            NullValueHandling = NullValueHandling.Ignore)]
        public List<Operation> Calls { get; set; }
        // TODO: Marked as ref
    }
}