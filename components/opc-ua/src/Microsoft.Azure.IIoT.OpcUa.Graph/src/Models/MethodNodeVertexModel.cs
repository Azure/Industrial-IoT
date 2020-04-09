// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IIoT.OpcUa.Graph.Models {
    using Gremlin.Net.CosmosDb.Structure;
    using System.Runtime.Serialization;

    /// <summary>
    /// Method node vertex
    /// </summary>
    [Label(AddressSpaceElementNames.Method)]
    [DataContract]
    public class MethodNodeVertexModel : BaseNodeVertexModel {

        /// <summary>
        /// If method node class, whether method can be called.
        /// </summary>
        [DataMember(Name = "executable",
            EmitDefaultValue = false)]
        public bool? Executable { get; set; }

        /// <summary>
        /// If method node class, whether method can be called
        /// by user.
        /// </summary>
        [DataMember(Name = "userExecutable",
            EmitDefaultValue = false)]
        public bool? UserExecutable { get; set; }
    }
}
