// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IIoT.OpcUa.Registry {
    using Microsoft.Azure.IIoT.Exceptions;
    using Microsoft.Azure.IIoT.OpcUa.Registry.Models;
    using Microsoft.Azure.IIoT.Utils;
    using System.Collections.Generic;
    using System.Threading;
    using System.Threading.Tasks;

    /// <summary>
    /// Application registry extensions
    /// </summary>
    public static class ApplicationRegistryEx {

        /// <summary>
        /// Find application.
        /// </summary>
        /// <param name="service"></param>
        /// <param name="applicationId"></param>
        /// <param name="ct"></param>
        /// <returns></returns>
        public static async Task<ApplicationRegistrationModel> FindApplicationAsync(
            this IApplicationRegistry service, string applicationId,
            CancellationToken ct = default) {
            try {
                return await service.GetApplicationAsync(applicationId, false, ct);
            }
            catch (ResourceNotFoundException) {
                return null;
            }
        }

        /// <summary>
        /// Find applications
        /// </summary>
        /// <param name="service"></param>
        /// <param name="query"></param>
        /// <param name="ct"></param>
        /// <returns></returns>
        public static async Task<List<ApplicationInfoModel>> QueryAllApplicationsAsync(
            this IApplicationRegistry service, ApplicationRegistrationQueryModel query,
            CancellationToken ct = default) {
            var registrations = new List<ApplicationInfoModel>();
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
        public static async Task<List<ApplicationInfoModel>> ListAllApplicationsAsync(
            this IApplicationRegistry service, CancellationToken ct = default) {
            var registrations = new List<ApplicationInfoModel>();
            var result = await service.ListApplicationsAsync(null, null, ct);
            registrations.AddRange(result.Items);
            while (result.ContinuationToken != null) {
                result = await service.ListApplicationsAsync(result.ContinuationToken, null, ct);
                registrations.AddRange(result.Items);
            }
            return registrations;
        }

        /// <summary>
        /// List all application registrations
        /// </summary>
        /// <param name="service"></param>
        /// <param name="ct"></param>
        /// <returns></returns>
        public static async Task<List<ApplicationRegistrationModel>> ListAllRegistrationsAsync(
            this IApplicationRegistry service, CancellationToken ct = default) {
            var registrations = new List<ApplicationRegistrationModel>();
            var infos = await service.ListAllApplicationsAsync(ct);
            foreach (var info in infos) {
                var registration = await service.GetApplicationAsync(info.ApplicationId, false, ct);
                registrations.Add(registration);
            }
            return registrations;
        }

        /// <summary>
        /// Unregister all applications and endpoints
        /// </summary>
        /// <param name="service"></param>
        /// <param name="ct"></param>
        /// <returns></returns>
        public static async Task UnregisterAllApplicationsAsync(
            this IApplicationRegistry service, CancellationToken ct = default) {
            var apps = await service.ListAllApplicationsAsync(ct);
            foreach (var app in apps) {
                await Try.Async(() => service.UnregisterApplicationAsync(
                    app.ApplicationId, null, ct));
            }
        }

        /// <summary>
        /// List all sites
        /// </summary>
        /// <param name="service"></param>
        /// <param name="ct"></param>
        /// <returns></returns>
        public static async Task<List<string>> ListAllSitesAsync(
            this IApplicationRegistry service, CancellationToken ct = default) {
            var sites = new List<string>();
            var result = await service.ListSitesAsync(null, null, ct);
            sites.AddRange(result.Sites);
            while (result.ContinuationToken != null) {
                result = await service.ListSitesAsync(result.ContinuationToken, null, ct);
                sites.AddRange(result.Sites);
            }
            return sites;
        }
    }
}
