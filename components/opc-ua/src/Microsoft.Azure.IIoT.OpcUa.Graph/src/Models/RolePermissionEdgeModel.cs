// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IIoT.OpcUa.Graph.Models {
    using Microsoft.Azure.IIoT.OpcUa.Core.Models;
    using Gremlin.Net.CosmosDb.Structure;
    using System.Runtime.Serialization;

    /// <summary>
    /// The outgoing vertex is the role of a permission vertex
    /// </summary>
    [Label(AddressSpaceElementNames.rolePermission)]
    [DataContract]
    public class RolePermissionEdgeModel :
        AddressSpaceEdgeModel<BaseNodeVertexModel, VariableNodeVertexModel> {

        /// <summary>
        /// Returns the permissions for the role
        /// </summary>
        [DataMember(Name = "permissions",
            EmitDefaultValue = false)]
        public RolePermissions? Permissions { get; set; }

        /// <summary>
        /// Returns the role id which is the target of the edge
        /// </summary>
        [DataMember(Name = "roleId",
            EmitDefaultValue = false)]
        public string RoleId { get; set; }
    }
}
