// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IIoT.Auth.Runtime {
    using System;
    using System.Collections.Generic;
    using System.Linq;

    /// <summary>
    /// Client auth configuration
    /// </summary>
    public class ClientAuthAggregateConfig : IClientAuthConfig {

        /// <inheritdoc/>
        public IEnumerable<IOAuthClientConfig> Providers { get; }

        /// <summary>
        /// Configuration constructor
        /// </summary>
        /// <param name="clients"></param>
        public ClientAuthAggregateConfig(IEnumerable<IOAuthClientConfig> clients) {
            Providers = clients?.Where(s => s.IsValid).ToList()
                ?? throw new ArgumentNullException(nameof(clients));
        }

        /// <inheritdoc/>
        public IEnumerable<IOAuthClientConfig> Query(string resource, string scheme) {
            return Providers?
                .Where(c => c.Resource == resource && c.Provider == scheme)
                    ?? Enumerable.Empty<IOAuthClientConfig>();
        }

        /// <inheritdoc/>
        public IEnumerable<IOAuthClientConfig> Query(string scheme) {
            return Providers?
                .Where(c => c.Provider == scheme)
                    ?? Enumerable.Empty<IOAuthClientConfig>();
        }
    }
}
