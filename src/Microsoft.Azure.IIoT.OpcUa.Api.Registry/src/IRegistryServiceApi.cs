// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IIoT.OpcUa.Api.Registry {
    using Microsoft.Azure.IIoT.OpcUa.Api.Registry.Models;
    using System;
    using System.Threading.Tasks;

    /// <summary>
    /// Registry api calls
    /// </summary>
    public interface IRegistryServiceApi {

        /// <summary>
        /// Returns status of the service
        /// </summary>
        /// <returns></returns>
        Task<StatusResponseApiModel> GetServiceStatusAsync();

        /// <summary>
        /// Kick off onboarding of new server
        /// </summary>
        /// <param name="request"></param>
        /// <returns></returns>
        Task RegisterAsync(ServerRegistrationRequestApiModel request);

        /// <summary>
        /// Kick off a one time discovery on all supervisors
        /// </summary>
        /// <param name="request"></param>
        /// <returns></returns>
        Task DiscoverAsync(DiscoveryRequestApiModel request);

        /// <summary>
        /// Register new application.
        /// </summary>
        /// <param name="request"></param>
        /// <returns></returns>
        Task<ApplicationRegistrationResponseApiModel> RegisterAsync(
            ApplicationRegistrationRequestApiModel request);

        /// <summary>
        /// Get application for specified unique application id
        /// </summary>
        /// <param name="applicationId"></param>
        /// <returns></returns>
        Task<ApplicationRegistrationApiModel> GetApplicationAsync(
            string applicationId);

        /// <summary>
        /// Register new application and all twins with it.
        /// </summary>
        /// <param name="request"></param>
        /// <returns></returns>
        Task UpdateApplicationAsync(
            ApplicationRegistrationUpdateApiModel request);

        /// <summary>
        /// List all Application sites to visually group applications.
        /// </summary>
        /// <param name="continuation"></param>
        /// <param name="pageSize"></param>
        /// <returns></returns>
        Task<ApplicationSiteListApiModel> ListSitesAsync(
            string continuation, int? pageSize);

        /// <summary>
        /// List all applications or continue a QueryApplications
        /// call.
        /// </summary>
        /// <param name="continuation"></param>
        /// <param name="pageSize"></param>
        /// <returns></returns>
        Task<ApplicationInfoListApiModel> ListApplicationsAsync(
            string continuation, int? pageSize);

        /// <summary>
        /// Find applications based on specified criteria. Pass
        /// continuation token if any returned to ListApplications to
        /// retrieve remaining items.
        /// </summary>
        /// <param name="query"></param>
        /// <param name="pageSize"></param>
        /// <returns></returns>
        Task<ApplicationInfoListApiModel> QueryApplicationsAsync(
            ApplicationRegistrationQueryApiModel query, int? pageSize);

        /// <summary>
        /// Unregister and delete application and all twins.
        /// </summary>
        /// <param name="applicationId"></param>
        /// <returns></returns>
        Task UnregisterApplicationAsync(string applicationId);

        /// <summary>
        /// Unregister disabled applications not seen since specified
        /// amount of time.
        /// </summary>
        /// <param name="notSeenSince"></param>
        /// <returns></returns>
        Task PurgeDisabledApplicationsAsync(TimeSpan notSeenSince);

        /// <summary>
        /// Get twin
        /// </summary>
        /// <param name="twinId"></param>
        /// <param name="onlyServerState"></param>
        /// <returns></returns>
        Task<TwinInfoApiModel> GetTwinAsync(
            string twinId, bool? onlyServerState);

        /// <summary>
        /// Set twin activation state to activated
        /// </summary>
        /// <param name="id"></param>
        /// <returns></returns>
        Task ActivateTwinAsync(string id);

        /// <summary>
        /// Update twin registration
        /// </summary>
        /// <param name="request"></param>
        /// <returns></returns>
        Task UpdateTwinAsync(
            TwinRegistrationUpdateApiModel request);

        /// <summary>
        /// Set twin activation state to deactivated
        /// </summary>
        /// <param name="id"></param>
        /// <returns></returns>
        Task DeactivateTwinAsync(string id);

        /// <summary>
        /// List all twins
        /// </summary>
        /// <param name="continuation"></param>
        /// <param name="onlyServerState"></param>
        /// <param name="pageSize"></param>
        /// <returns></returns>
        Task<TwinInfoListApiModel> ListTwinsAsync(
            string continuation, bool? onlyServerState, int? pageSize);

        /// <summary>
        /// Find twins based on specified critiria. Pass continuation
        /// token if any is returned to ListTwins to retrieve
        /// the remaining items
        /// </summary>
        /// <param name="query"></param>
        /// <param name="onlyServerState"></param>
        /// <param name="pageSize"></param>
        /// <returns></returns>
        Task<TwinInfoListApiModel> QueryTwinsAsync(
            TwinRegistrationQueryApiModel query, bool? onlyServerState,
            int? pageSize);

        /// <summary>
        /// Get supervisor
        /// </summary>
        /// <param name="supervisorId"></param>
        /// <returns></returns>
        Task<SupervisorApiModel> GetSupervisorAsync(
            string supervisorId);

        /// <summary>
        /// Update supervisor including config updates.
        /// </summary>
        /// <param name="request"></param>
        /// <returns></returns>
        Task UpdateSupervisorAsync(SupervisorUpdateApiModel request);

        /// <summary>
        /// List all supervisors
        /// </summary>
        /// <param name="continuation"></param>
        /// <param name="pageSize"></param>
        /// <returns></returns>
        Task<SupervisorListApiModel> ListSupervisorsAsync(
            string continuation, int? pageSize);

        /// <summary>
        /// Find supervisors based on specified criteria. Pass
        /// continuation token if any returned to ListSupervisors to
        /// retrieve remaining items.
        /// </summary>
        /// <param name="query"></param>
        /// <param name="pageSize"></param>
        /// <returns></returns>
        Task<SupervisorListApiModel> QuerySupervisorsAsync(
            SupervisorQueryApiModel query, int? pageSize);
    }
}
