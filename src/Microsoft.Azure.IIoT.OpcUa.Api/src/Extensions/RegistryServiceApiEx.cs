// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IIoT.OpcUa.Api {
    using Microsoft.Azure.IIoT.OpcUa.Api.Models;
    using System.Collections.Generic;
    using System.Threading.Tasks;

    public static class RegistryServiceApiEx {

        /// <summary>
        /// Find twins
        /// </summary>
        /// <param name="service"></param>
        /// <returns></returns>
        public static async Task<IEnumerable<TwinInfoApiModel>> QueryAllTwinsAsync(
            this IRegistryServiceApi service, TwinRegistrationQueryApiModel query,
            bool? onlyServerState = null) {
            var registrations = new List<TwinInfoApiModel>();
            var result = await service.QueryTwinsAsync(query, onlyServerState, null);
            registrations.AddRange(result.Items);
            while (result.ContinuationToken != null) {
                result = await service.ListTwinsAsync(result.ContinuationToken,
                    onlyServerState, null);
                registrations.AddRange(result.Items);
            }
            return registrations;
        }

        /// <summary>
        /// List all twins
        /// </summary>
        /// <param name="service"></param>
        /// <param name="onlyServerState"></param>
        /// <returns></returns>
        public static async Task<IEnumerable<TwinInfoApiModel>> ListAllTwinsAsync(
            this IRegistryServiceApi service, bool? onlyServerState = null) {
            var registrations = new List<TwinInfoApiModel>();
            var result = await service.ListTwinsAsync(null, onlyServerState, null);
            registrations.AddRange(result.Items);
            while (result.ContinuationToken != null) {
                result = await service.ListTwinsAsync(result.ContinuationToken,
                    onlyServerState, null);
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
            this IRegistryServiceApi service, ApplicationRegistrationQueryApiModel query) {
            var registrations = new List<ApplicationInfoApiModel>();
            var result = await service.QueryApplicationsAsync(query, null);
            registrations.AddRange(result.Items);
            while (result.ContinuationToken != null) {
                result = await service.ListApplicationsAsync(result.ContinuationToken, null);
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
            this IRegistryServiceApi service) {
            var registrations = new List<ApplicationInfoApiModel>();
            var result = await service.ListApplicationsAsync(null, null);
            registrations.AddRange(result.Items);
            while (result.ContinuationToken != null) {
                result = await service.ListApplicationsAsync(result.ContinuationToken, null);
                registrations.AddRange(result.Items);
            }
            return registrations;
        }

        /// <summary>
        /// List all sites
        /// </summary>
        /// <param name="service"></param>
        /// <returns></returns>
        public static async Task<IEnumerable<string>> ListAllSitesAsync(
            this IRegistryServiceApi service) {
            var sites = new List<string>();
            var result = await service.ListSitesAsync(null, null);
            sites.AddRange(result.Sites);
            while (result.ContinuationToken != null) {
                result = await service.ListSitesAsync(result.ContinuationToken, null);
                sites.AddRange(result.Sites);
            }
            return sites;
        }

        /// <summary>
        /// List all supervisors
        /// </summary>
        /// <param name="service"></param>
        /// <returns></returns>
        public static async Task<IEnumerable<SupervisorApiModel>> ListAllSupervisorsAsync(
            this IRegistryServiceApi service) {
            var registrations = new List<SupervisorApiModel>();
            var result = await service.ListSupervisorsAsync(null, null);
            registrations.AddRange(result.Items);
            while (result.ContinuationToken != null) {
                result = await service.ListSupervisorsAsync(result.ContinuationToken, null);
                registrations.AddRange(result.Items);
            }
            return registrations;
        }
    }
}
