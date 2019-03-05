// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IIoT.Modules.OpcUa.Twin.v1.Models {
    using Microsoft.Azure.IIoT.OpcUa.Twin.Models;
    using Newtonsoft.Json;
    using System;

    /// <summary>
    /// Node path target
    /// </summary>
    public class NodePathTargetApiModel {

        /// <summary>
        /// Default constructor
        /// </summary>
        public NodePathTargetApiModel() { }

        /// <summary>
        /// Create reference api model
        /// </summary>
        /// <param name="model"></param>
        public NodePathTargetApiModel(NodePathTargetModel model) {
            if (model == null) {
                throw new ArgumentNullException(nameof(model));
            }
            RemainingPathIndex = model.RemainingPathIndex;
            BrowsePath = model.BrowsePath;
            Target = model.Target == null ? null :
                new NodeApiModel(model.Target);
        }

        /// <summary>
        /// The target browse path
        /// </summary>
        [JsonProperty(PropertyName = "BrowsePath")]
        public string[] BrowsePath { get; set; }

        /// <summary>
        /// Target node
        /// </summary>
        [JsonProperty(PropertyName = "Target")]
        public NodeApiModel Target { get; set; }

        /// <summary>
        /// Remaining index in path
        /// </summary>
        [JsonProperty(PropertyName = "RemainingPathIndex",
            NullValueHandling = NullValueHandling.Ignore)]
        public int? RemainingPathIndex { get; set; }
    }
}
