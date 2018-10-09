// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IIoT.OpcUa.Modules.Twin.v1.Models {
    using Microsoft.Azure.IIoT.OpcUa.Twin.Models;
    using Newtonsoft.Json.Linq;

    /// <summary>
    /// Value write request model for twin module
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
            NodeId = model.NodeId;
            DataType = model.DataType;
            IndexRange = model.IndexRange;
            Value = model.Value;
            Elevation = model.Elevation == null ? null :
                new AuthenticationApiModel(model.Elevation);
            Diagnostics = model.Diagnostics == null ? null :
               new DiagnosticsApiModel(model.Diagnostics);
        }

        /// <summary>
        /// Convert back to service model
        /// </summary>
        /// <returns></returns>
        public ValueWriteRequestModel ToServiceModel() {
            return new ValueWriteRequestModel {
                NodeId = NodeId,
                DataType = DataType,
                IndexRange = IndexRange,
                Diagnostics = Diagnostics?.ToServiceModel(),
                Elevation = Elevation?.ToServiceModel(),
                Value = Value
            };
        }

        /// <summary>
        /// Node id to to write value to - from browse.
        /// </summary>
        public string NodeId { get; set; }

        /// <summary>
        /// Value to write
        /// </summary>
        public JToken Value { get; set; }

        /// <summary>
        /// A built in datatype for the value to write.
        /// </summary>
        public string DataType { get; set; }

        /// <summary>
        /// Index range to write
        /// </summary>
        public string IndexRange { get; set; }

        /// <summary>
        /// Optional User elevation
        /// </summary>
        public AuthenticationApiModel Elevation { get; set; }

        /// <summary>
        /// Optional diagnostics configuration
        /// </summary>
        public DiagnosticsApiModel Diagnostics { get; set; }
    }
}
