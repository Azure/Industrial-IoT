// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IIoT.Hub {
    using Microsoft.Azure.IIoT.Exceptions;
    using Microsoft.Azure.IIoT.Hub.Models;
    using Microsoft.Azure.IIoT.Serializers;
    using System.Collections.Generic;
    using System.Threading;
    using System.Threading.Tasks;

    /// <summary>
    /// Twin services
    /// </summary>
    public interface IIoTHubTwinServices {

        /// <summary>
        /// Get the host name of the iot hub
        /// </summary>
        string HostName { get; }

        /// <summary>
        /// Create new twin or update existing one.  If there is
        /// a conflict and force is set, ensures the twin exists
        /// as specified in the end.
        /// </summary>
        /// <exception cref="ConflictingResourceException"></exception>
        /// <param name="device">device twin to create</param>
        /// <param name="force">skip conflicting resource and update
        /// to the passed in twin state</param>
        /// <param name="ct"></param>
        /// <returns>new device</returns>
        Task<DeviceTwinModel> CreateAsync(
            DeviceTwinModel device, bool force = false,
            CancellationToken ct = default);

        /// <summary>
        /// Update existing one.
        /// </summary>
        /// <exception cref="ResourceNotFoundException"></exception>
        /// <param name="device"></param>
        /// <param name="force">Do not use etag</param>
        /// <param name="ct"></param>
        /// <returns>new device</returns>
        Task<DeviceTwinModel> PatchAsync(
            DeviceTwinModel device, bool force = false,
            CancellationToken ct = default);

        /// <summary>
        /// Returns twin
        /// </summary>
        /// <param name="deviceId"></param>
        /// <param name="moduleId"></param>
        /// <param name="ct"></param>
        /// <returns></returns>
        Task<DeviceTwinModel> GetAsync(string deviceId,
            string moduleId = null, CancellationToken ct = default);

        /// <summary>
        /// Returns registration info
        /// </summary>
        /// <param name="deviceId"></param>
        /// <param name="moduleId"></param>
        /// <param name="ct"></param>
        /// <returns></returns>
        Task<DeviceModel> GetRegistrationAsync(string deviceId,
            string moduleId = null, CancellationToken ct = default);

        /// <summary>
        /// Query and return result and continuation
        /// </summary>
        /// <param name="query"></param>
        /// <param name="continuation"></param>
        /// <param name="pageSize"></param>
        /// <param name="ct"></param>
        /// <returns></returns>
        Task<QueryResultModel> QueryAsync(string query,
            string continuation = null, int? pageSize = null,
            CancellationToken ct = default);

        /// <summary>
        /// Update device properties through twin
        /// </summary>
        /// <param name="deviceId"></param>
        /// <param name="moduleId"></param>
        /// <param name="properties"></param>
        /// <param name="etag"></param>
        /// <param name="ct"></param>
        /// <returns></returns>
        Task UpdatePropertiesAsync(string deviceId, string moduleId,
            Dictionary<string, VariantValue> properties, string etag = null,
            CancellationToken ct = default);

        /// <summary>
        /// Delete twin
        /// </summary>
        /// <param name="deviceId"></param>
        /// <param name="moduleId"></param>
        /// <param name="etag"></param>
        /// <param name="ct"></param>
        /// <returns></returns>
        Task DeleteAsync(string deviceId, string moduleId = null,
            string etag = null, CancellationToken ct = default);

        /// <summary>
        /// Call device method on twin
        /// </summary>
        /// <param name="deviceId"></param>
        /// <param name="moduleId"></param>
        /// <param name="parameters"></param>
        /// <param name="ct"></param>
        /// <returns></returns>
        Task<MethodResultModel> CallMethodAsync(string deviceId,
            string moduleId, MethodParameterModel parameters,
            CancellationToken ct = default);
    }
}
