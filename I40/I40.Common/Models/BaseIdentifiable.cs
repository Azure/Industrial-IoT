// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace I40.Common.Models {
    using Newtonsoft.Json;
    using System.ComponentModel.DataAnnotations;

    /// <inheritdoc/>
    public abstract class BaseIdentifiable : BaseReferable, IIdentifiable {

        /// <inheritdoc/>
        [JsonProperty(PropertyName = "id")]
        [Required]
        public Identification Id { get; set; }

        /// <inheritdoc/>
        [JsonProperty(PropertyName = "administration",
            NullValueHandling = NullValueHandling.Ignore)]
        public AdministrativeInformation Administration { get; set; }
    }
}