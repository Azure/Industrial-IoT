// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace I40.DINPVSxx.Models {
    using I40.Common.Models;
    using Newtonsoft.Json;
    using System.ComponentModel.DataAnnotations;

    /// <summary>
    /// A property representing a triple and allowing to
    /// model ontologies / subject-predicate-object tuples.
    /// Use the qualifier as predicate.  Use the semantics
    /// for reeification.
    /// </summary>
    public class Relationship : Property {

        /// <summary>
        /// Subject
        /// </summary>
        [JsonProperty(PropertyName = "first")]
        [Required]
        public Reference Subject { get; set; }

        /// <summary>
        /// Object
        /// </summary>
        [JsonProperty(PropertyName = "second")]
        [Required]
        public Reference Object { get; set; }

        // TODO: Is the qualifer the predicate?
        // TODO: Can semantics be used for reefication?
        // TODO: Why encode expression logic this way?
        // TODO: first should be subject, second should be object
    }
}