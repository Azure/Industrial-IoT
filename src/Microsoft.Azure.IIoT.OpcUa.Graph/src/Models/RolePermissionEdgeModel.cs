// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IIoT.OpcUa.Graph.Models {
    using Microsoft.Azure.IIoT.OpcUa.Twin.Models;
    using Gremlin.Net.CosmosDb.Structure;
    using Newtonsoft.Json;

    /// <summary>
    /// The outgoing vertex is the role of a permission vertex
    /// </summary>
    [Label(AddressSpaceElementNames.rolePermission)]
    public class RolePermissionEdgeModel :
        AddressSpaceEdgeModel<NodeVertexModel, VariableNodeVertexModel> {

        /// <summary>
        /// Returns the permissions for the role
        /// </summary>
        [JsonProperty(PropertyName = "permissions")]
        public RolePermissions? Permissions { get; set; }

        /// <summary>
        /// Returns the role id which is the target of the edge
        /// </summary>
        [JsonProperty(PropertyName = "roleId")]
        public string RoleId { get; set; }
    }
}
