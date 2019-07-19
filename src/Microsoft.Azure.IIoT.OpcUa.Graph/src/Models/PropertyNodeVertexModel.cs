// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IIoT.OpcUa.Graph.Models {
    using Gremlin.Net.CosmosDb.Structure;

    /// <summary>
    /// Variable node vertex - note that isabstract is always null
    /// </summary>
    [Label(AddressSpaceElementNames.Property)]
    public class PropertyNodeVertexModel : VariableNodeVertexModel {
    }
}
