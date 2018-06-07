// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace I40.DIN91345.Models {
    using I40.Common.Models;
    using I40.DIN91345.Models.Access;
    using System.ComponentModel.DataAnnotations;
    using Newtonsoft.Json;
    using System.Collections.Generic;

    /// <summary>
    /// Asset Admin shell
    /// </summary>
    public /* abstract */ class AssetAdministrationShell : BaseAsset,
        IIdentifiable, IHasTemplate, IPackageable {

        /// <summary>
        /// Asset header
        /// </summary>
        [JsonProperty(PropertyName = "header")]
        [Required]
        public Header Header { get; set; }

        /// <summary>
        /// Asset body
        /// </summary>
        [JsonProperty(PropertyName = "body")]
        [Required]
        public Body Body { get; set; }

        /// <summary>
        /// Security attributes
        /// </summary>
        [JsonProperty(PropertyName = "securityAttributes",
            NullValueHandling = NullValueHandling.Ignore)]
        public SecurityAttributes SecurityAttributes { get; set; }

        /// <summary>
        /// [0..*] (Splittable) Relevant dictionaries
        /// Resolve to <see cref="IDictionary"/> in particular
        /// <see cref="PropertyDescriptionDictionary"/> and
        /// <see cref="TemplateDictionary"/> instances.
        /// </summary>
        [JsonProperty(PropertyName = "dictionaries",
            NullValueHandling = NullValueHandling.Ignore)]
        public List<IDictionary> Dictionaries { get; set; }

        /// <summary>
        /// [0..1] The reference to the predecessor AAS.
        /// Constraint AAS-001: if the asset is of
        /// asset_kind = Instance then a reference to an
        /// AAS of asset_kind = Type should be added.
        /// Resolves to <see cref="AssetAdministrationShell"/>
        /// </summary>
        [JsonProperty(PropertyName = "ref_predecessor",
            NullValueHandling = NullValueHandling.Ignore)]
        public Reference Predecessor { get; set; }
    }
}