// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace I40.DIN91345.Models {
    using I40.Common.Models;
    using Newtonsoft.Json;
    using System.ComponentModel.DataAnnotations;

    /// <summary>
    /// Asset
    /// </summary>
    public class Asset : BaseAsset, IIdentifiable, ITypable, IHasTemplate {

        /// <summary>
        /// Kind_asset denotes whether the AAS represents
        /// an asset type (kind_asset=Type) or an asset
        /// instance (kind_asset=Instance).
        /// </summary>
        [JsonProperty(PropertyName = "kind")]
        [Required]
        public Kind Kind { get; set; }

        /// <summary>
        /// [0..1] A reference to a Submodel that describes
        /// meta data w.r.t. to the asset represented by the AAS.
        /// Main purpose of this model is the definition of the
        /// handling of additional identifiers for the asset
        /// like e.g.serial number etc.
        /// Resolve to <see cref="Submodel"/>
        /// </summary>
        [JsonProperty(PropertyName = "ref_assetIdentificationModel",
            NullValueHandling = NullValueHandling.Ignore)]
        public Reference AssetIdentificationModel { get; set; }
    }
}