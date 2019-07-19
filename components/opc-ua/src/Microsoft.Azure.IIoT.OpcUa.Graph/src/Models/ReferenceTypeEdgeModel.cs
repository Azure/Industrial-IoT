// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IIoT.OpcUa.Graph.Models {
    using Gremlin.Net.CosmosDb.Structure;

    /// <summary>
    /// Declares the type of an address space vertex, e.g. a
    /// reference's reference type node id or variable node's data type.
    /// </summary>
    [Label(AddressSpaceElementNames.ofType)]
    public class ReferenceTypeEdgeModel :
        AddressSpaceEdgeModel<ReferenceNodeVertexModel, ReferenceTypeNodeVertexModel> {
    }
}
