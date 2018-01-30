// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IoTSolutions.OpcUaExplorer.EdgeService {
    using System;

    /// <summary>
    /// Encapsulates a runnable module
    /// </summary>
    public interface IEdgeService : IDisposable {

        /// <summary>
        /// Start service
        /// </summary>
        /// <returns></returns>
        void Start();

        /// <summary>
        /// Stop service
        /// </summary>
        /// <returns></returns>
        void Stop();
    }
}