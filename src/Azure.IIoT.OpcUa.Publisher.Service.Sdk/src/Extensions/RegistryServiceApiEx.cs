// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Azure.IIoT.OpcUa.Publisher.Service.Sdk
{
    using Azure.IIoT.OpcUa.Publisher.Models;
    using System.Collections.Generic;
    using System.Threading;
    using System.Threading.Tasks;

    /// <summary>
    /// Registry api extensions
    /// </summary>
    public static class RegistryServiceApiEx
    {
        /// <summary>
        /// Find endpoints
        /// </summary>
        /// <param name="service"></param>
        /// <param name="query"></param>
        /// <param name="onlyServerState"></param>
        /// <param name="ct"></param>
        /// <returns></returns>
        public static async Task<IEnumerable<EndpointInfoModel>> QueryAllEndpointsAsync(
            this IRegistryServiceApi service, EndpointRegistrationQueryModel query,
            bool? onlyServerState = null, CancellationToken ct = default)
        {
            var registrations = new List<EndpointInfoModel>();
            var result = await service.QueryEndpointsAsync(query, onlyServerState, null, ct).ConfigureAwait(false);
            registrations.AddRange(result.Items);
            while (result.ContinuationToken != null)
            {
                result = await service.ListEndpointsAsync(result.ContinuationToken,
                    onlyServerState, null, ct).ConfigureAwait(false);
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
        public static async Task<IEnumerable<EndpointInfoModel>> ListAllEndpointsAsync(
            this IRegistryServiceApi service, bool? onlyServerState = null,
            CancellationToken ct = default)
        {
            var registrations = new List<EndpointInfoModel>();
            var result = await service.ListEndpointsAsync(null, onlyServerState, null, ct).ConfigureAwait(false);
            registrations.AddRange(result.Items);
            while (result.ContinuationToken != null)
            {
                result = await service.ListEndpointsAsync(result.ContinuationToken,
                    onlyServerState, null, ct).ConfigureAwait(false);
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
        public static async Task<IEnumerable<ApplicationInfoModel>> QueryAllApplicationsAsync(
            this IRegistryServiceApi service, ApplicationRegistrationQueryModel query,
            CancellationToken ct = default)
        {
            var registrations = new List<ApplicationInfoModel>();
            var result = await service.QueryApplicationsAsync(query, null, ct).ConfigureAwait(false);
            registrations.AddRange(result.Items);
            while (result.ContinuationToken != null)
            {
                result = await service.ListApplicationsAsync(result.ContinuationToken, null, ct).ConfigureAwait(false);
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
        public static async Task<IEnumerable<ApplicationInfoModel>> ListAllApplicationsAsync(
            this IRegistryServiceApi service, CancellationToken ct = default)
        {
            var registrations = new List<ApplicationInfoModel>();
            var result = await service.ListApplicationsAsync(null, null, ct).ConfigureAwait(false);
            registrations.AddRange(result.Items);
            while (result.ContinuationToken != null)
            {
                result = await service.ListApplicationsAsync(result.ContinuationToken, null, ct).ConfigureAwait(false);
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
            this IRegistryServiceApi service, CancellationToken ct = default)
        {
            var sites = new List<string>();
            var result = await service.ListSitesAsync(null, null, ct).ConfigureAwait(false);
            if (result.Sites != null)
            {
                sites.AddRange(result.Sites);
            }

            while (result.ContinuationToken != null)
            {
                result = await service.ListSitesAsync(result.ContinuationToken, null, ct).ConfigureAwait(false);
                if (result.Sites != null)
                {
                    sites.AddRange(result.Sites);
                }
            }
            return sites;
        }

        /// <summary>
        /// List all discoverers
        /// </summary>
        /// <param name="service"></param>
        /// <param name="ct"></param>
        /// <returns></returns>
        public static async Task<IEnumerable<DiscovererModel>> ListAllDiscoverersAsync(
            this IRegistryServiceApi service, CancellationToken ct = default)
        {
            var registrations = new List<DiscovererModel>();
            var result = await service.ListDiscoverersAsync(null, null, ct).ConfigureAwait(false);
            if (result.Items != null)
            {
                registrations.AddRange(result.Items);
            }

            while (result.ContinuationToken != null)
            {
                result = await service.ListDiscoverersAsync(result.ContinuationToken,
                    null, ct).ConfigureAwait(false);
                if (result.Items != null)
                {
                    registrations.AddRange(result.Items);
                }
            }
            return registrations;
        }

        /// <summary>
        /// Find discoverers
        /// </summary>
        /// <param name="service"></param>
        /// <param name="query"></param>
        /// <param name="onlyServerState"></param>
        /// <param name="ct"></param>
        /// <returns></returns>
        public static async Task<IEnumerable<DiscovererModel>> QueryAllDiscoverersAsync(
            this IRegistryServiceApi service, DiscovererQueryModel query, bool? onlyServerState = null,
            CancellationToken ct = default)
        {
            var registrations = new List<DiscovererModel>();
            var result = await service.QueryDiscoverersAsync(query, null, ct).ConfigureAwait(false);
            if (result.Items != null)
            {
                registrations.AddRange(result.Items);
            }

            while (result.ContinuationToken != null)
            {
                result = await service.ListDiscoverersAsync(result.ContinuationToken,
                    null, ct).ConfigureAwait(false);
                if (result.Items != null)
                {
                    registrations.AddRange(result.Items);
                }
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
        public static async Task<IEnumerable<SupervisorModel>> ListAllSupervisorsAsync(
            this IRegistryServiceApi service, bool? onlyServerState = null,
            CancellationToken ct = default)
        {
            var registrations = new List<SupervisorModel>();
            var result = await service.ListSupervisorsAsync(null, onlyServerState, null, ct).ConfigureAwait(false);
            if (result.Items != null)
            {
                registrations.AddRange(result.Items);
            }

            while (result.ContinuationToken != null)
            {
                result = await service.ListSupervisorsAsync(result.ContinuationToken,
                    onlyServerState, null, ct).ConfigureAwait(false);
                if (result.Items != null)
                {
                    registrations.AddRange(result.Items);
                }
            }
            return registrations;
        }

        /// <summary>
        /// Find supervisors
        /// </summary>
        /// <param name="service"></param>
        /// <param name="query"></param>
        /// <param name="onlyServerState"></param>
        /// <param name="ct"></param>
        /// <returns></returns>
        public static async Task<IEnumerable<SupervisorModel>> QueryAllSupervisorsAsync(
            this IRegistryServiceApi service, SupervisorQueryModel query, bool? onlyServerState = null,
            CancellationToken ct = default)
        {
            var registrations = new List<SupervisorModel>();
            var result = await service.QuerySupervisorsAsync(query, onlyServerState, null, ct).ConfigureAwait(false);
            if (result.Items != null)
            {
                registrations.AddRange(result.Items);
            }

            while (result.ContinuationToken != null)
            {
                result = await service.ListSupervisorsAsync(result.ContinuationToken,
                    onlyServerState, null, ct).ConfigureAwait(false);
                if (result.Items != null)
                {
                    registrations.AddRange(result.Items);
                }
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
        public static async Task<IEnumerable<PublisherModel>> ListAllPublishersAsync(
            this IRegistryServiceApi service, bool? onlyServerState = null,
            CancellationToken ct = default)
        {
            var registrations = new List<PublisherModel>();
            var result = await service.ListPublishersAsync(null, onlyServerState, null, ct).ConfigureAwait(false);
            if (result.Items != null)
            {
                registrations.AddRange(result.Items);
            }

            while (result.ContinuationToken != null)
            {
                result = await service.ListPublishersAsync(result.ContinuationToken,
                    onlyServerState, null, ct).ConfigureAwait(false);
                if (result.Items != null)
                {
                    registrations.AddRange(result.Items);
                }
            }
            return registrations;
        }

        /// <summary>
        /// Find publishers
        /// </summary>
        /// <param name="service"></param>
        /// <param name="query"></param>
        /// <param name="onlyServerState"></param>
        /// <param name="ct"></param>
        /// <returns></returns>
        public static async Task<IEnumerable<PublisherModel>> QueryAllPublishersAsync(
            this IRegistryServiceApi service, PublisherQueryModel query, bool? onlyServerState = null,
            CancellationToken ct = default)
        {
            var registrations = new List<PublisherModel>();
            var result = await service.QueryPublishersAsync(query, onlyServerState, null, ct).ConfigureAwait(false);
            if (result.Items != null)
            {
                registrations.AddRange(result.Items);
            }

            while (result.ContinuationToken != null)
            {
                result = await service.ListPublishersAsync(result.ContinuationToken,
                    onlyServerState, null, ct).ConfigureAwait(false);
                if (result.Items != null)
                {
                    registrations.AddRange(result.Items);
                }
            }
            return registrations;
        }

        /// <summary>
        /// List all gateways
        /// </summary>
        /// <param name="service"></param>
        /// <param name="ct"></param>
        /// <returns></returns>
        public static async Task<IEnumerable<GatewayModel>> ListAllGatewaysAsync(
            this IRegistryServiceApi service, CancellationToken ct = default)
        {
            var registrations = new List<GatewayModel>();
            var result = await service.ListGatewaysAsync(null, null, ct).ConfigureAwait(false);
            if (result.Items != null)
            {
                registrations.AddRange(result.Items);
            }

            while (result.ContinuationToken != null)
            {
                result = await service.ListGatewaysAsync(result.ContinuationToken,
                    null, ct).ConfigureAwait(false);
                if (result.Items != null)
                {
                    registrations.AddRange(result.Items);
                }
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
        public static async Task<IEnumerable<GatewayModel>> QueryAllGatewaysAsync(
            this IRegistryServiceApi service, GatewayQueryModel query,
            CancellationToken ct = default)
        {
            var registrations = new List<GatewayModel>();
            var result = await service.QueryGatewaysAsync(query, null, ct).ConfigureAwait(false);
            if (result.Items != null)
            {
                registrations.AddRange(result.Items);
            }

            while (result.ContinuationToken != null)
            {
                result = await service.ListGatewaysAsync(result.ContinuationToken,
                    null, ct).ConfigureAwait(false);
                if (result.Items != null)
                {
                    registrations.AddRange(result.Items);
                }
            }
            return registrations;
        }
    }
}
