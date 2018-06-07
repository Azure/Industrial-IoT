// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace I40.Common.Models {

    /// <summary>
    /// Reference to semantics
    /// </summary>
    public interface IHasSemanticId {

        /// <summary>
        /// [0..1] Semantics referred to
        /// </summary>
        Reference Semantics { get; }
    }
}