// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IIoT.Messaging {
    using System.Threading;
    using System.Threading.Tasks;

    /// <summary>
    /// Register user in group
    /// </summary>
    public interface IGroupRegistration {

        /// <summary>
        /// Add client to multicast group
        /// </summary>
        /// <param name="group"></param>
        /// <param name="userId"></param>
        /// <param name="ct"></param>
        /// <returns></returns>
        Task SubscribeAsync(string group, string userId,
            CancellationToken ct = default);

        /// <summary>
        /// Remove client from group
        /// </summary>
        /// <param name="group"></param>
        /// <param name="userId"></param>
        /// <param name="ct"></param>
        /// <returns></returns>
        Task UnsubscribeAsync(string group, string userId,
            CancellationToken ct = default);
    }
}
