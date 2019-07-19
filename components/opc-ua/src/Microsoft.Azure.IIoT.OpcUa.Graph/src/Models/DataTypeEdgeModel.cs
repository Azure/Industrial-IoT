// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IIoT.OpcUa.Graph.Models {
    using Gremlin.Net.CosmosDb.Structure;

    /// <summary>
    /// Declares the type of a a variable and variable type node
    /// </summary>
    [Label(AddressSpaceElementNames.ofType)]
    public class DataTypeEdgeModel :
        AddressSpaceEdgeModel<VariableTypeNodeVertexModel, DataTypeNodeVertexModel> {
    }
}
