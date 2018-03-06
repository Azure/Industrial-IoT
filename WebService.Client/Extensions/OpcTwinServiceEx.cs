// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IoTSolutions.OpcTwin.WebService.Client {
    using Microsoft.Azure.IoTSolutions.OpcTwin.WebService.Client.Models;
    using System.Collections.Generic;
    using System.Threading.Tasks;

    public static class OpcTwinServiceEx {

        /// <summary>
        /// Get endpoint
        /// </summary>
        /// <param name="service"></param>
        /// <param name="endpointId"></param>
        /// <returns></returns>
        public static Task<TwinRegistrationApiModel> GetEndpointAsync(
            this IOpcTwinService service, string endpointId) =>
            service.GetTwinAsync(endpointId, null);

        /// <summary>
        /// List all registrations
        /// </summary>
        /// <param name="service"></param>
        /// <param name="continuation"></param>
        /// <returns></returns>
        public static Task<TwinRegistrationListApiModel> ListTwinsAsync(
            this IOpcTwinService service, string continuation) =>
            service.ListTwinsAsync(continuation, null);

        /// <summary>
        /// List all registrations
        /// </summary>
        /// <param name="service"></param>
        /// <param name="onlyServerState"></param>
        /// <returns></returns>
        public static async Task<IEnumerable<TwinRegistrationApiModel>> ListTwinsAsync(
            this IOpcTwinService service, bool? onlyServerState = null) {
            var registrations = new List<TwinRegistrationApiModel>();
            var result = await service.ListTwinsAsync(null, onlyServerState);
            registrations.AddRange(result.Items);
            while (result.ContinuationToken != null) {
                result = await service.ListTwinsAsync(result.ContinuationToken,
                    onlyServerState);
                registrations.AddRange(result.Items);
            }
            return registrations;
        }

        /// <summary>
        /// Get server by server info
        /// </summary>
        /// <param name="service"></param>
        /// <param name="applicationUri"></param>
        /// <param name="supervisorId"></param>
        /// <returns></returns>
        public static Task<ServerApiModel> FindServerAsync(this IOpcTwinService service,
            string applicationUri, string supervisorId = null) =>
            service.FindServerAsync(new ServerInfoApiModel {
                ApplicationUri = applicationUri,
                SupervisorId = supervisorId
            });

        /// <summary>
        /// List all servers
        /// </summary>
        /// <param name="service"></param>
        /// <returns></returns>
        public static async Task<IEnumerable<ServerInfoApiModel>> ListServersAsync(
            this IOpcTwinService service) {
            var registrations = new List<ServerInfoApiModel>();
            var result = await service.ListServersAsync(null);
            registrations.AddRange(result.Items);
            while (result.ContinuationToken != null) {
                result = await service.ListServersAsync(result.ContinuationToken);
                registrations.AddRange(result.Items);
            }
            return registrations;
        }

        /// <summary>
        /// List all registrations
        /// </summary>
        /// <param name="service"></param>
        /// <returns></returns>
        public static async Task<IEnumerable<SupervisorApiModel>> ListSupervisorsAsync(
            this IOpcTwinService service) {
            var registrations = new List<SupervisorApiModel>();
            var result = await service.ListSupervisorsAsync(null);
            registrations.AddRange(result.Items);
            while (result.ContinuationToken != null) {
                result = await service.ListSupervisorsAsync(result.ContinuationToken);
                registrations.AddRange(result.Items);
            }
            return registrations;
        }

        /// <summary>
        /// Get list of published nodes
        /// </summary>
        /// <param name="service"></param>
        /// <param name="endpointId"></param>
        /// <returns></returns>
        public static async Task<IEnumerable<PublishedNodeApiModel>> ListPublishedNodesAsync(
            this IOpcTwinService service, string endpointId) {
            var nodes = new List<PublishedNodeApiModel>();
            var result = await service.ListPublishedNodesAsync(null, endpointId);
            nodes.AddRange(result.Items);
            while (result.ContinuationToken != null) {
                result = await service.ListPublishedNodesAsync(result.ContinuationToken,
                    endpointId);
                nodes.AddRange(result.Items);
            }
            return nodes;
        }
    }
}
