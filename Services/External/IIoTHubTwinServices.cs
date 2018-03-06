// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IoTSolutions.OpcTwin.Services.External {
    using Microsoft.Azure.IoTSolutions.OpcTwin.Services.External.Models;
    using Newtonsoft.Json.Linq;
    using System;
    using System.Collections.Generic;
    using System.Threading.Tasks;

    /// <summary>
    /// Twin services
    /// </summary>
    public interface IIoTHubTwinServices {

        /// <summary>
        /// Create new twin
        /// </summary>
        /// <param name="device"></param>
        /// <returns></returns>
        Task<TwinModel> CreateOrUpdateAsync(TwinModel device);

        /// <summary>
        /// Returns twin
        /// </summary>
        /// <param name="twinId"></param>
        /// <returns></returns>
        Task<TwinModel> GetAsync(string twinId);

        /// <summary>
        /// Returns registration info
        /// </summary>
        /// <param name="twinId"></param>
        /// <returns></returns>
        Task<DeviceModel> GetRegistrationAsync(string twinId);

        /// <summary>
        /// Call device method on twin
        /// </summary>
        /// <param name="twinId"></param>
        /// <param name="parameters"></param>
        /// <returns></returns>
        Task<MethodResultModel> CallMethodAsync(string twinId,
            MethodParameterModel parameters);

        /// <summary>
        /// Update device properties through twin
        /// </summary>
        /// <param name="twinId"></param>
        /// <param name="properties"></param>
        /// <returns></returns>
        Task UpdatePropertiesAsync(string twinId,
            Dictionary<string, JToken> properties);

        /// <summary>
        /// Returns devices matching a query string
        /// </summary>
        /// <param name="query"></param>
        /// <param name="continuation"></param>
        /// <returns></returns>
        Task<TwinListModel> QueryAsync(string query,
            string continuation);

        /// <summary>
        /// Query and return result string and continuation
        /// </summary>
        /// <param name="query"></param>
        /// <param name="continuation"></param>
        /// <returns></returns>
        Task<Tuple<string, string>> QueryRawAsync(
            string query, string continuation);

        /// <summary>
        /// Delete twin
        /// </summary>
        /// <param name="twinId"></param>
        /// <returns></returns>
        Task DeleteAsync(string twinId);
    }
}