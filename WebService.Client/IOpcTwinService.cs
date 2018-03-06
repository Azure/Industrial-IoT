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
        /// Register new twin
        /// </summary>
        /// <param name="content"></param>
        /// <returns></returns>
        Task<TwinRegistrationResponseApiModel> RegisterTwinAsync(
            TwinRegistrationRequestApiModel content);

        /// <summary>
        /// Update twin registration
        /// </summary>
        /// <param name="content"></param>
        /// <returns></returns>
        Task UpdateTwinAsync(
            TwinRegistrationUpdateApiModel content);

        /// <summary>
        /// Get twin
        /// </summary>
        /// <param name="twinId"></param>
        /// <param name="onlyServerState"></param>
        /// <returns></returns>
        Task<TwinRegistrationApiModel> GetTwinAsync(
            string twinId, bool? onlyServerState);

        /// <summary>
        /// List all twins
        /// </summary>
        /// <param name="continuation"></param>
        /// <param name="onlyServerState"></param>
        /// <returns></returns>
        Task<TwinRegistrationListApiModel> ListTwinsAsync(
            string continuation, bool? onlyServerState);

        /// <summary>
        /// Delete twin
        /// </summary>
        /// <param name="twinId"></param>
        /// <returns></returns>
        Task DeleteTwinAsync(string twinId);

        /// <summary>
        /// Find server based on info criteria
        /// </summary>
        /// <param name="info"></param>
        /// <returns></returns>
        Task<ServerApiModel> FindServerAsync(
            ServerInfoApiModel info);

        /// <summary>
        /// Get server for specified unique server id
        /// </summary>
        /// <param name="serverId"></param>
        /// <returns></returns>
        Task<ServerApiModel> GetServerAsync(string serverId);

        /// <summary>
        /// List all servers
        /// </summary>
        /// <param name="continuation"></param>
        /// <returns></returns>
        Task<ServerInfoListApiModel> ListServersAsync(
            string continuation);

        /// <summary>
        /// Get supervisor
        /// </summary>
        /// <param name="supervisorId"></param>
        /// <returns></returns>
        Task<SupervisorApiModel> GetSupervisorAsync(
            string supervisorId);

        /// <summary>
        /// Update supervisor registration
        /// </summary>
        /// <param name="content"></param>
        /// <returns></returns>
        Task UpdateSupervisorAsync(
            SupervisorUpdateApiModel content);

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
        /// <param name="content"></param>
        /// <returns></returns>
        Task<BrowseResponseApiModel> NodeBrowseAsync(string twinId,
            BrowseRequestApiModel content);

        /// <summary>
        /// Call method on twin
        /// </summary>
        /// <param name="twinId"></param>
        /// <param name="content"></param>
        /// <returns></returns>
        Task<MethodCallResponseApiModel> NodeMethodCallAsync(string twinId,
            MethodCallRequestApiModel content);

        /// <summary>
        /// Get meta data for method call on twin
        /// </summary>
        /// <param name="twinId"></param>
        /// <param name="content"></param>
        /// <returns></returns>
        Task<MethodMetadataResponseApiModel> NodeMethodGetMetadataAsync(
            string twinId, MethodMetadataRequestApiModel content);

        /// <summary>
        /// Publish or unpublish node on twin
        /// </summary>
        /// <param name="twinId"></param>
        /// <param name="content"></param>
        /// <returns></returns>
        Task<PublishResponseApiModel> NodePublishAsync(string twinId,
            PublishRequestApiModel content);

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
        /// <param name="content"></param>
        /// <returns></returns>
        Task<ValueReadResponseApiModel> NodeValueReadAsync(string twinId,
            ValueReadRequestApiModel content);

        /// <summary>
        /// Write node value on twin
        /// </summary>
        /// <param name="twinId"></param>
        /// <param name="content"></param>
        /// <returns></returns>
        Task<ValueWriteResponseApiModel> NodeValueWriteAsync(string twinId,
            ValueWriteRequestApiModel content);

        /// <summary>
        /// Returns the server certificate of the twin
        /// </summary>
        /// <param name="twinId"></param>
        /// <returns></returns>
        Task<string> GetServerCertificateAsync(string twinId);
    }
}