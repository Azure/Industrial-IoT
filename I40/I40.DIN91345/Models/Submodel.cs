// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace I40.DIN91345.Models {
    using I40.DINPVSxx.Models;
    using I40.Common.Models;
    using Newtonsoft.Json;
    using System.Collections.Generic;
    using System.ComponentModel.DataAnnotations;

    /// <summary>
    /// Submodels collate properties, data and functions for an industrial
    /// domain [4] or of a specific aspect of the asset(s) represented
    /// by the AAS.
    ///
    /// Submodels are used to structure the information and technical
    /// functionality of an Administration shell into distinguishable
    /// parts. Each submodel refers to a well-defined domain or subject
    /// matter. Submodels can become standardized and thus become
    /// submodel types.
    ///
    /// From the point of view of an accessing system, they initially
    /// provide a hierarchy of properties which can then refer to further
    /// data and functions (within or outside the Administration shell).
    /// A submodel can be qualified (qualifier).
    /// </summary>
    public class Submodel : BaseModel,
        IHasSemanticId, IHasTemplate, IIdentifiable, IQualifiable, ITypable {

        /// <summary>
        /// A submodel may represent either a submodel type
        /// (kind=Type) or a submodel instance (kind=Instance).
        /// </summary>
        [JsonProperty(PropertyName = "kind")]
        [Required]
        public Kind Kind { get; set; }

        /// <summary>
        /// [0..*] A submodel consists of typically zero or more properties
        /// which can be grouped using <see cref="PropertyContainer"/>s.
        /// </summary>
        [JsonProperty(PropertyName = "property",
            NullValueHandling = NullValueHandling.Ignore)]
        public List<Property> Properties { get; set; }
    }
}