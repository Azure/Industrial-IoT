// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Azure.IIoT.OpcUa.Publisher.Service.Sdk.SignalR
{
    using System.Threading;
    using System.Threading.Tasks;

    /// <summary>
    /// Factory to get callback registrations
    /// </summary>
    public interface ICallbackClient
    {
        /// <summary>
        /// Get callback registration interface for hub
        /// at endoint
        /// </summary>
        /// <param name="endpointUrl"></param>
        /// <param name="ct"></param>
        /// <returns></returns>
        Task<ICallbackRegistrar> GetHubAsync(
            string endpointUrl, CancellationToken ct = default);
    }
}
