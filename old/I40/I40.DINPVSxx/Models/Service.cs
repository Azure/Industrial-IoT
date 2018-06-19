// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace I40.DINPVSxx.Models {
    using Newtonsoft.Json;
    using System.Collections.Generic;

    /// <summary>
    /// A service is a set of operations.  Service realizes
    /// a <see cref="Skill"/> and thus is a property
    /// </summary>
    public class Service : Skill {

        /// <summary>
        /// [0..*] Operations performed by service in order
        /// </summary>
        [JsonProperty(PropertyName = "operation",
            NullValueHandling = NullValueHandling.Ignore)]
        public List<Operation> Performs { get; set; }

        /// <summary>
        /// [0..*] Skills realized by service.
        /// </summary>
        [JsonProperty(PropertyName = "realizes",
            NullValueHandling = NullValueHandling.Ignore)]
        public List<Skill> Realizes { get; set; }

        // TODO: attribute
        // TODO: Marked as ref
    }
}