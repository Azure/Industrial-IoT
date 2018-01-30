// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IoTSolutions.Shared.External {
    using Microsoft.Azure.IoTSolutions.Shared.External.Models;
    using System.Threading.Tasks;

    /// <summary>
    /// Represents explorer service api functions
    /// </summary>
    public interface IOpcUaExplorerService {

        /// <summary>
        /// Returns status of the service
        /// </summary>
        /// <returns></returns>
        Task<StatusResponseApiModel> GetServiceStatusAsync();

        /// <summary>
        /// Register new endpoint
        /// </summary>
        /// <param name="content"></param>
        /// <returns></returns>
        Task<ServerRegistrationResponseApiModel> RegisterEndpointAsync(
            ServerRegistrationRequestApiModel content);

        /// <summary>
        /// Update endpoint registration
        /// </summary>
        /// <param name="content"></param>
        /// <returns></returns>
        Task UpdateEndpointAsync(ServerRegistrationApiModel content);

        /// <summary>
        /// Delete endpoint
        /// </summary>
        /// <param name="endpointId"></param>
        /// <returns></returns>
        Task DeleteEndpointAsync(string endpointId);

        /// <summary>
        /// Get endpoint
        /// </summary>
        /// <param name="endpointId"></param>
        /// <returns></returns>
        Task<ServerEndpointApiModel> GetEndpointAsync(string endpointId);

        /// <summary>
        /// List all endpoints
        /// </summary>
        /// <param name="continuation"></param>
        /// <returns></returns>
        Task<ServerRegistrationListApiModel> ListEndpointsAsync(
            string continuation);

        /// <summary>
        /// Browse node
        /// </summary>
        /// <param name="endpointId"></param>
        /// <param name="content"></param>
        /// <returns></returns>
        Task<BrowseResponseApiModel> NodeBrowseAsync(string endpointId,
            BrowseRequestApiModel content);

        /// <summary>
        /// Call method
        /// </summary>
        /// <param name="endpointId"></param>
        /// <param name="content"></param>
        /// <returns></returns>
        Task<MethodCallResponseApiModel> NodeMethodCallAsync(string endpointId,
            MethodCallRequestApiModel content);

        /// <summary>
        /// Get meta data for method call
        /// </summary>
        /// <param name="endpointId"></param>
        /// <param name="content"></param>
        /// <returns></returns>
        Task<MethodMetadataResponseApiModel> NodeMethodGetMetadataAsync(
            string endpointId, MethodMetadataRequestApiModel content);

        /// <summary>
        /// Publish or unpublish node
        /// </summary>
        /// <param name="endpointId"></param>
        /// <param name="content"></param>
        /// <returns></returns>
        Task<PublishResponseApiModel> NodePublishAsync(string endpointId,
            PublishRequestApiModel content);

        /// <summary>
        /// Read node value
        /// </summary>
        /// <param name="endpointId"></param>
        /// <param name="content"></param>
        /// <returns></returns>
        Task<ValueReadResponseApiModel> NodeValueReadAsync(string endpointId,
            ValueReadRequestApiModel content);

        /// <summary>
        /// Write node value
        /// </summary>
        /// <param name="endpointId"></param>
        /// <param name="content"></param>
        /// <returns></returns>
        Task<ValueWriteResponseApiModel> NodeValueWriteAsync(string endpointId,
            ValueWriteRequestApiModel content);

        /// <summary>
        /// Returns the client certificate
        /// </summary>
        /// <param name="endpointId"></param>
        /// <returns></returns>
        Task<string> GetClientCertificateAsync(string endpointId);

        /// <summary>
        /// Returns the server certificate
        /// </summary>
        /// <param name="endpointId"></param>
        /// <returns></returns>
        Task<string> GetServerCertificateAsync(string endpointId);
    }
}