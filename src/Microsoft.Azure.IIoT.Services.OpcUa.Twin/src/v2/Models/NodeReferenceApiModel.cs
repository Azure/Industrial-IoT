// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IIoT.Services.OpcUa.Twin.v2.Models {
    using Microsoft.Azure.IIoT.OpcUa.Twin.Models;
    using Newtonsoft.Json;
    using System;
    using System.ComponentModel.DataAnnotations;

    /// <summary>
    /// reference model
    /// </summary>
    public class NodeReferenceApiModel {

        /// <summary>
        /// Default constructor
        /// </summary>
        public NodeReferenceApiModel() {}

        /// <summary>
        /// Create reference api model
        /// </summary>
        /// <param name="model"></param>
        public NodeReferenceApiModel(NodeReferenceModel model) {
            if (model == null) {
                throw new ArgumentNullException(nameof(model));
            }
            ReferenceTypeId = model.ReferenceTypeId;
            Direction = model.Direction;
            Target = model.Target == null ? null :
                new NodeApiModel(model.Target);
        }

        /// <summary>
        /// Reference Type identifier
        /// </summary>
        [JsonProperty(PropertyName = "referenceTypeId",
            NullValueHandling = NullValueHandling.Ignore)]
        public string ReferenceTypeId { get; set; }

        /// <summary>
        /// Browse direction of reference
        /// </summary>
        [JsonProperty(PropertyName = "direction",
            NullValueHandling = NullValueHandling.Ignore)]
        public BrowseDirection? Direction { get; set; }

        /// <summary>
        /// Target node
        /// </summary>
        [JsonProperty(PropertyName = "target")]
        [Required]
        public NodeApiModel Target { get; set; }
    }
}
