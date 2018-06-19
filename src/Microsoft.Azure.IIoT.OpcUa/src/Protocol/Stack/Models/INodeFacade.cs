// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Opc.Ua.Models {
    using System;
    using System.Threading.Tasks;

    /// <summary>
    /// Represents a node
    /// </summary>
    public interface INodeFacade {

        /// <summary>
        /// Returns the local node id
        /// </summary>
        NodeId LocalId { get; }

        /// <summary>
        /// Class of node
        /// </summary>
        NodeClass Class { get; }
    }
}