// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IIoT.OpcUa.Registry {
    using System.Threading;
    using System.Threading.Tasks;

    /// <summary>
    /// Connection services
    /// </summary>
    public interface IConnectionServices<T> {

        /// <summary>
        /// Connect endpoint
        /// </summary>
        /// <param name="id"></param>
        /// <param name="ct"></param>
        /// <returns></returns>
        Task ConnectAsync(T id, CancellationToken ct = default);

        /// <summary>
        /// Disconnect endpoint if there are
        /// no subscriptions
        /// </summary>
        /// <param name="id"></param>
        /// <param name="ct"></param>
        /// <returns></returns>
        Task DisconnectAsync(T id, CancellationToken ct = default);
    }
}
