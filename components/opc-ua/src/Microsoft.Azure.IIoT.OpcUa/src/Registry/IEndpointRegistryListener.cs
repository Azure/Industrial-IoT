// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IIoT.OpcUa.Registry {
    using Microsoft.Azure.IIoT.OpcUa.Registry.Models;
    using System.Threading.Tasks;

    /// <summary>
    /// Notified when endpoint registry changes
    /// </summary>
    public interface IEndpointRegistryListener {

        /// <summary>
        /// New endpoint
        /// </summary>
        /// <param name="context"></param>
        /// <param name="endpoint"></param>
        /// <returns></returns>
        Task OnEndpointNewAsync(RegistryOperationContextModel context,
            EndpointInfoModel endpoint);

        /// <summary>
        /// New endpoint
        /// </summary>
        /// <param name="context"></param>
        /// <param name="endpoint"></param>
        /// <returns></returns>
        Task OnEndpointActivatedAsync(RegistryOperationContextModel context,
            EndpointInfoModel endpoint);

        /// <summary>
        /// New endpoint
        /// </summary>
        /// <param name="context"></param>
        /// <param name="endpoint"></param>
        /// <returns></returns>
        Task OnEndpointDeactivatedAsync(RegistryOperationContextModel context,
            EndpointInfoModel endpoint);

        /// <summary>
        /// Disabled endpoint
        /// </summary>
        /// <param name="context"></param>
        /// <param name="endpoint"></param>
        /// <returns></returns>
        Task OnEndpointDisabledAsync(RegistryOperationContextModel context,
            EndpointInfoModel endpoint);

        /// <summary>
        /// Enabled endpoint
        /// </summary>
        /// <param name="context"></param>
        /// <param name="endpoint"></param>
        /// <returns></returns>
        Task OnEndpointEnabledAsync(RegistryOperationContextModel context,
            EndpointInfoModel endpoint);

        /// <summary>
        /// New endpoint
        /// </summary>
        /// <param name="context"></param>
        /// <param name="endpoint"></param>
        /// <returns></returns>
        Task OnEndpointUpdatedAsync(RegistryOperationContextModel context,
            EndpointInfoModel endpoint);

        /// <summary>
        /// New endpoint
        /// </summary>
        /// <param name="context"></param>
        /// <param name="endpointId"></param>
        /// <param name="endpoint"></param>
        /// <returns></returns>
        Task OnEndpointDeletedAsync(RegistryOperationContextModel context,
            string endpointId, EndpointInfoModel endpoint);
    }
}
