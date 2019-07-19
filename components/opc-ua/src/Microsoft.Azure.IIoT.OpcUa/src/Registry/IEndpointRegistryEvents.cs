// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------


namespace Microsoft.Azure.IIoT.OpcUa.Registry {
    using System;

    /// <summary>
    /// Emits endpoint registry events
    /// </summary>
    public interface IEndpointRegistryEvents {

        /// <summary>
        /// Register listener
        /// </summary>
        /// <returns></returns>
        Action Register(IEndpointRegistryListener listener);
    }
}
