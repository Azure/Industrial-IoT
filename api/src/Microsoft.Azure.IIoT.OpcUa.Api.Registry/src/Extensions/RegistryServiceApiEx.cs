// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IIoT.OpcUa.Api.Registry {
    using Microsoft.Azure.IIoT.OpcUa.Api.Registry.Models;
    using System.Collections.Generic;
    using System.Threading;
    using System.Threading.Tasks;

    /// <summary>
    /// Registry api extensions
    /// </summary>
    public static class RegistryServiceApiEx {

        /// <summary>
        /// Find endpoints
        /// </summary>
        /// <param name="service"></param>
        /// <param name="query"></param>
        /// <param name="onlyServerState"></param>
        /// <param name="ct"></param>
        /// <returns></returns>
        public static async Task<IEnumerable<EndpointInfoApiModel>> QueryAllEndpointsAsync(
            this IRegistryServiceApi service, EndpointRegistrationQueryApiModel query,
            bool? onlyServerState = null, CancellationToken ct = default) {
            var registrations = new List<EndpointInfoApiModel>();
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
        public static async Task<IEnumerable<EndpointInfoApiModel>> ListAllEndpointsAsync(
            this IRegistryServiceApi service, bool? onlyServerState = null,
            CancellationToken ct = default) {
            var registrations = new List<EndpointInfoApiModel>();
            var result = await service.ListEndpointsAsync(null, onlyServerState, null, ct);
            registrations.AddRange(result.Items);
            while (result.ContinuationToken != null) {
                result = await service.ListEndpointsAsync(result.ContinuationToken,
                    onlyServerState, null, ct);
                registrations.AddRange(result.Items);
            }
            return registrations;
        }

        /// <summary>
        /// Find applications
        /// </summary>
        /// <param name="service"></param>
        /// <param name="query"></param>
        /// <param name="ct"></param>
        /// <returns></returns>
        public static async Task<IEnumerable<ApplicationInfoApiModel>> QueryAllApplicationsAsync(
            this IRegistryServiceApi service, ApplicationRegistrationQueryApiModel query,
            CancellationToken ct = default) {
            var registrations = new List<ApplicationInfoApiModel>();
            var result = await service.QueryApplicationsAsync(query, null, ct);
            registrations.AddRange(result.Items);
            while (result.ContinuationToken != null) {
                result = await service.ListApplicationsAsync(result.ContinuationToken, null, ct);
                registrations.AddRange(result.Items);
            }
            return registrations;
        }

        /// <summary>
        /// List all applications
        /// </summary>
        /// <param name="service"></param>
        /// <param name="ct"></param>
        /// <returns></returns>
        public static async Task<IEnumerable<ApplicationInfoApiModel>> ListAllApplicationsAsync(
            this IRegistryServiceApi service, CancellationToken ct = default) {
            var registrations = new List<ApplicationInfoApiModel>();
            var result = await service.ListApplicationsAsync(null, null, ct);
            registrations.AddRange(result.Items);
            while (result.ContinuationToken != null) {
                result = await service.ListApplicationsAsync(result.ContinuationToken, null, ct);
                registrations.AddRange(result.Items);
            }
            return registrations;
        }

        /// <summary>
        /// List all sites
        /// </summary>
        /// <param name="service"></param>
        /// <param name="ct"></param>
        /// <returns></returns>
        public static async Task<IEnumerable<string>> ListAllSitesAsync(
            this IRegistryServiceApi service, CancellationToken ct = default) {
            var sites = new List<string>();
            var result = await service.ListSitesAsync(null, null, ct);
            sites.AddRange(result.Sites);
            while (result.ContinuationToken != null) {
                result = await service.ListSitesAsync(result.ContinuationToken, null, ct);
                sites.AddRange(result.Sites);
            }
            return sites;
        }

        /// <summary>
        /// List all discoverers
        /// </summary>
        /// <param name="service"></param>
        /// <param name="ct"></param>
        /// <returns></returns>
        public static async Task<IEnumerable<DiscovererApiModel>> ListAllDiscoverersAsync(
            this IRegistryServiceApi service, CancellationToken ct = default) {
            var registrations = new List<DiscovererApiModel>();
            var result = await service.ListDiscoverersAsync(null, null, ct);
            registrations.AddRange(result.Items);
            while (result.ContinuationToken != null) {
                result = await service.ListDiscoverersAsync(result.ContinuationToken,
                    null, ct);
                registrations.AddRange(result.Items);
            }
            return registrations;
        }

        /// <summary>
        /// Find discoverers
        /// </summary>
        /// <param name="service"></param>
        /// <param name="onlyServerState"></param>
        /// <param name="query"></param>
        /// <param name="ct"></param>
        /// <returns></returns>
        public static async Task<IEnumerable<DiscovererApiModel>> QueryAllDiscoverersAsync(
            this IRegistryServiceApi service, DiscovererQueryApiModel query, bool? onlyServerState = null,
            CancellationToken ct = default) {
            var registrations = new List<DiscovererApiModel>();
            var result = await service.QueryDiscoverersAsync(query, null, ct);
            registrations.AddRange(result.Items);
            while (result.ContinuationToken != null) {
                result = await service.ListDiscoverersAsync(result.ContinuationToken,
                    null, ct);
                registrations.AddRange(result.Items);
            }
            return registrations;
        }

        /// <summary>
        /// List all supervisors
        /// </summary>
        /// <param name="service"></param>
        /// <param name="onlyServerState"></param>
        /// <param name="ct"></param>
        /// <returns></returns>
        public static async Task<IEnumerable<SupervisorApiModel>> ListAllSupervisorsAsync(
            this IRegistryServiceApi service, bool? onlyServerState = null,
            CancellationToken ct = default) {
            var registrations = new List<SupervisorApiModel>();
            var result = await service.ListSupervisorsAsync(null, onlyServerState, null, ct);
            registrations.AddRange(result.Items);
            while (result.ContinuationToken != null) {
                result = await service.ListSupervisorsAsync(result.ContinuationToken,
                    onlyServerState, null, ct);
                registrations.AddRange(result.Items);
            }
            return registrations;
        }

        /// <summary>
        /// Find supervisors
        /// </summary>
        /// <param name="service"></param>
        /// <param name="onlyServerState"></param>
        /// <param name="query"></param>
        /// <param name="ct"></param>
        /// <returns></returns>
        public static async Task<IEnumerable<SupervisorApiModel>> QueryAllSupervisorsAsync(
            this IRegistryServiceApi service, SupervisorQueryApiModel query, bool? onlyServerState = null,
            CancellationToken ct = default) {
            var registrations = new List<SupervisorApiModel>();
            var result = await service.QuerySupervisorsAsync(query, onlyServerState, null, ct);
            registrations.AddRange(result.Items);
            while (result.ContinuationToken != null) {
                result = await service.ListSupervisorsAsync(result.ContinuationToken,
                    onlyServerState, null, ct);
                registrations.AddRange(result.Items);
            }
            return registrations;
        }

        /// <summary>
        /// List all publishers
        /// </summary>
        /// <param name="service"></param>
        /// <param name="onlyServerState"></param>
        /// <param name="ct"></param>
        /// <returns></returns>
        public static async Task<IEnumerable<PublisherApiModel>> ListAllPublishersAsync(
            this IRegistryServiceApi service, bool? onlyServerState = null,
            CancellationToken ct = default) {
            var registrations = new List<PublisherApiModel>();
            var result = await service.ListPublishersAsync(null, onlyServerState, null, ct);
            registrations.AddRange(result.Items);
            while (result.ContinuationToken != null) {
                result = await service.ListPublishersAsync(result.ContinuationToken,
                    onlyServerState, null, ct);
                registrations.AddRange(result.Items);
            }
            return registrations;
        }

        /// <summary>
        /// Find publishers
        /// </summary>
        /// <param name="service"></param>
        /// <param name="onlyServerState"></param>
        /// <param name="query"></param>
        /// <param name="ct"></param>
        /// <returns></returns>
        public static async Task<IEnumerable<PublisherApiModel>> QueryAllPublishersAsync(
            this IRegistryServiceApi service, PublisherQueryApiModel query, bool? onlyServerState = null,
            CancellationToken ct = default) {
            var registrations = new List<PublisherApiModel>();
            var result = await service.QueryPublishersAsync(query, onlyServerState, null, ct);
            registrations.AddRange(result.Items);
            while (result.ContinuationToken != null) {
                result = await service.ListPublishersAsync(result.ContinuationToken,
                    onlyServerState, null, ct);
                registrations.AddRange(result.Items);
            }
            return registrations;
        }


        /// <summary>
        /// List all gateways
        /// </summary>
        /// <param name="service"></param>
        /// <param name="ct"></param>
        /// <returns></returns>
        public static async Task<IEnumerable<GatewayApiModel>> ListAllGatewaysAsync(
            this IRegistryServiceApi service, CancellationToken ct = default) {
            var registrations = new List<GatewayApiModel>();
            var result = await service.ListGatewaysAsync(null, null, ct);
            registrations.AddRange(result.Items);
            while (result.ContinuationToken != null) {
                result = await service.ListGatewaysAsync(result.ContinuationToken,
                    null, ct);
                registrations.AddRange(result.Items);
            }
            return registrations;
        }

        /// <summary>
        /// Find gateways
        /// </summary>
        /// <param name="service"></param>
        /// <param name="query"></param>
        /// <param name="ct"></param>
        /// <returns></returns>
        public static async Task<IEnumerable<GatewayApiModel>> QueryAllGatewaysAsync(
            this IRegistryServiceApi service, GatewayQueryApiModel query,
            CancellationToken ct = default) {
            var registrations = new List<GatewayApiModel>();
            var result = await service.QueryGatewaysAsync(query, null, ct);
            registrations.AddRange(result.Items);
            while (result.ContinuationToken != null) {
                result = await service.ListGatewaysAsync(result.ContinuationToken,
                    null, ct);
                registrations.AddRange(result.Items);
            }
            return registrations;
        }
    }
}
