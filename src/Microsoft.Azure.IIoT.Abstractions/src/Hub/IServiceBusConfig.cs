// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IIoT.Hub {

    /// <summary>
    /// Service bus configuration
    /// </summary>
    public interface IServiceBusConfig {

        /// <summary>
        /// Service bus connection string
        /// </summary>
        string ServiceBusConnString { get; }
    }
}
