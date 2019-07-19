// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IIoT.OpcUa.Graph.Models {
    using Gremlin.Net.CosmosDb.Structure;

    /// <summary>
    /// Node to source edge.  A node can point to sources with
    /// different versions.  The source uri in the node id
    /// guarantees that all sources have the same uri.
    /// </summary>
    [Label(AddressSpaceElementNames.originatesFrom)]
    public class AddressSpaceSourceEdgeModel :
        AddressSpaceEdgeModel<AddressSpaceVertexModel, SourceVertexModel> {
    }
}
