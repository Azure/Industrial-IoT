// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IIoT.OpcUa.Graph.Models {
    using Gremlin.Net.CosmosDb.Structure;
    using System.Runtime.Serialization;

    /// <summary>
    /// Node vertex
    /// </summary>
    [Label(AddressSpaceElementNames.ReferenceType)]
    [DataContract]
    public class ReferenceTypeNodeVertexModel : BaseNodeVertexModel {

        /// <summary>
        /// Whether type is abstract, if type can
        /// be abstract.  Null if not type node.
        /// </summary>
        [DataMember(Name = "isAbstract",
            EmitDefaultValue = false)]
        public bool? IsAbstract { get; set; }

        /// <summary>
        /// Inverse name of the reference if the node is
        /// a reference type, keyed on the locale
        /// </summary>
        [DataMember(Name = "inverseName",
            EmitDefaultValue = false)]
        public string InverseName { get; set; }

        /// <summary>
        /// Whether the reference is symmetric in case
        /// the node is a reference type, otherwise
        /// null.
        /// </summary>
        [DataMember(Name = "symmetric",
            EmitDefaultValue = false)]
        public bool? Symmetric { get; set; }

        /// <summary>
        /// Where it is part of
        /// </summary>
        public ReferenceTypeEdgeModel Defines { get; set; }
    }
}
