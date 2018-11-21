// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IIoT.OpcUa.Protocol.Models {
    using Opc.Ua;

    /// <summary>
    /// A typed reference between two nodes
    /// </summary>
    public interface IReferenceFacade {

        /// <summary>
        /// Source node id
        /// </summary>
        ExpandedNodeId SourceId { get; }

        /// <summary>
        /// The type of reference.
        /// </summary>
        /// <value>The reference type identifier.</value>
        NodeId ReferenceTypeId { get; }

        /// <summary>
        /// The target node
        /// </summary>
        ExpandedNodeId TargetId { get; }
    }
}
