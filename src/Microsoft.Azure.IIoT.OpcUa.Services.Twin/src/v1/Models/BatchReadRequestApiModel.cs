// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IIoT.OpcUa.Services.Twin.v1.Models {
    using Microsoft.Azure.IIoT.OpcUa.Twin.Models;
    using Newtonsoft.Json;
    using System.ComponentModel.DataAnnotations;
    using System.Collections.Generic;
    using System.Linq;

    /// <summary>
    /// Request node attribute read or update
    /// </summary>
    public class BatchReadRequestApiModel {

        /// <summary>
        /// Default constructor
        /// </summary>
        public BatchReadRequestApiModel() { }

        /// <summary>
        /// Create from service model
        /// </summary>
        /// <param name="model"></param>
        public BatchReadRequestApiModel(BatchReadRequestModel model) {
            Attributes = model.Attributes?
                .Select(a => new AttributeReadRequestApiModel(a)).ToList();
            Elevation = model.Elevation == null ? null :
                new CredentialApiModel(model.Elevation);
            Diagnostics = model.Diagnostics == null ? null :
                new DiagnosticsApiModel(model.Diagnostics);
        }

        /// <summary>
        /// Convert back to service model
        /// </summary>
        /// <returns></returns>
        public BatchReadRequestModel ToServiceModel() {
            return new BatchReadRequestModel {
                Attributes = Attributes?.Select(a => a.ToServiceModel()).ToList(),
                Diagnostics = Diagnostics?.ToServiceModel(),
                Elevation = Elevation?.ToServiceModel()
            };
        }

        /// <summary>
        /// Attributes to update or read
        /// </summary>
        [JsonProperty(PropertyName = "attributes")]
        [Required]
        public List<AttributeReadRequestApiModel> Attributes { get; set; }

        /// <summary>
        /// Optional User Elevation
        /// </summary>
        [JsonProperty(PropertyName = "elevation",
            NullValueHandling = NullValueHandling.Ignore)]
        public CredentialApiModel Elevation { get; set; }

        /// <summary>
        /// Optional diagnostics configuration
        /// </summary>
        [JsonProperty(PropertyName = "diagnostics",
            NullValueHandling = NullValueHandling.Ignore)]
        public DiagnosticsApiModel Diagnostics { get; set; }
    }
}
