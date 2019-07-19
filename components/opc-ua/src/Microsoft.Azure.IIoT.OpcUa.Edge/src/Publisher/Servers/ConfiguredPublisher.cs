// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IIoT.OpcUa.Edge.Publisher.Servers {
    using System;
    using System.Threading.Tasks;

    /// <summary>
    /// Configured publisher server
    /// </summary>
    public sealed class ConfiguredPublisher : IPublisherServer {

        /// <summary>
        /// Create Server
        /// </summary>
        /// <param name="client"></param>
        public ConfiguredPublisher(IPublisherClient client) {
            _client = client ?? throw new ArgumentNullException(nameof(client));
        }

        /// <inheritdoc/>
        public Task<IPublisherClient> ConnectAsync() {
            return Task.FromResult(_client);
        }

        private readonly IPublisherClient _client;
    }
}
