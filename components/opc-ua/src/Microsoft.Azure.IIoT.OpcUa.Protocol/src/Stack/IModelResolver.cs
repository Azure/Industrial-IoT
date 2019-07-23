// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Opc.Ua {

    /// <summary>
    /// A model resolver resolves nodes and identifiers
    /// </summary>
    public interface IModelResolver : INodeResolver, INodeIdAssigner {
    }
}
