// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IIoT.OpcUa.Graph.Models {
    using Gremlin.Net.CosmosDb.Structure;
    using System.Runtime.Serialization;

    /// <summary>
    /// Object type vertex
    /// </summary>
    [Label(AddressSpaceElementNames.ObjectType)]
    [DataContract]
    public class ObjectTypeNodeVertexModel : BaseNodeVertexModel {

        /// <summary>
        /// Whether type is abstract, if type can
        /// be abstract.  Null if not type node.
        /// </summary>
        [DataMember(Name = "isAbstract",
            EmitDefaultValue = false)]
        public bool? IsAbstract { get; set; }
    }
}
