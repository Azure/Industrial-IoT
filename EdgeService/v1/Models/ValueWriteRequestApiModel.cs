// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IoTSolutions.OpcUaExplorer.EdgeService.v1.Models {
    using Microsoft.Azure.IoTSolutions.OpcUaExplorer.Services.Models;

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
        }

        /// <summary>
        /// Convert back to service node model
        /// </summary>
        /// <returns></returns>
        public ValueWriteRequestModel ToServiceModel() {
            return new ValueWriteRequestModel {
                Node = Node.ToServiceModel(),
                Value = Value
            };
        }

        /// <summary>
        /// Node information to allow writing - from browse.
        /// </summary>
        public NodeApiModel Node { get; set; }

        /// <summary>
        /// Value to write
        /// </summary>
        public string Value { get; set; }
    }
}
