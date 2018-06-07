// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace I40.DINPVSxx.Models {
    using I40.Common.Models;
    using Newtonsoft.Json;
    using System.ComponentModel;
    using System.ComponentModel.DataAnnotations;

    /// <summary>
    /// A property describes a characteristic of an element.
    /// It is a defined parameter suitable for the descriptions and
    /// differenations of assets. [compare to SOURCE: ISO/IEC Guide 77-2]
    ///
    /// NOTE: The concept of type and instance applies to properties.
    /// The property types are defined in dictionaries (like the IEC
    /// Common Data Dictionary or ecl @ss), they do not have a value.
    /// The property type is also called data element type in some standards.
    /// The property instances typically have a value. A property
    /// instance is also called property-value pair in certain standards.
    ///
    /// HasSemantics has [0..1] reference to a property description
    /// within the same or another AAS and resolves to a
    /// <see cref="PropertyDescription"/> from a package dictionary
    ///
    /// Constraint AAS-008: In case of a property instance either a
    /// reference to an internal property description or a semantic
    /// reference (id_semantics) to an external property
    /// definition (like e.g.eCl @ss) should be provided.
    ///
    /// Constraint AAS-009: In case of a property type either a
    /// reference to an internal property description or a semantic
    /// reference (id_semantics) to an external property
    /// definition (like e.g.eCl @ss) shall be provided.
    ///
    /// Constraint AAS-013: If a property has a reference to an internal
    /// property as well as to an external property (id_semantics) then
    /// the id of the external property has to be identical to the semantic
    /// reference within the referenced internal PropertyDescription.
    /// </summary>
    public abstract class Property : BaseContainer, ITypable,
        IQualifiable, IHasTemplate, IHasSemanticId {

        /// <summary>
        /// Whether the property is a property statement.
        /// Alternative is category.
        /// </summary>
        [JsonProperty(PropertyName = "propertyStatement",
            NullValueHandling = NullValueHandling.Ignore)]
        [DefaultValue(false)]
        public bool IsPropertyStatement { get; set; }

        /// <summary>
        /// Kind denotes whether the property represents a property type
        /// (kind=Type) or a property instance (kind=Instance).
        /// </summary>
        [JsonProperty(PropertyName = "kind")]
        [Required]
        public Kind Kind { get; set; }

        /// <inheritdoc/>
        [JsonProperty(PropertyName = "qualifier",
            NullValueHandling = NullValueHandling.Ignore)]
        public Qualifier Qualifier { get; set; }

        /// <summary>
        /// HasSemantics has [0..1] reference to a property description
        /// within the same or another AAS and resolves to a
        /// <see cref="PropertyDescription"/> from a package dictionary
        /// or to a global semantic object identified by the reference
        /// id.
        /// </summary>
        ///
        /// <inheritdoc/>
        [JsonProperty(PropertyName = "id_semantics",
            NullValueHandling = NullValueHandling.Ignore)]
        public Reference Semantics { get; set; }
    }
}