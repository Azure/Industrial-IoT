// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IoTSolutions.OpcUaExplorer.EdgeService {

    /// <summary>
    /// Module configuration
    /// </summary>
    public interface IEdgeConfig {

        /// <summary>
        /// Edge hub connection string
        /// </summary>
        string EdgeHubConnectionString { get; }

        /// <summary>
        /// Bypass cert validation with hub
        /// </summary>
        bool BypassCertVerification { get; }
    }
}