// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Azure.IIoT.OpcUa.Publisher.Service
{
    using Azure.IIoT.OpcUa.Publisher.Models;
    using System.Threading.Tasks;

    /// <summary>
    /// Notified when endpoint registry changes
    /// </summary>
    public interface IEndpointRegistryListener
    {
        /// <summary>
        /// New endpoint
        /// </summary>
        /// <param name="context"></param>
        /// <param name="endpoint"></param>
        /// <returns></returns>
        Task OnEndpointNewAsync(OperationContextModel? context,
            EndpointInfoModel endpoint);

        /// <summary>
        /// Disabled endpoint
        /// </summary>
        /// <param name="context"></param>
        /// <param name="endpoint"></param>
        /// <returns></returns>
        Task OnEndpointDisabledAsync(OperationContextModel? context,
            EndpointInfoModel endpoint);

        /// <summary>
        /// Enabled endpoint
        /// </summary>
        /// <param name="context"></param>
        /// <param name="endpoint"></param>
        /// <returns></returns>
        Task OnEndpointEnabledAsync(OperationContextModel? context,
            EndpointInfoModel endpoint);

        /// <summary>
        /// Deleted endpoint
        /// </summary>
        /// <param name="context"></param>
        /// <param name="endpointId"></param>
        /// <param name="endpoint"></param>
        /// <returns></returns>
        Task OnEndpointDeletedAsync(OperationContextModel? context,
            string endpointId, EndpointInfoModel endpoint);
    }
}
