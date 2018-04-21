// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IIoT.OpcTwin.Services.External {
    using Microsoft.Azure.IIoT.OpcTwin.Services.External.Models;
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
        Task<DeviceTwinModel> CreateOrUpdateAsync(
            DeviceTwinModel device);

        /// <summary>
        /// Returns twin
        /// </summary>
        /// <param name="deviceId"></param>
        /// <param name="moduleId"></param>
        /// <returns></returns>
        Task<DeviceTwinModel> GetAsync(string deviceId,
            string moduleId);

        /// <summary>
        /// Returns registration info
        /// </summary>
        /// <param name="deviceId"></param>
        /// <param name="moduleId"></param>
        /// <returns></returns>
        Task<DeviceModel> GetRegistrationAsync(string deviceId, 
            string moduleId);

        /// <summary>
        /// Call device method on twin
        /// </summary>
        /// <param name="deviceId"></param>
        /// <param name="moduleId"></param>
        /// <param name="parameters"></param>
        /// <returns></returns>
        Task<MethodResultModel> CallMethodAsync(string deviceId, 
            string moduleId, MethodParameterModel parameters);

        /// <summary>
        /// Update device properties through twin
        /// </summary>
        /// <param name="deviceId"></param>
        /// <param name="moduleId"></param>
        /// <param name="properties"></param>
        /// <returns></returns>
        Task UpdatePropertiesAsync(string deviceId, string moduleId,
            Dictionary<string, JToken> properties);

        /// <summary>
        /// Returns devices matching a query string
        /// </summary>
        /// <param name="query"></param>
        /// <param name="continuation"></param>
        /// <param name="pageSize"></param>
        /// <returns></returns>
        Task<DeviceTwinListModel> QueryAsync(string query,
            string continuation, int? pageSize);

        /// <summary>
        /// Query and return result string and continuation
        /// </summary>
        /// <param name="query"></param>
        /// <param name="continuation"></param>
        /// <param name="pageSize"></param>
        /// <returns></returns>
        Task<Tuple<string, string>> QueryRawAsync(string query,
            string continuation, int? pageSize);

        /// <summary>
        /// Delete twin
        /// </summary>
        /// <param name="deviceId"></param>
        /// <param name="moduleId"></param>
        /// <returns></returns>
        Task DeleteAsync(string deviceId, string moduleId);
    }
}