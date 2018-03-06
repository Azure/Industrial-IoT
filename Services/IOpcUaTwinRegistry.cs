// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IoTSolutions.OpcTwin.Services {
    using Microsoft.Azure.IoTSolutions.OpcTwin.Services.Models;
    using System.Threading.Tasks;

    /// <summary>
    /// Twin registry
    /// </summary>
    public interface IOpcUaTwinRegistry {

        /// <summary>
        /// Get all twins in paged form
        /// </summary>
        /// <param name="onlyServerState">Whether only
        /// desired endpoint state should be returned.
        /// </param>
        /// <param name="continuation"></param>
        /// <returns></returns>
        Task<TwinRegistrationListModel> ListTwinsAsync(
            string continuation, bool onlyServerState);

        /// <summary>
        /// Register new twin. If exact twin already
        /// exists, that twin is returned.
        /// </summary>
        /// <param name="request"></param>
        /// <returns></returns>
        Task<TwinRegistrationResultModel> RegisterTwinAsync(
            TwinRegistrationRequestModel request);

        /// <summary>
        /// Get twin registration by identifer.
        /// </summary>
        /// <param name="id"></param>
        /// <param name="onlyServerState">Whether only
        /// desired twin state should be returned.
        /// </param>
        /// <returns></returns>
        Task<TwinRegistrationModel> GetTwinAsync(string id,
            bool onlyServerState);

        /// <summary>
        /// Find registration of the supplied endpoint.
        /// </summary>
        /// <param name="endpoint"></param>
        /// <returns></returns>
        Task<TwinRegistrationModel> FindTwinAsync(
            EndpointModel endpoint);

        /// <summary>
        /// Update existing server twin registration. Note that
        /// Id and url field in request must not be null and
        /// endpoint registration must exist.
        /// </summary>
        /// <param name="request"></param>
        /// <returns></returns>
        Task UpdateTwinAsync(TwinRegistrationUpdateModel request);

        /// <summary>
        /// Delete twin by id
        /// </summary>
        /// <param name="id"></param>
        /// <returns></returns>
        Task DeleteTwinAsync(string id);
    }
}