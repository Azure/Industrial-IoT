// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IIoT.OpcUa.Modules.Twin.v1.Models {
    using Microsoft.Azure.IIoT.OpcUa.Models;

    /// <summary>
    /// Node value read request twin module model
    /// </summary>
    public class ValueReadRequestApiModel {
        /// <summary>
        /// Default constructor
        /// </summary>
        public ValueReadRequestApiModel() {}

        /// <summary>
        /// Create from service model
        /// </summary>
        /// <param name="model"></param>
        public ValueReadRequestApiModel(ValueReadRequestModel model) {
            NodeId = model.NodeId;
        }

        /// <summary>
        /// Convert back to service model
        /// </summary>
        /// <returns></returns>
        public ValueReadRequestModel ToServiceModel() {
            return new ValueReadRequestModel {
                NodeId = NodeId
            };
        }

        /// <summary>
        /// Node to read value from
        /// </summary>
        public string NodeId { get; set; }
    }
}
