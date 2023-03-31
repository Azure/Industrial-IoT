// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Azure.IIoT.OpcUa.Publisher
{
    using Azure.IIoT.OpcUa.Publisher.Models;
    using System.Threading;
    using System.Threading.Tasks;

    /// <summary>
    /// Connection services
    /// </summary>
    /// <typeparam name="T"></typeparam>
    public interface IConnectionServices<T>
    {
        /// <summary>
        /// Connect endpoint
        /// </summary>
        /// <param name="endpoint">Server endpoint to talk to</param>
        /// <param name="credential"></param>
        /// <returns></returns>
        /// <param name="ct"></param>
        Task ConnectAsync(T endpoint, CredentialModel? credential = null,
            CancellationToken ct = default);

        /// <summary>
        /// Disconnect endpoint if there are
        /// no subscriptions
        /// </summary>
        /// <param name="endpoint">Server endpoint to talk to</param>
        /// <param name="credential"></param>
        /// <returns></returns>
        /// <param name="ct"></param>
        Task DisconnectAsync(T endpoint, CredentialModel? credential = null,
            CancellationToken ct = default);
    }
}
