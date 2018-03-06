// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IoTSolutions.OpcTwin.WebService.Client {
    /// <summary>
    /// Configuration for service
    /// </summary>
    public interface IOpcTwinConfig {

        /// <summary>
        /// Opc twin service url
        /// </summary>
        string OpcTwinServiceApiUrl { get; }
    }
}
