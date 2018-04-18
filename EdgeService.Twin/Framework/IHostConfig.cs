// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.Devices.Edge {

    /// <summary>
    /// Module configuration
    /// </summary>
    public interface IHostConfig {

        /// <summary>
        /// Hub connection string
        /// </summary>
        string HubConnectionString { get; }

        /// <summary>
        /// Bypass cert validation with hub
        /// </summary>
        bool BypassCertVerification { get; }

        /// <summary>
        /// Transports to use
        /// </summary>
        TransportOption Transport { get; }
    }
}