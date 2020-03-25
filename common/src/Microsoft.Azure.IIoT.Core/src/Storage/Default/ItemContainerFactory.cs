// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IIoT.Storage.Default {
    using Microsoft.Azure.IIoT.Diagnostics;
    using System.Threading.Tasks;
    using System;

    /// <summary>
    /// Injectable container factory
    /// </summary>
    public class ItemContainerFactory : IItemContainerFactory {

        /// <summary>
        /// Create container factory
        /// </summary>
        /// <param name="server"></param>
        /// <param name="config"></param>
        /// <param name="process"></param>
        public ItemContainerFactory(IDatabaseServer server,
            IItemContainerConfig config = null, IProcessIdentity process = null) :
            this(server, config?.DatabaseName,
                config?.ContainerName ?? process?.ServiceId) {
        }

        /// <summary>
        /// Create container factory
        /// </summary>
        /// <param name="server"></param>
        /// <param name="database"></param>
        /// <param name="container"></param>
        private ItemContainerFactory(IDatabaseServer server, string database,
            string container) {
            _server = server ?? throw new ArgumentNullException(nameof(server));
            _container = container;
            _database = database;
        }

        /// <inheritdoc/>
        public async Task<IItemContainer> OpenAsync(string postfix,
            ContainerOptions options) {
            var database = await _server.OpenAsync(_database);
            var name = _container;
            if (!string.IsNullOrEmpty(postfix)) {
                name += "-" + postfix;
            }
            return await database.OpenContainerAsync(name, options);
        }

        private readonly IDatabaseServer _server;
        private readonly string _database;
        private readonly string _container;
    }
}
