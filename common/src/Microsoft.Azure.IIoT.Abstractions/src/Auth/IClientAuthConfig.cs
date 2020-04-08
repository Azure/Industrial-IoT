// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IIoT.Auth.Clients {
    using System.Collections.Generic;

    /// <summary>
    /// Configuration interface for client authentication
    /// </summary>
    public interface IClientAuthConfig {

        /// <summary>
        /// Supported clients
        /// </summary>
        IEnumerable<IOAuthClientConfig> ClientSchemes { get; }
    }
}
