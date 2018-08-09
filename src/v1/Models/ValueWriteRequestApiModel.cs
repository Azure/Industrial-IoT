// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IIoT.OpcUa.Services.Twin.v1.Models {
    using Microsoft.Azure.IIoT.OpcUa.Models;
    using Newtonsoft.Json;
    using Newtonsoft.Json.Linq;
    using System.ComponentModel.DataAnnotations;

    /// <summary>
    /// Value write request model for webservice api
    /// </summary>
    public class ValueWriteRequestApiModel {

        /// <summary>
        /// Default constructor
        /// </summary>
        public ValueWriteRequestApiModel() {}

        /// <summary>
        /// Create from service model
        /// </summary>
        /// <param name="model"></param>
        public ValueWriteRequestApiModel(ValueWriteRequestModel model) {
            Node = new NodeApiModel(model.Node);
            Value = model.Value;
            Elevation = model.Elevation == null ? null :
                new AuthenticationApiModel(model.Elevation);
        }

        /// <summary>
        /// Convert back to service model
        /// </summary>
        /// <returns></returns>
        public ValueWriteRequestModel ToServiceModel() {
            return new ValueWriteRequestModel {
                Node = Node.ToServiceModel(),
                Elevation = Elevation?.ToServiceModel(),
                Value = Value
            };
        }

        /// <summary>
        /// Node information to allow writing - from browse.
        /// </summary>
        [JsonProperty(PropertyName = "node")]
        [Required]
        public NodeApiModel Node { get; set; }

        /// <summary>
        /// Value to write
        /// </summary>
        [JsonProperty(PropertyName = "value")]
        [Required]
        public JToken Value { get; set; }

        /// <summary>
        /// Optional User elevation
        /// </summary>
        [JsonProperty(PropertyName = "elevation",
            NullValueHandling = NullValueHandling.Ignore)]
        public AuthenticationApiModel Elevation { get; set; }
    }
}
