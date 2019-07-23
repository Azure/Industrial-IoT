// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IIoT.Net {
    using System.Net;
    using System.Threading;
    using System.Threading.Tasks;

    /// <summary>
    /// Creates shells that can be used to interact with systems.
    /// </summary>
    public interface IShellFactory {

        /// <summary>
        /// Open shell to destination
        /// </summary>
        /// <param name="host"></param>
        /// <param name="port"></param>
        /// <param name="credentials"></param>
        /// <param name="ct"></param>
        /// <returns></returns>
        Task<ISecureShell> OpenSecureShellAsync(string host, int port,
            NetworkCredential credentials, CancellationToken ct);
    }
}
