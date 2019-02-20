// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IIoT.Services.OpcUa.Twin.v1.Models {
    using Microsoft.Azure.IIoT.OpcUa.Twin.Models;
    using Newtonsoft.Json;
    using System;
    using System.ComponentModel.DataAnnotations;

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
            Target = model.Target == null ? null :
                new NodeApiModel(model.Target);
        }

        /// <summary>
        /// Target node
        /// </summary>
        [JsonProperty(PropertyName = "target")]
        [Required]
        public NodeApiModel Target { get; set; }

        /// <summary>
        /// Remaining index in path
        /// </summary>
        [JsonProperty(PropertyName = "remainingPathIndex",
            NullValueHandling = NullValueHandling.Ignore)]
        public int? RemainingPathIndex { get; set; }
    }
}
