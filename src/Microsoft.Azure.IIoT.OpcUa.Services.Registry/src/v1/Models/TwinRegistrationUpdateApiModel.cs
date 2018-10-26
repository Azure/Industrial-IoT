// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IIoT.OpcUa.Services.Registry.v1.Models {
    using Microsoft.Azure.IIoT.OpcUa.Registry.Models;
    using Newtonsoft.Json;
    using System.ComponentModel;
    using System.ComponentModel.DataAnnotations;

    /// <summary>
    /// Twin registration update request
    /// </summary>
    public class TwinRegistrationUpdateApiModel {

        /// <summary>
        /// Default constructor
        /// </summary>
        public TwinRegistrationUpdateApiModel() { }

        /// <summary>
        /// Create from service model
        /// </summary>
        /// <param name="model"></param>
        public TwinRegistrationUpdateApiModel(TwinRegistrationUpdateModel model) {
            Id = model.Id;
            Duplicate = model.Duplicate;
            Authentication = model.Authentication == null ?
                null : new AuthenticationApiModel(model.Authentication);
        }

        /// <summary>
        /// Convert back to service model
        /// </summary>
        /// <returns></returns>
        public TwinRegistrationUpdateModel ToServiceModel() {
            return new TwinRegistrationUpdateModel {
                Id = Id,
                Duplicate = Duplicate,
                Authentication = Authentication?.ToServiceModel()
            };
        }

        /// <summary>
        /// Identifier of the twin to patch
        /// </summary>
        [JsonProperty(PropertyName = "id")]
        [Required]
        public string Id { get; set; }

        /// <summary>
        /// Whether to copy existing registration
        /// rather than replacing, null == false
        /// </summary>
        [JsonProperty(PropertyName = "duplicate",
            NullValueHandling = NullValueHandling.Ignore)]
        [DefaultValue(null)]
        public bool? Duplicate { get; set; }

        /// <summary>
        /// Authentication to change on the twin.
        /// </summary>
        [JsonProperty(PropertyName = "authentication",
            NullValueHandling = NullValueHandling.Ignore)]
        [DefaultValue(null)]
        public AuthenticationApiModel Authentication { get; set; }
    }
}
