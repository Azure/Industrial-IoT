// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Azure.IIoT.OpcUa {
    using Azure.IIoT.OpcUa.Api.Models;
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
        /// <param name="credential"></param>
        /// <returns></returns>
        /// <param name="ct"></param>
        Task ConnectAsync(T id, CredentialModel credential = null,
            CancellationToken ct = default);

        /// <summary>
        /// Disconnect endpoint if there are
        /// no subscriptions
        /// </summary>
        /// <param name="id"></param>
        /// <param name="credential"></param>
        /// <returns></returns>
        /// <param name="ct"></param>
        Task DisconnectAsync(T id, CredentialModel credential = null,
            CancellationToken ct = default);
    }
}
