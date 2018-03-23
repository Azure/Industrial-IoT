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
        public static Task<TwinInfoApiModel> GetEndpointAsync(
            this IOpcTwinService service, string endpointId) =>
            service.GetTwinAsync(endpointId, null);

        /// <summary>
        /// Find twins
        /// </summary>
        /// <param name="service"></param>
        /// <param name="query"></param>
        /// <returns></returns>
        public static Task<TwinInfoListApiModel> QueryTwinsAsync(
            this IOpcTwinService service, TwinRegistrationQueryApiModel query) =>
            service.QueryTwinsAsync(query, null);

        /// <summary>
        /// Find twins
        /// </summary>
        /// <param name="service"></param>
        /// <returns></returns>
        public static async Task<IEnumerable<TwinInfoApiModel>> QueryAllTwinsAsync(
            this IOpcTwinService service, TwinRegistrationQueryApiModel query,
            bool? onlyServerState = null) {
            var registrations = new List<TwinInfoApiModel>();
            var result = await service.QueryTwinsAsync(query, onlyServerState);
            registrations.AddRange(result.Items);
            while (result.ContinuationToken != null) {
                result = await service.ListTwinsAsync(result.ContinuationToken,
                    onlyServerState);
                registrations.AddRange(result.Items);
            }
            return registrations;
        }

        /// <summary>
        /// List twins
        /// </summary>
        /// <param name="service"></param>
        /// <param name="continuation"></param>
        /// <returns></returns>
        public static Task<TwinInfoListApiModel> ListTwinsAsync(
            this IOpcTwinService service, string continuation) =>
            service.ListTwinsAsync(continuation, null);

        /// <summary>
        /// List all twins
        /// </summary>
        /// <param name="service"></param>
        /// <param name="onlyServerState"></param>
        /// <returns></returns>
        public static async Task<IEnumerable<TwinInfoApiModel>> ListAllTwinsAsync(
            this IOpcTwinService service, bool? onlyServerState = null) {
            var registrations = new List<TwinInfoApiModel>();
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
        /// Find applications
        /// </summary>
        /// <param name="service"></param>
        /// <returns></returns>
        public static async Task<IEnumerable<ApplicationInfoApiModel>> QueryAllApplicationsAsync(
            this IOpcTwinService service, ApplicationRegistrationQueryApiModel query) {
            var registrations = new List<ApplicationInfoApiModel>();
            var result = await service.QueryApplicationsAsync(query);
            registrations.AddRange(result.Items);
            while (result.ContinuationToken != null) {
                result = await service.ListApplicationsAsync(result.ContinuationToken);
                registrations.AddRange(result.Items);
            }
            return registrations;
        }

        /// <summary>
        /// List all applications
        /// </summary>
        /// <param name="service"></param>
        /// <returns></returns>
        public static async Task<IEnumerable<ApplicationInfoApiModel>> ListAllApplicationsAsync(
            this IOpcTwinService service) {
            var registrations = new List<ApplicationInfoApiModel>();
            var result = await service.ListApplicationsAsync(null);
            registrations.AddRange(result.Items);
            while (result.ContinuationToken != null) {
                result = await service.ListApplicationsAsync(result.ContinuationToken);
                registrations.AddRange(result.Items);
            }
            return registrations;
        }

        /// <summary>
        /// List all registrations
        /// </summary>
        /// <param name="service"></param>
        /// <returns></returns>
        public static async Task<IEnumerable<SupervisorApiModel>> ListAllSupervisorsAsync(
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
