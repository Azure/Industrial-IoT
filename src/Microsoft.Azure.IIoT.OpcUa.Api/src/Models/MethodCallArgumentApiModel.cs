// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IIoT.OpcUa.Api.Models {
    using Newtonsoft.Json;
    using Newtonsoft.Json.Linq;

    /// <summary>
    /// method arg model
    /// </summary>
    public class MethodCallArgumentApiModel {

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
