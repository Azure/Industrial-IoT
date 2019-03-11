// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IIoT.OpcUa.Graph.Models {
    using Gremlin.Net.CosmosDb.Structure;
    using Newtonsoft.Json;
    using Newtonsoft.Json.Linq;
    using Opc.Ua;
    using Opc.Ua.Nodeset.Schema;

    /// <summary>
    /// Data type node vertex
    /// </summary>
    [Label(AddressSpaceElementNames.DataType)]
    public class DataTypeNodeVertexModel : NodeVertexModel {

        /// <summary>
        /// Whether type is abstract, if type can
        /// be abstract.  Null if not type node.
        /// </summary>
        [JsonProperty(PropertyName = "isAbstract")]
        public bool? IsAbstract { get; set; }

        /// <summary>
        /// Data type definition as extension object.
        /// </summary>
        [JsonProperty(PropertyName = "dataTypeDefinition")]
        public JToken DataTypeDefinition { get; set; }

        /// <summary>
        /// Where it is defining a variable or variable type value
        /// </summary>
        public DataTypeEdgeModel Defines { get; set; }
        public DataTypeDefinition Definition { get; internal set; }
        public DataTypePurpose Purpose { get; internal set; }
    }
}
