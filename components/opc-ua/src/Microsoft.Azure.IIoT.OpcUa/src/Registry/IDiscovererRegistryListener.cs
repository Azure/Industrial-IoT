// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IIoT.OpcUa.Registry {
    using Microsoft.Azure.IIoT.OpcUa.Registry.Models;
    using System.Threading.Tasks;

    /// <summary>
    /// Discoverer registry change listener
    /// </summary>
    public interface IDiscovererRegistryListener {

        /// <summary>
        /// Called when discoverer is added
        /// </summary>
        /// <param name="context"></param>
        /// <param name="discoverer"></param>
        /// <returns></returns>
        Task OnDiscovererNewAsync(RegistryOperationContextModel context,
            DiscovererModel discoverer);

        /// <summary>
        /// Called when discoverer is updated
        /// </summary>
        /// <param name="context"></param>
        /// <param name="discoverer"></param>
        /// <returns></returns>
        Task OnDiscovererUpdatedAsync(RegistryOperationContextModel context,
            DiscovererModel discoverer);

        /// <summary>
        /// Called when discoverer is delted
        /// </summary>
        /// <param name="context"></param>
        /// <param name="discovererId"></param>
        /// <returns></returns>
        Task OnDiscovererDeletedAsync(RegistryOperationContextModel context,
            string discovererId);
    }
}
