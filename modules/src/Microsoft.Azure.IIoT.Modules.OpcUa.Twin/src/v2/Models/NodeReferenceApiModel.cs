// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IIoT.Modules.OpcUa.Twin.v2.Models {
    using Microsoft.Azure.IIoT.OpcUa.Core.Models;
    using Newtonsoft.Json;
    using System;

    /// <summary>
    /// reference model for module
    /// </summary>
    public class NodeReferenceApiModel {

        /// <summary>
        /// Default constructor
        /// </summary>
        public NodeReferenceApiModel() { }

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
        [JsonProperty(PropertyName = "ReferenceTypeId",
            NullValueHandling = NullValueHandling.Ignore)]
        public string ReferenceTypeId { get; set; }

        /// <summary>
        /// Browse direction of reference
        /// </summary>
        [JsonProperty(PropertyName = "Direction",
            NullValueHandling = NullValueHandling.Ignore)]
        public BrowseDirection? Direction { get; set; }

        /// <summary>
        /// Target node
        /// </summary>
        [JsonProperty(PropertyName = "Target")]
        public NodeApiModel Target { get; set; }
    }
}
