// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IIoT.OpcUa.Api.Twin.Models {
    using Newtonsoft.Json;

    /// <summary>
    /// Attribute to read
    /// </summary>
    public class AttributeReadRequestApiModel {

        /// <summary>
        /// Node to read from or write to (mandatory)
        /// </summary>
        [JsonProperty(PropertyName = "nodeId")]
        public string NodeId { get; set; }

        /// <summary>
        /// Attribute to read or write
        /// </summary>
        [JsonProperty(PropertyName = "attribute")]
        public NodeAttribute Attribute { get; set; }
    }
}
