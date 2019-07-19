// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IIoT.OpcUa.Protocol {
    using System;
    using System.Collections.Generic;
    using System.Security.Cryptography.X509Certificates;
    using System.Threading.Tasks;

    /// <summary>
    /// Server host
    /// </summary>
    public interface IServerHost : IDisposable {

        /// <summary>
        /// Server application instance certificate
        /// </summary>
        X509Certificate2 Certificate { get; }

        /// <summary>
        /// Set auto accept mode
        /// </summary>
        bool AutoAccept { get; set; }

        /// <summary>
        /// Start server listening on the specified
        /// ports.
        /// </summary>
        /// <param name="ports"></param>
        /// <returns></returns>
        Task StartAsync(IEnumerable<int> ports);

        /// <summary>
        /// Stop server - same as dispose but async.
        /// </summary>
        Task StopAsync();
    }
}
