// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IIoT.Services.OpcUa.Twin.v2.Models {
    using Microsoft.Azure.IIoT.OpcUa.Twin.Models;
    using Newtonsoft.Json;
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.ComponentModel.DataAnnotations;
    using System.ComponentModel;

    /// <summary>
    /// Request node attribute write
    /// </summary>
    public class WriteRequestApiModel {

        /// <summary>
        /// Default constructor
        /// </summary>
        public WriteRequestApiModel() { }

        /// <summary>
        /// Create from service model
        /// </summary>
        /// <param name="model"></param>
        public WriteRequestApiModel(WriteRequestModel model) {
            if (model == null) {
                throw new ArgumentNullException(nameof(model));
            }
            Attributes = model.Attributes?
                .Select(a => a == null ? null : new AttributeWriteRequestApiModel(a))
                .ToList();
            Header = model.Header == null ? null :
                new RequestHeaderApiModel(model.Header);
        }

        /// <summary>
        /// Convert back to service model
        /// </summary>
        /// <returns></returns>
        public WriteRequestModel ToServiceModel() {
            return new WriteRequestModel {
                Attributes = Attributes?.Select(a => a?.ToServiceModel()).ToList(),
                Header = Header?.ToServiceModel()
            };
        }

        /// <summary>
        /// Attributes to update
        /// </summary>
        [JsonProperty(PropertyName = "attributes")]
        [Required]
        public List<AttributeWriteRequestApiModel> Attributes { get; set; }

        /// <summary>
        /// Optional request header
        /// </summary>
        [JsonProperty(PropertyName = "header",
            NullValueHandling = NullValueHandling.Ignore)]
        [DefaultValue(null)]
        public RequestHeaderApiModel Header { get; set; }
    }
}
