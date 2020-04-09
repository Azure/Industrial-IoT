// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------


namespace Microsoft.Azure.IIoT.OpcUa.Registry {
    using System;

    /// <summary>
    /// Emits Registry events
    /// </summary>
    public interface IRegistryEvents<T> where T : class {

        /// <summary>
        /// Register listener
        /// </summary>
        /// <returns></returns>
        Action Register(T listener);
    }
}
