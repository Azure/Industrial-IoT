// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IIoT.Net {
    using System.Net;
    using System.Threading;
    using System.Threading.Tasks;

    /// <summary>
    /// Shell factory extensions
    /// </summary>
    public static class ShellFactoryEx {

        /// <summary>
        /// Open shell to destination
        /// </summary>
        /// <param name="factory"></param>
        /// <param name="host"></param>
        /// <param name="port"></param>
        /// <param name="userName"></param>
        /// <param name="password"></param>
        /// <param name="ct"></param>
        /// <returns></returns>
        public static Task<ISecureShell> OpenSecureShellAsync(this IShellFactory factory,
            string host, int port, string userName, string password, CancellationToken ct) {
            return factory.OpenSecureShellAsync(host, port, new NetworkCredential(userName, password), ct);
        }

        /// <summary>
        /// Open shell to destination
        /// </summary>
        /// <param name="factory"></param>
        /// <param name="host"></param>
        /// <param name="userName"></param>
        /// <param name="password"></param>
        /// <param name="ct"></param>
        /// <returns></returns>
        public static Task<ISecureShell> OpenSecureShellAsync(this IShellFactory factory,
            string host, string userName, string password, CancellationToken ct) {
            return factory.OpenSecureShellAsync(host, new NetworkCredential(userName, password), ct);
        }

        /// <summary>
        /// Open shell to destination
        /// </summary>
        /// <param name="factory"></param>
        /// <param name="host"></param>
        /// <param name="userName"></param>
        /// <param name="password"></param>
        /// <returns></returns>
        public static Task<ISecureShell> OpenSecureShellAsync(this IShellFactory factory,
            string host, string userName, string password) {
            return factory.OpenSecureShellAsync(host, new NetworkCredential(userName, password));
        }

        /// <summary>
        /// Open shell to destination
        /// </summary>
        /// <param name="factory"></param>
        /// <param name="host"></param>
        /// <param name="credentials"></param>
        /// <param name="ct"></param>
        /// <returns></returns>
        public static Task<ISecureShell> OpenSecureShellAsync(this IShellFactory factory,
            string host, NetworkCredential credentials, CancellationToken ct) {
            return factory.OpenSecureShellAsync(host, 22, credentials, ct);
        }

        /// <summary>
        /// Open shell to destination
        /// </summary>
        /// <param name="factory"></param>
        /// <param name="host"></param>
        /// <param name="credentials"></param>
        /// <returns></returns>
        public static Task<ISecureShell> OpenSecureShellAsync(this IShellFactory factory,
            string host, NetworkCredential credentials) {
            return factory.OpenSecureShellAsync(host, credentials, CancellationToken.None);
        }
    }
}
