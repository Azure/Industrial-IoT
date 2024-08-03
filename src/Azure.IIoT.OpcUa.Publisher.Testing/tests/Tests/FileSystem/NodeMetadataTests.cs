// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Azure.IIoT.OpcUa.Publisher.Testing.Tests
{
    using Azure.IIoT.OpcUa.Publisher.Models;
    using System;
    using System.Linq;
    using System.Threading;
    using System.Threading.Tasks;
    using Xunit;

    public class FileSystemTests<T>
    {
        /// <summary>
        /// Create metadata tests
        /// </summary>
        /// <param name="services"></param>
        /// <param name="connection"></param>
        public FileSystemTests(Func<IFileSystemServices<T>> services, T connection)
        {
            _services = services;
            _connection = connection;
        }

        public async Task GetServerCapabilitiesTestAsync(CancellationToken ct = default)
        {
            var services = _services();

            await Task.Delay(1, ct).ConfigureAwait(false);
        }

        private readonly T _connection;
        private readonly Func<IFileSystemServices<T>> _services;
    }
}
