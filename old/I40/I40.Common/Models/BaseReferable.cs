// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace I40.Common.Models {
    using Newtonsoft.Json;
    using System.ComponentModel.DataAnnotations;

    /// <inheritdoc/>
    public abstract class BaseReferable : IReferable {

        /// <inheritdoc/>
        [JsonProperty(PropertyName = "idShort")]  // TODO: Was idShort
        [Required]
        public string Name { get; set; }

        /// <inheritdoc/>
        [JsonProperty(PropertyName = "description",
            NullValueHandling = NullValueHandling.Ignore)]
        public LanguageString Description { get; set; }

        /// <inheritdoc/>
        [JsonProperty(PropertyName = "$id")]
        [Required]
        public Reference Self { get; set; }

        /// <summary>
        /// Parent of the referable
        /// </summary>
        [JsonIgnore]
        internal Reference Parent { get; set; }

        /// <summary>
        /// Implicit conversion to Reference
        /// </summary>
        /// <param name="r"></param>
        public static implicit operator Reference(
            BaseReferable r) => r.Self;
    }
}