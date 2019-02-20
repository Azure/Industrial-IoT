// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IIoT.OpcUa.Registry {
    using Microsoft.Azure.IIoT.OpcUa.Registry.Models;
    using System.Threading.Tasks;

    /// <summary>
    /// Endpoint registry
    /// </summary>
    public interface IEndpointRegistry {

        /// <summary>
        /// Get all endpoints in paged form
        /// </summary>
        /// <param name="onlyServerState">Whether only
        /// desired endpoint state should be returned.
        /// </param>
        /// <param name="continuation"></param>
        /// <param name="pageSize"></param>
        /// <returns></returns>
        Task<EndpointInfoListModel> ListEndpointsAsync(string continuation,
            bool onlyServerState = false, int? pageSize = null);

        /// <summary>
        /// Find registration of the supplied endpoint.
        /// </summary>
        /// <param name="query"></param>
        /// <param name="onlyServerState">Whether only
        /// desired endpoint state should be returned.
        /// </param>
        /// <param name="pageSize"></param>
        /// <returns></returns>
        Task<EndpointInfoListModel> QueryEndpointsAsync(
            EndpointRegistrationQueryModel query,
            bool onlyServerState = false, int? pageSize = null);

        /// <summary>
        /// Get endpoint registration by identifer.
        /// </summary>
        /// <param name="id"></param>
        /// <param name="onlyServerState">Whether only
        /// desired endpoint state should be returned.
        /// </param>
        /// <returns></returns>
        Task<EndpointInfoModel> GetEndpointAsync(string id,
            bool onlyServerState = false);

        /// <summary>
        /// Set the endpoint state to activated
        /// </summary>
        /// <param name="id"></param>
        /// <returns></returns>
        Task ActivateEndpointAsync(string id);

        /// <summary>
        /// Update existing server endpoint registration.
        /// Id and url field in request must not be null and
        /// endpoint registration must exist.
        /// </summary>
        /// <param name="id"></param>
        /// <param name="request"></param>
        /// <returns></returns>
        Task UpdateEndpointAsync(string id,
            EndpointRegistrationUpdateModel request);

        /// <summary>
        /// Set the endpoint state to deactivated
        /// </summary>
        /// <param name="id"></param>
        /// <returns></returns>
        Task DeactivateEndpointAsync(string id);
    }
}
