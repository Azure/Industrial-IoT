// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IIoT.OpcUa.Services.Twin.v1.Models {
    using Microsoft.Azure.IIoT.OpcUa.Twin.Models;
    using Newtonsoft.Json;
    using Newtonsoft.Json.Linq;

    /// <summary>
    /// method arg model
    /// </summary>
    public class MethodCallArgumentApiModel {

        /// <summary>
        /// Default constructor
        /// </summary>
        public MethodCallArgumentApiModel() {}

        /// <summary>
        /// Create from service model
        /// </summary>
        /// <param name="model"></param>
        public MethodCallArgumentApiModel(MethodCallArgumentModel model) {
            Value = model.Value;
            DataType = model.DataType;
        }

        /// <summary>
        /// Convert back to service model
        /// </summary>
        /// <returns></returns>
        public MethodCallArgumentModel ToServiceModel() {
            return new MethodCallArgumentModel {
                Value = Value,
                DataType = DataType
            };
        }

        /// <summary>
        /// Initial value or value to use
        /// </summary>
        [JsonProperty(PropertyName = "value")]
        public JToken Value { get; set; }

        /// <summary>
        /// Data type Id of the value (from meta data)
        /// </summary>
        [JsonProperty(PropertyName = "dataType")]
        public string DataType { get; set; }
    }
}
