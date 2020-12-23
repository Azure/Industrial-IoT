// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IIoT.OpcUa.Registry {
    using Microsoft.Azure.IIoT.OpcUa.Registry.Models;
    using System.Threading.Tasks;

    /// <summary>
    /// Publisher registry change listener
    /// </summary>
    public interface IPublisherRegistryListener {

        /// <summary>
        /// Called when publisher is created
        /// </summary>
        /// <param name="context"></param>
        /// <param name="publisher"></param>
        /// <returns></returns>
        Task OnPublisherNewAsync(RegistryOperationContextModel context,
            PublisherModel publisher);

        /// <summary>
        /// Called when publisher is updated
        /// </summary>
        /// <param name="context"></param>
        /// <param name="publisher"></param>
        /// <returns></returns>
        Task OnPublisherUpdatedAsync(RegistryOperationContextModel context,
            PublisherModel publisher);

        /// <summary>
        /// Called when publisher is deleted
        /// </summary>
        /// <param name="context"></param>
        /// <param name="publisherId"></param>
        /// <returns></returns>
        Task OnPublisherDeletedAsync(RegistryOperationContextModel context,
            string publisherId);
    }
}
