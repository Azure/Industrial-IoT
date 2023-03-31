// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Azure.IIoT.OpcUa.Publisher.Service
{
    using Azure.IIoT.OpcUa.Publisher.Models;
    using System.Threading.Tasks;

    /// <summary>
    /// Gateway registry change listener
    /// </summary>
    public interface IGatewayRegistryListener
    {
        /// <summary>
        /// Called when gateway is created
        /// </summary>
        /// <param name="context"></param>
        /// <param name="gateway"></param>
        /// <returns></returns>
        Task OnGatewayNewAsync(OperationContextModel? context,
            GatewayModel gateway);

        /// <summary>
        /// Called when gateway is updated
        /// </summary>
        /// <param name="context"></param>
        /// <param name="gateway"></param>
        /// <returns></returns>
        Task OnGatewayUpdatedAsync(OperationContextModel? context,
            GatewayModel gateway);

        /// <summary>
        /// Called when gateway is deleted
        /// </summary>
        /// <param name="context"></param>
        /// <param name="gatewayId"></param>
        /// <returns></returns>
        Task OnGatewayDeletedAsync(OperationContextModel? context,
            string gatewayId);
    }
}
