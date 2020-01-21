// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IIoT.OpcUa.Registry {
    using Microsoft.Azure.IIoT.Exceptions;
    using Microsoft.Azure.IIoT.OpcUa.Registry.Models;
    using System.Collections.Generic;
    using System.Linq;
    using System.Threading;
    using System.Threading.Tasks;

    /// <summary>
    /// Edge Gateway registry extensions
    /// </summary>
    public static class GatewayRegistryEx {

        /// <summary>
        /// Find edge gateway.
        /// </summary>
        /// <param name="service"></param>
        /// <param name="gatewayId"></param>
        /// <param name="onlyServerState"></param>
        /// <param name="ct"></param>
        /// <returns></returns>
        public static async Task<GatewayInfoModel> FindGatewayAsync(
            this IGatewayRegistry service, string gatewayId,
            bool onlyServerState = false, CancellationToken ct = default) {
            try {
                return await service.GetGatewayAsync(gatewayId, onlyServerState, ct);
            }
            catch (ResourceNotFoundException) {
                return null;
            }
        }

        /// <summary>
        /// List all edge gateways
        /// </summary>
        /// <param name="service"></param>
        /// <param name="ct"></param>
        /// <returns></returns>
        public static async Task<List<GatewayModel>> ListAllGatewaysAsync(
            this IGatewayRegistry service, CancellationToken ct = default) {
            var publishers = new List<GatewayModel>();
            var result = await service.ListGatewaysAsync(null, null, ct);
            publishers.AddRange(result.Items);
            while (result.ContinuationToken != null) {
                result = await service.ListGatewaysAsync(result.ContinuationToken,
                    null, ct);
                publishers.AddRange(result.Items);
            }
            return publishers;
        }

        /// <summary>
        /// Returns all edge gateway ids from the registry
        /// </summary>
        /// <param name="service"></param>
        /// <param name="ct"></param>
        /// <returns></returns>
        public static async Task<List<string>> ListAllGatewayIdsAsync(
            this IGatewayRegistry service, CancellationToken ct = default) {
            var publishers = new List<string>();
            var result = await service.ListGatewaysAsync(null, null, ct);
            publishers.AddRange(result.Items.Select(s => s.Id));
            while (result.ContinuationToken != null) {
                result = await service.ListGatewaysAsync(result.ContinuationToken,
                    null, ct);
                publishers.AddRange(result.Items.Select(s => s.Id));
            }
            return publishers;
        }
    }
}
