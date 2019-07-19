// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IIoT.OpcUa.Registry {
    using Microsoft.Azure.IIoT.Exceptions;
    using Microsoft.Azure.IIoT.OpcUa.Registry.Models;
    using System.Collections.Generic;
    using System.Threading;
    using System.Threading.Tasks;

    /// <summary>
    /// Endpoint registry extensions
    /// </summary>
    public static class EndpointRegistryEx {

        /// <summary>
        /// Find endpoint.
        /// </summary>
        /// <param name="service"></param>
        /// <param name="endpointId"></param>
        /// <param name="ct"></param>
        /// <returns></returns>
        public static async Task<EndpointInfoModel> FindEndpointAsync(
            this IEndpointRegistry service, string endpointId,
            CancellationToken ct = default) {
            try {
                return await service.GetEndpointAsync(endpointId, false, ct);
            }
            catch (ResourceNotFoundException) {
                return null;
            }
        }

        /// <summary>
        /// Find endpoints using query
        /// </summary>
        /// <param name="service"></param>
        /// <param name="query"></param>
        /// <param name="onlyServerState"></param>
        /// <param name="ct"></param>
        /// <returns></returns>
        public static async Task<List<EndpointInfoModel>> QueryAllEndpointsAsync(
            this IEndpointRegistry service, EndpointRegistrationQueryModel query,
            bool onlyServerState = false, CancellationToken ct = default) {
            var registrations = new List<EndpointInfoModel>();
            var result = await service.QueryEndpointsAsync(query, onlyServerState, null, ct);
            registrations.AddRange(result.Items);
            while (result.ContinuationToken != null) {
                result = await service.ListEndpointsAsync(result.ContinuationToken,
                    onlyServerState, null, ct);
                registrations.AddRange(result.Items);
            }
            return registrations;
        }

        /// <summary>
        /// List all endpoints
        /// </summary>
        /// <param name="service"></param>
        /// <param name="onlyServerState"></param>
        /// <param name="ct"></param>
        /// <returns></returns>
        public static async Task<List<EndpointInfoModel>> ListAllEndpointsAsync(
            this IEndpointRegistry service, bool onlyServerState = false,
            CancellationToken ct = default) {
            var registrations = new List<EndpointInfoModel>();
            var result = await service.ListEndpointsAsync(null, onlyServerState, null, ct);
            registrations.AddRange(result.Items);
            while (result.ContinuationToken != null) {
                result = await service.ListEndpointsAsync(result.ContinuationToken,
                    onlyServerState, null, ct);
                registrations.AddRange(result.Items);
            }
            return registrations;
        }
    }
}
