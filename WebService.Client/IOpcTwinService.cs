// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IoTSolutions.OpcTwin.WebService.Client {
    using Microsoft.Azure.IoTSolutions.OpcTwin.WebService.Client.Models;
    using System.Threading.Tasks;

    /// <summary>
    /// Represents OPC twin service api functions
    /// </summary>
    public interface IOpcTwinService {

        /// <summary>
        /// Returns status of the service
        /// </summary>
        /// <returns></returns>
        Task<StatusResponseApiModel> GetServiceStatusAsync();

        /// <summary>
        /// Register new server and all twins with it.
        /// </summary>
        /// <param name="request"></param>
        /// <returns></returns>
        Task<ApplicationRegistrationResponseApiModel> RegisterAsync(
            ServerRegistrationRequestApiModel request);

        /// <summary>
        /// Register new application, does not register twins.
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
        /// Returns the certificate of the application
        /// </summary>
        /// <param name="applicationId"></param>
        /// <returns></returns>
        Task<string> GetCertificateAsync(string applicationId);

        /// <summary>
        /// Register new application and all twins with it.
        /// </summary>
        /// <param name="request"></param>
        /// <returns></returns>
        Task UpdateApplicationAsync(
            ApplicationRegistrationUpdateApiModel request);

        /// <summary>
        /// List all applications or continue a QueryApplications
        /// call.
        /// </summary>
        /// <param name="continuation"></param>
        /// <returns></returns>
        Task<ApplicationInfoListApiModel> ListApplicationsAsync(
            string continuation);

        /// <summary>
        /// Find applications based on specified criteria. Pass
        /// continuation token if any returned to ListApplications to
        /// retrieve remaining items.
        /// </summary>
        /// <param name="query"></param>
        /// <returns></returns>
        Task<ApplicationInfoListApiModel> QueryApplicationsAsync(
            ApplicationRegistrationQueryApiModel query);

        /// <summary>
        /// Unregister and delete application and all twins.
        /// </summary>
        /// <param name="applicationId"></param>
        /// <returns></returns>
        Task UnregisterApplicationAsync(string applicationId);

        /// <summary>
        /// Get twin
        /// </summary>
        /// <param name="twinId"></param>
        /// <param name="onlyServerState"></param>
        /// <returns></returns>
        Task<TwinInfoApiModel> GetTwinAsync(
            string twinId, bool? onlyServerState);

        /// <summary>
        /// Update twin registration
        /// </summary>
        /// <param name="request"></param>
        /// <returns></returns>
        Task UpdateTwinAsync(
            TwinRegistrationUpdateApiModel request);

        /// <summary>
        /// List all twins
        /// </summary>
        /// <param name="continuation"></param>
        /// <param name="onlyServerState"></param>
        /// <returns></returns>
        Task<TwinInfoListApiModel> ListTwinsAsync(
            string continuation, bool? onlyServerState);

        /// <summary>
        /// Find twins based on specified critiria. Pass continuation
        /// token if any is returned to ListTwins to retrieve
        /// the remaining items
        /// </summary>
        /// <param name="query"></param>
        /// <param name="onlyServerState"></param>
        /// <returns></returns>
        Task<TwinInfoListApiModel> QueryTwinsAsync(
            TwinRegistrationQueryApiModel query, bool? onlyServerState);

        /// <summary>
        /// Get supervisor
        /// </summary>
        /// <param name="supervisorId"></param>
        /// <returns></returns>
        Task<SupervisorApiModel> GetSupervisorAsync(
            string supervisorId);

        /// <summary>
        /// Update supervisor including configuration updates.
        /// </summary>
        /// <param name="request"></param>
        /// <returns></returns>
        Task UpdateSupervisorAsync(
            SupervisorUpdateApiModel request);

        /// <summary>
        /// List all sueprvisors
        /// </summary>
        /// <param name="continuation"></param>
        /// <returns></returns>
        Task<SupervisorListApiModel> ListSupervisorsAsync(
            string continuation);

        /// <summary>
        /// Browse node on twin
        /// </summary>
        /// <param name="twinId"></param>
        /// <param name="request"></param>
        /// <returns></returns>
        Task<BrowseResponseApiModel> NodeBrowseAsync(string twinId,
            BrowseRequestApiModel request);

        /// <summary>
        /// Call method on twin
        /// </summary>
        /// <param name="twinId"></param>
        /// <param name="request"></param>
        /// <returns></returns>
        Task<MethodCallResponseApiModel> NodeMethodCallAsync(string twinId,
            MethodCallRequestApiModel request);

        /// <summary>
        /// Get meta data for method call on twin
        /// </summary>
        /// <param name="twinId"></param>
        /// <param name="request"></param>
        /// <returns></returns>
        Task<MethodMetadataResponseApiModel> NodeMethodGetMetadataAsync(
            string twinId, MethodMetadataRequestApiModel request);

        /// <summary>
        /// Publish or unpublish node on twin
        /// </summary>
        /// <param name="twinId"></param>
        /// <param name="request"></param>
        /// <returns></returns>
        Task<PublishResponseApiModel> NodePublishAsync(string twinId,
            PublishRequestApiModel request);

        /// <summary>
        /// Get list of published nodes on twin
        /// </summary>
        /// <param name="continuation"></param>
        /// <param name="twinId"></param>
        /// <returns></returns>
        Task<PublishedNodeListApiModel> ListPublishedNodesAsync(
            string continuation, string twinId);

        /// <summary>
        /// Read node value on twin
        /// </summary>
        /// <param name="twinId"></param>
        /// <param name="request"></param>
        /// <returns></returns>
        Task<ValueReadResponseApiModel> NodeValueReadAsync(string twinId,
            ValueReadRequestApiModel request);

        /// <summary>
        /// Write node value on twin
        /// </summary>
        /// <param name="twinId"></param>
        /// <param name="request"></param>
        /// <returns></returns>
        Task<ValueWriteResponseApiModel> NodeValueWriteAsync(string twinId,
            ValueWriteRequestApiModel request);
    }
}