// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IIoT.OpcUa.Graph.Models {
    using Gremlin.Net.CosmosDb.Structure;
    using Newtonsoft.Json;

    /// <summary>
    /// Method node vertex
    /// </summary>
    [Label(AddressSpaceElementNames.Method)]
    public class MethodNodeVertexModel : BaseNodeVertexModel {

        /// <summary>
        /// If method node class, whether method can be called.
        /// </summary>
        [JsonProperty(PropertyName = "executable",
            NullValueHandling = NullValueHandling.Ignore)]
        public bool? Executable { get; set; }

        /// <summary>
        /// If method node class, whether method can be called
        /// by user.
        /// </summary>
        [JsonProperty(PropertyName = "userExecutable",
            NullValueHandling = NullValueHandling.Ignore)]
        public bool? UserExecutable { get; set; }
    }
}
