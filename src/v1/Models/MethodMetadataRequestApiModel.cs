// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IIoT.OpcUa.Services.Twin.v1.Models {
    using Microsoft.Azure.IIoT.OpcUa.Models;
    using Newtonsoft.Json;
    using System.ComponentModel;
    using System.ComponentModel.DataAnnotations;

    /// <summary>
    /// Method metadata request model
    /// </summary>
    public class MethodMetadataRequestApiModel {

        /// <summary>
        /// Default constructor
        /// </summary>
        public MethodMetadataRequestApiModel() { }

        /// <summary>
        /// Create from service model
        /// </summary>
        /// <param name="model"></param>
        public MethodMetadataRequestApiModel(MethodMetadataRequestModel model) {
            MethodId = model.MethodId;
            Elevation = model.Elevation == null ? null :
                new AuthenticationApiModel(model.Elevation);
        }

        /// <summary>
        /// Convert back to service model
        /// </summary>
        /// <returns></returns>
        public MethodMetadataRequestModel ToServiceModel() {
            return new MethodMetadataRequestModel {
                MethodId = MethodId,
                Elevation = Elevation?.ToServiceModel()
            };
        }

        /// <summary>
        /// Count of input arguments
        /// </summary>
        [JsonProperty(PropertyName = "methodId")]
        [Required]
        public string MethodId { get; set; }

        /// <summary>
        /// Optional User elevation
        /// </summary>
        [JsonProperty(PropertyName = "elevation",
            NullValueHandling = NullValueHandling.Ignore)]
        [DefaultValue(null)]
        public AuthenticationApiModel Elevation { get; set; }
    }
}
