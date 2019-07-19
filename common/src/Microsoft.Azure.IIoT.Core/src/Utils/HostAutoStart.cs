// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IIoT.Utils {
    using System;
    using Autofac;

    /// <summary>
    /// Host auto starter
    /// </summary>
    public class HostAutoStart : IDisposable, IStartable {

        /// <summary>
        /// Auto registers handlers in client
        /// </summary>
        /// <param name="host"></param>
        public HostAutoStart(IHost host) {
            _host = host ?? throw new ArgumentNullException(nameof(host));
        }

        /// <inheritdoc/>
        public void Dispose() {
            _host.StopAsync().Wait();
        }

        /// <inheritdoc/>
        public void Start() {
            _host.StartAsync().Wait();
        }

        private readonly IHost _host;
    }
}