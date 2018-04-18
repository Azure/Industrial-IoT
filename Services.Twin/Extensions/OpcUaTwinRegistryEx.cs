// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IoTSolutions.OpcTwin.Services {
    using Microsoft.Azure.IoTSolutions.OpcTwin.Services.Models;
    using Microsoft.Azure.IoTSolutions.Common.Exceptions;
    using System.Threading.Tasks;

    public static class OpcUaTwinRegistryEx {

        /// <summary>
        /// Get endpoint registration by identifer.
        /// If endoint does not exist, null is returned.
        /// </summary>
        /// <param name="id"></param>
        /// <returns></returns>
        public static async Task<EndpointModel> GetEndpointAsync(
            this IOpcUaTwinRegistry service, string id) {
            var registration = await service.GetTwinAsync(id, false);
            if (registration == null) {
                throw new ResourceNotFoundException($"Endpoint {id} not found.");
            }
            return registration.Endpoint;
        }

        /// <summary>
        /// Find endpoint or return passed in endpoint
        /// </summary>
        /// <param name="endpoint"></param>
        /// <returns></returns>
        public static async Task<EndpointModel> FindEndpointAsync(
            this IOpcUaTwinRegistry service, EndpointModel endpoint) {
            var registration = await service.FindTwinAsync(endpoint, false);
            return registration?.Endpoint ?? endpoint;
        }

        /// <summary>
        /// Get twin registrations in paged form
        /// </summary>
        /// <param name="continuation"></param>
        /// <returns></returns>
        public static Task<TwinInfoListModel> ListTwinsAsync(
            this IOpcUaTwinRegistry service, string continuation) =>
            service.ListTwinsAsync(continuation, null);

        /// <summary>
        /// Get twin registrations in paged form
        /// </summary>
        /// <param name="continuation"></param>
        /// <returns></returns>
        public static Task<TwinInfoListModel> ListTwinsAsync(
            this IOpcUaTwinRegistry service, string continuation, int? pageSize) =>
            service.ListTwinsAsync(continuation, false, pageSize);
    }
}
