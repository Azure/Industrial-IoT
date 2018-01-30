// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IoTSolutions.Shared.External {
    /// <summary>
    /// Configuration for service
    /// </summary>
    public interface IOpcUaExplorerConfig {

        /// <summary>
        /// Opc UA explorer url
        /// </summary>
        string OpcUaExplorerV1ApiUrl { get; }
    }
}
