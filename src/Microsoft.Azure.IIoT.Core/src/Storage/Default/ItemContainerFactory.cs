// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IIoT.Storage.Default {
    using System.Threading.Tasks;
    using System;

    /// <summary>
    /// Injectable container
    /// </summary>
    public class ItemContainerFactory : IItemContainerFactory {

        /// <summary>
        /// Create container factory
        /// </summary>
        /// <param name="server"></param>
        /// <param name="config"></param>
        public ItemContainerFactory(IDatabaseServer server,
            IItemContainerConfig config = null) {
            _server = server ?? throw new ArgumentNullException(nameof(server));
            _config = config;
        }

        /// <inheritdoc/>
        public async Task<IItemContainer> OpenAsync() {
            var database = await _server.OpenAsync(_config?.DatabaseName);
            return await database.OpenContainerAsync(_config?.ContainerName);
        }

        private readonly IDatabaseServer _server;
        private readonly IItemContainerConfig _config;
    }
}
