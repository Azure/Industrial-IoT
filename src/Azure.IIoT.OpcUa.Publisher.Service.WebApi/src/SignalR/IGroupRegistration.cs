// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Azure.IIoT.OpcUa.Publisher.Service.WebApi.SignalR
{
    using System.Threading;
    using System.Threading.Tasks;

    /// <summary>
    /// Group registration
    /// </summary>
    /// <typeparam name="THub"></typeparam>
    public interface IGroupRegistration<THub>
    {
        /// <summary>
        /// Add client to multicast group
        /// </summary>
        /// <param name="group"></param>
        /// <param name="client"></param>
        /// <param name="ct"></param>
        /// <returns></returns>
        Task SubscribeAsync(string group, string client,
            CancellationToken ct = default);

        /// <summary>
        /// Remove client from group
        /// </summary>
        /// <param name="group"></param>
        /// <param name="client"></param>
        /// <param name="ct"></param>
        /// <returns></returns>
        Task UnsubscribeAsync(string group, string client,
            CancellationToken ct = default);
    }
}
