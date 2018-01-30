// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IoTSolutions.OpcUaExplorer.EdgeService.v1.Models {
    using Microsoft.Azure.IoTSolutions.OpcUaExplorer.Services.Models;

    /// <summary>
    /// Node value read request webservice api model
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
        /// Convert back to service node model
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
