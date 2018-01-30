// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IoTSolutions.OpcUaExplorer.Services.Runtime {
    /// <summary>
    /// Configuration for service
    /// </summary>
    public interface IOpcUaServicesConfig {

        /// <summary>
        /// Connection string
        /// </summary>
        string IoTHubConnString { get; }

        /// <summary>
        /// IoTHub manager url
        /// </summary>
        string IoTHubManagerV1ApiUrl { get; }

        /// <summary>
        /// Bypass the use of proxy - for development
        /// </summary>
        bool BypassProxy { get; }
    }
}
