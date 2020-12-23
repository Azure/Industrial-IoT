// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IIoT.Auth {
    using System.Collections.Generic;

    /// <summary>
    /// Configuration interface for client authentication
    /// </summary>
    public interface IClientAuthConfig {

        /// <summary>
        /// Supported providers
        /// </summary>
        IEnumerable<IOAuthClientConfig> Providers { get; }

        /// <summary>
        /// Retrieve configuration for resource and provider
        /// </summary>
        /// <param name="resource"></param>
        /// <param name="provider"></param>
        /// <returns></returns>
        IEnumerable<IOAuthClientConfig> Query(string resource, string provider);

        /// <summary>
        /// Retrieve configuration for provider
        /// </summary>
        /// <param name="provider"></param>
        /// <returns></returns>
        IEnumerable<IOAuthClientConfig> Query(string provider);
    }
}
