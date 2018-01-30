// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IoTSolutions.OpcUaExplorer.Services.External {
    using Microsoft.Azure.IoTSolutions.OpcUaExplorer.Services.External.Models;
    using System.Threading.Tasks;

    /// <summary>
    /// Twin services
    /// </summary>
    public interface IIoTHubTwinServices {

        /// <summary>
        /// Call device method on twin
        /// </summary>
        /// <param name="twinId"></param>
        /// <param name="parameters"></param>
        /// <returns></returns>
        Task<MethodResultModel> CallMethodAsync(string twinId,
            MethodParameterModel parameters);

        /// <summary>
        /// Returns devices matching a query string
        /// </summary>
        /// <param name="query"></param>
        /// <param name="continuation"></param>
        /// <returns></returns>
        Task<DeviceTwinListModel> QueryAsync(string query,
            string continuation);

        /// <summary>
        /// Create new twin
        /// </summary>
        /// <param name="device"></param>
        /// <returns></returns>
        Task<DeviceTwinModel> CreateOrUpdateAsync(
            DeviceTwinModel device);

        /// <summary>
        /// Returns twin
        /// </summary>
        /// <param name="twinId"></param>
        /// <returns></returns>
        Task<DeviceTwinModel> GetAsync(string twinId);

        /// <summary>
        /// Delete twin
        /// </summary>
        /// <param name="twinId"></param>
        /// <returns></returns>
        Task DeleteAsync(string twinId);
    }
}