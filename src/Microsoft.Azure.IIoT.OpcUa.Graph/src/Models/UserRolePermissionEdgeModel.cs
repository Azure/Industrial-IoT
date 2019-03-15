// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IIoT.OpcUa.Graph.Models {
    using Gremlin.Net.CosmosDb.Structure;

    /// <summary>
    /// The outgoing vertex is the role of a permission vertex
    /// </summary>
    [Label(AddressSpaceElementNames.userRolePermission)]
    public class UserRolePermissionEdgeModel : RolePermissionEdgeModel {
    }
}
