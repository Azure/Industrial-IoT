// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IIoT.Services.Http.Proxy {
    using System.Collections.Generic;

    /// <summary>
    /// Configures the proxy
    /// </summary>
    public interface IReverseProxyConfig {

        /// <summary>
        /// Resource to host translation table
        /// </summary>
        IDictionary<string, string> ResourceIdToHostLookup { get; }
    }
}
