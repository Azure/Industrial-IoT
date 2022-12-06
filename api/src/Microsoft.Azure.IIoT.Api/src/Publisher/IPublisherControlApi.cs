// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IIoT.OpcUa.Api.Publisher {
    using Microsoft.Azure.IIoT.OpcUa.Api.Publisher.Models;
    using System.Collections.Generic;
    using System.Threading.Tasks;

    /// <summary>
    /// Publisher control
    /// </summary>
    public interface IPublisherControlApi {

        /// <summary>
        /// Add or update publishing endpoints
        /// </summary>
        /// <param name="deviceId"></param>
        /// <param name="moduleId"></param>
        /// <param name="request"></param>
        /// <returns></returns>
        Task<PublishedNodesResponseApiModel> AddOrUpdateEndpointsAsync(
            string deviceId, string moduleId, List<PublishNodesEndpointApiModel> request);

        /// <summary>
        /// Get configured endpoints
        /// </summary>
        /// <param name="deviceId"></param>
        /// <param name="moduleId"></param>
        /// <returns></returns>
        Task<GetConfiguredEndpointsResponseApiModel> GetConfiguredEndpointsAsync(
            string deviceId, string moduleId);

        /// <summary>
        /// Get nodes of an endpoint
        /// </summary>
        /// <param name="deviceId"></param>
        /// <param name="moduleId"></param>
        /// <param name="request"></param>
        /// <returns></returns>
        Task<GetConfiguredNodesOnEndpointResponseApiModel> GetConfiguredNodesOnEndpointAsync(
            string deviceId, string moduleId, PublishNodesEndpointApiModel request);

        /// <summary>
        /// Get diagnostic info
        /// </summary>
        /// <param name="deviceId"></param>
        /// <param name="moduleId"></param>
        /// <returns></returns>
        Task<List<DiagnosticInfoApiModel>> GetDiagnosticInfoAsync(
            string deviceId, string moduleId);

        /// <summary>
        /// Publish nodes
        /// </summary>
        /// <param name="deviceId"></param>
        /// <param name="moduleId"></param>
        /// <param name="request"></param>
        /// <returns></returns>
        Task<PublishedNodesResponseApiModel> PublishNodesAsync(
            string deviceId, string moduleId, PublishNodesEndpointApiModel request);

        /// <summary>
        /// Remove all nodes on endpoint
        /// </summary>
        /// <param name="deviceId"></param>
        /// <param name="moduleId"></param>
        /// <param name="request"></param>
        /// <returns></returns>
        Task<PublishedNodesResponseApiModel> UnpublishAllNodesAsync(
            string deviceId, string moduleId, PublishNodesEndpointApiModel request);

        /// <summary>
        /// Stop publishing specified nodes on endpoint
        /// </summary>
        /// <param name="deviceId"></param>
        /// <param name="moduleId"></param>
        /// <param name="request"></param>
        /// <returns></returns>
        Task<PublishedNodesResponseApiModel> UnpublishNodesAsync(
            string deviceId, string moduleId, PublishNodesEndpointApiModel request);
    }
}