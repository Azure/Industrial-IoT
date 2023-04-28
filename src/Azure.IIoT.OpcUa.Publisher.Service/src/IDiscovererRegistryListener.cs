// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Azure.IIoT.OpcUa.Publisher.Service
{
    using Azure.IIoT.OpcUa.Publisher.Models;
    using System.Threading.Tasks;

    /// <summary>
    /// Discoverer registry change listener
    /// </summary>
    public interface IDiscovererRegistryListener
    {
        /// <summary>
        /// Called when discoverer is added
        /// </summary>
        /// <param name="context"></param>
        /// <param name="discoverer"></param>
        /// <returns></returns>
        Task OnDiscovererNewAsync(OperationContextModel? context,
            DiscovererModel discoverer);

        /// <summary>
        /// Called when discoverer is updated
        /// </summary>
        /// <param name="context"></param>
        /// <param name="discoverer"></param>
        /// <returns></returns>
        Task OnDiscovererUpdatedAsync(OperationContextModel? context,
            DiscovererModel discoverer);

        /// <summary>
        /// Called when discoverer is delted
        /// </summary>
        /// <param name="context"></param>
        /// <param name="discovererId"></param>
        /// <returns></returns>
        Task OnDiscovererDeletedAsync(OperationContextModel? context,
            string discovererId);
    }
}
