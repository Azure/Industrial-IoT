// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IIoT.Net.Ssh {
    using Microsoft.Azure.IIoT.Diagnostics;
    using Microsoft.Azure.IIoT.Utils;
    using System.Net;
    using System.Threading;
    using System.Threading.Tasks;

    /// <summary>
    /// Secure shell factory
    /// </summary>
    public class SshShellFactory : IShellFactory {

        /// <summary>
        /// Create factory
        /// </summary>
        /// <param name="logger">Logger dependency</param>
        public SshShellFactory(ILogger logger) {
            _logger = logger;
        }

        /// <summary>
        /// Create ssh shell - tries until succeeded or give up.
        /// </summary>
        /// <param name="host"></param>
        /// <param name="port"></param>
        /// <param name="credential"></param>
        /// <param name="ct"></param>
        /// <returns></returns>
        public Task<ISecureShell> OpenSecureShellAsync(string host, int port,
            NetworkCredential credential, CancellationToken ct) {
            return Retry.WithLinearBackoff<ISecureShell>(_logger,
                ct, () => new SshSecureShell(host, port,
                    credential.UserName, credential.Password, _logger));
        }

        private readonly ILogger _logger;
    }
}
