// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IoTSolutions.OpcUaExplorer.Services {
    using Microsoft.Azure.IoTSolutions.OpcUaExplorer.Services.Models;
    using System.Threading.Tasks;

    /// <summary>
    /// Endpoint services
    /// </summary>
    public interface IOpcUaEndpointServices {

        /// <summary>
        /// Get endpoints in paged form starting from
        /// offset and returning number
        /// </summary>
        /// <param name="continuation"></param>
        /// <returns></returns>
        Task<ServerRegistrationListModel> ListAsync(
            string continuation);

        /// <summary>
        /// Register new endpoint. If endpoint already
        /// exists, that endpoint is returned.
        /// </summary>
        /// <param name="request"></param>
        /// <returns></returns>
        Task<ServerRegistrationResultModel> RegisterAsync(
            ServerRegistrationRequestModel request);

        /// <summary>
        /// Get endpoint by identifer.  If endoint does
        /// not exist, null is returned.
        /// </summary>
        /// <param name="id"></param>
        /// <returns></returns>
        Task<ServerEndpointModel> GetAsync(string id);

        /// <summary>
        /// Update existing server endpoint. Note that
        /// Id field in request must not be null and
        /// endpoint registration must exist.
        /// </summary>
        /// <param name="registration"></param>
        /// <returns></returns>
        Task PatchAsync(ServerRegistrationModel registration);

        /// <summary>
        /// Delete endpoint by id
        /// </summary>
        /// <param name="id"></param>
        /// <returns></returns>
        Task DeleteAsync(string id);
    }
}