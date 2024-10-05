// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Azure.IIoT.OpcUa.Publisher.Stack
{
    using System;
    using System.Collections.Generic;
    using System.Security.Cryptography.X509Certificates;
    using System.Threading.Tasks;

    /// <summary>
    /// Server host
    /// </summary>
    public interface IServerHost : IDisposable
    {
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
        /// Restart server.
        /// </summary>
        /// <returns></returns>
        Task RestartAsync();

        /// <summary>
        /// Add reverse connection
        /// </summary>
        /// <param name="client"></param>
        /// <param name="maxSessionCount"></param>
        Task AddReverseConnectionAsync(Uri client, int maxSessionCount);

        /// <summary>
        /// Remove reverse connection
        /// </summary>
        /// <param name="client"></param>
        Task RemoveReverseConnectionAsync(Uri client);

        /// <summary>
        /// Stop server - same as dispose but async.
        /// </summary>
        Task StopAsync();
    }
}
