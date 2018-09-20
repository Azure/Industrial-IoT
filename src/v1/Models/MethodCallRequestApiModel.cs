// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IIoT.OpcUa.Services.Twin.v1.Models {
    using Microsoft.Azure.IIoT.OpcUa.Models;
    using Newtonsoft.Json;
    using System.Collections.Generic;
    using System.Linq;
    using System.ComponentModel.DataAnnotations;
    using System.ComponentModel;

    /// <summary>
    /// Call request model
    /// </summary>
    public class MethodCallRequestApiModel {

        /// <summary>
        /// Default constructor
        /// </summary>
        public MethodCallRequestApiModel() { }

        /// <summary>
        /// Create from service model
        /// </summary>
        /// <param name="model"></param>
        public MethodCallRequestApiModel(MethodCallRequestModel model) {
            MethodId = model.MethodId;
            ObjectId = model.ObjectId;
            if (model.Arguments != null) {
                Arguments = model.Arguments
                    .Select(s => new MethodCallArgumentApiModel(s))
                    .ToList();
            }
            else {
                Arguments = new List<MethodCallArgumentApiModel>();
            }
            Elevation = model.Elevation == null ? null :
                new AuthenticationApiModel(model.Elevation);
        }

        /// <summary>
        /// Convert back to service model
        /// </summary>
        /// <returns></returns>
        public MethodCallRequestModel ToServiceModel() {
            return new MethodCallRequestModel {
                MethodId = MethodId,
                ObjectId = ObjectId,
                Elevation = Elevation?.ToServiceModel(),
                Arguments = Arguments.Select(s => s.ToServiceModel()).ToList()
            };
        }

        /// <summary>
        /// Method id of method to call
        /// </summary>
        [JsonProperty(PropertyName = "methodId")]
        [Required]
        public string MethodId { get; set; }

        /// <summary>
        /// If not global (= null), object or type scope
        /// </summary>
        [JsonProperty(PropertyName = "objectId",
            NullValueHandling = NullValueHandling.Ignore)]
        public string ObjectId { get; set; }

        /// <summary>
        /// Arguments for the method - null means no args
        /// </summary>
        [JsonProperty(PropertyName = "arguments",
            NullValueHandling = NullValueHandling.Ignore)]
        public List<MethodCallArgumentApiModel> Arguments { get; set; }

        /// <summary>
        /// Optional User elevation
        /// </summary>
        [JsonProperty(PropertyName = "elevation",
            NullValueHandling = NullValueHandling.Ignore)]
        [DefaultValue(null)]
        public AuthenticationApiModel Elevation { get; set; }
    }
}
