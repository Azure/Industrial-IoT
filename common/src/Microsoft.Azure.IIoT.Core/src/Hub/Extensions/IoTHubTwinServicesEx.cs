// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IIoT.Hub {
    using Microsoft.Azure.IIoT.Exceptions;
    using Microsoft.Azure.IIoT.Hub.Models;
    using Microsoft.Azure.IIoT.Serializers;
    using Microsoft.Azure.IIoT.Utils;
    using System.Collections.Generic;
    using System.Linq;
    using System.Threading;
    using System.Threading.Tasks;

    /// <summary>
    /// Twin services extensions
    /// </summary>
    public static class IoTHubTwinServicesEx {

        /// <summary>
        /// Find twin or return null
        /// </summary>
        /// <param name="service"></param>
        /// <param name="deviceId"></param>
        /// <param name="moduleId"></param>
        /// <param name="ct"></param>
        /// <returns></returns>
        public static async Task<DeviceTwinModel> FindAsync(this IIoTHubTwinServices service,
            string deviceId, string moduleId = null, CancellationToken ct = default) {
            try {
                return await service.GetAsync(deviceId, moduleId, ct);
            }
            catch (ResourceNotFoundException) {
                return null;
            }
        }

        /// <summary>
        /// Returns device connection string
        /// </summary>
        /// <param name="service"></param>
        /// <param name="deviceId"></param>
        /// <param name="primary"></param>
        /// <param name="ct"></param>
        /// <returns></returns>
        public static async Task<ConnectionString> GetConnectionStringAsync(
            this IIoTHubTwinServices service, string deviceId, bool primary = true,
            CancellationToken ct = default) {
            var model = await service.GetRegistrationAsync(deviceId, null, ct);
            if (model == null) {
                throw new ResourceNotFoundException("Could not find " + deviceId);
            }
            return ConnectionString.CreateDeviceConnectionString(service.HostName,
                deviceId, primary ?
                    model.Authentication.PrimaryKey : model.Authentication.SecondaryKey);
        }

        /// <summary>
        /// Returns module connection string
        /// </summary>
        /// <param name="service"></param>
        /// <param name="deviceId"></param>
        /// <param name="moduleId"></param>
        /// <param name="primary"></param>
        /// <param name="ct"></param>
        /// <returns></returns>
        public static async Task<ConnectionString> GetConnectionStringAsync(
            this IIoTHubTwinServices service, string deviceId, string moduleId,
            bool primary = true, CancellationToken ct = default) {
            var model = await service.GetRegistrationAsync(deviceId, moduleId, ct);
            if (model == null) {
                throw new ResourceNotFoundException("Could not find " + moduleId);
            }
            return ConnectionString.CreateModuleConnectionString(service.HostName,
                deviceId, moduleId, primary ?
                    model.Authentication.PrimaryKey : model.Authentication.SecondaryKey);
        }

        /// <summary>
        /// Returns device or module primary key
        /// </summary>
        /// <param name="service"></param>
        /// <param name="deviceId"></param>
        /// <param name="moduleId"></param>
        /// <param name="ct"></param>
        /// <returns></returns>
        public static async Task<string> GetPrimaryKeyAsync(
            this IIoTHubTwinServices service, string deviceId, string moduleId = null,
            CancellationToken ct = default) {
            var model = await service.GetRegistrationAsync(deviceId, moduleId, ct);
            if (model == null) {
                throw new ResourceNotFoundException("Could not find " + deviceId);
            }
            return model.Authentication.PrimaryKey;
        }

        /// <summary>
        /// Returns device or module secondary key
        /// </summary>
        /// <param name="service"></param>
        /// <param name="deviceId"></param>
        /// <param name="moduleId"></param>
        /// <param name="ct"></param>
        /// <returns></returns>
        public static async Task<string> GetSecondaryKeyAsync(
            this IIoTHubTwinServices service, string deviceId, string moduleId = null,
            CancellationToken ct = default) {
            var model = await service.GetRegistrationAsync(deviceId, moduleId, ct);
            if (model == null) {
                throw new ResourceNotFoundException("Could not find " + deviceId);
            }
            return model.Authentication.SecondaryKey;
        }

        /// <summary>
        /// Query twins
        /// </summary>
        /// <param name="service"></param>
        /// <param name="query"></param>
        /// <param name="continuation"></param>
        /// <param name="pageSize"></param>
        /// <param name="ct"></param>
        /// <returns></returns>
        public static async Task<DeviceTwinListModel> QueryDeviceTwinsAsync(
            this IIoTHubTwinServices service, string query, string continuation,
            int? pageSize = null, CancellationToken ct = default) {
            var response = await service.QueryAsync(query, continuation, pageSize, ct);
            return new DeviceTwinListModel {
                ContinuationToken = response.ContinuationToken,
                Items = response.Result
                    .Select(j => j.ConvertTo<DeviceTwinModel>())
                    .ToList()
            };
        }

        /// <summary>
        /// Query hub for device twins
        /// </summary>
        /// <param name="service"></param>
        /// <param name="query"></param>
        /// <param name="ct"></param>
        /// <returns></returns>
        public static async Task<List<DeviceTwinModel>> QueryAllDeviceTwinsAsync(
            this IIoTHubTwinServices service, string query, CancellationToken ct = default) {
            var result = new List<DeviceTwinModel>();
            string continuation = null;
            do {
                var response = await service.QueryDeviceTwinsAsync(query, continuation, null, ct);
                result.AddRange(response.Items);
                continuation = response.ContinuationToken;
            }
            while (continuation != null);
            return result;
        }

        /// <summary>
        /// Query all results
        /// </summary>
        /// <param name="service"></param>
        /// <param name="query"></param>
        /// <param name="ct"></param>
        /// <returns></returns>
        public static async Task<IEnumerable<VariantValue>> QueryAsync(
            this IIoTHubTwinServices service, string query, CancellationToken ct = default) {
            var result = new List<VariantValue>();
            string continuation = null;
            do {
                var response = await service.QueryAsync(query, continuation, null, ct);
                result.AddRange(response.Result);
                continuation = response.ContinuationToken;
            }
            while (continuation != null);
            return result;
        }

        /// <summary>
        /// Update device property through twin
        /// </summary>
        /// <param name="service"></param>
        /// <param name="deviceId"></param>
        /// <param name="moduleId"></param>
        /// <param name="property"></param>
        /// <param name="value"></param>
        /// <param name="ct"></param>
        /// <returns></returns>
        public static Task UpdatePropertyAsync(this IIoTHubTwinServices service,
            string deviceId, string moduleId, string property, VariantValue value,
            CancellationToken ct = default) {
            return service.UpdatePropertiesAsync(deviceId, moduleId,
                new Dictionary<string, VariantValue> {
                    [property] = value ?? VariantValue.Null
                }, null, ct);
        }

        /// <summary>
        /// Update device property through twin
        /// </summary>
        /// <param name="service"></param>
        /// <param name="deviceId"></param>
        /// <param name="property"></param>
        /// <param name="value"></param>
        /// <param name="ct"></param>
        /// <returns></returns>
        public static Task UpdatePropertyAsync(this IIoTHubTwinServices service,
            string deviceId, string property, VariantValue value,
            CancellationToken ct = default) {
            return service.UpdatePropertyAsync(deviceId, null, property, value, ct);
        }

        /// <summary>
        /// Update device properties through twin
        /// </summary>
        /// <param name="service"></param>
        /// <param name="deviceId"></param>
        /// <param name="moduleId"></param>
        /// <param name="properties"></param>
        /// <param name="ct"></param>
        /// <returns></returns>
        public static Task UpdatePropertiesAsync(this IIoTHubTwinServices service,
            string deviceId, string moduleId, Dictionary<string, VariantValue> properties,
            CancellationToken ct = default) {
            return service.UpdatePropertiesAsync(deviceId, moduleId, properties, null, ct);
        }

        /// <summary>
        /// Update device properties through twin
        /// </summary>
        /// <param name="service"></param>
        /// <param name="deviceId"></param>
        /// <param name="properties"></param>
        /// <param name="ct"></param>
        /// <returns></returns>
        public static Task UpdatePropertiesAsync(this IIoTHubTwinServices service,
            string deviceId, Dictionary<string, VariantValue> properties,
            CancellationToken ct = default) {
            return service.UpdatePropertiesAsync(deviceId, null, properties, ct);
        }

        /// <summary>
        /// Call device method on twin
        /// </summary>
        /// <param name="service"></param>
        /// <param name="deviceId"></param>
        /// <param name="parameters"></param>
        /// <param name="ct"></param>
        /// <returns></returns>
        public static Task<MethodResultModel> CallMethodAsync(this IIoTHubTwinServices service,
            string deviceId, MethodParameterModel parameters, CancellationToken ct = default) {
            return service.CallMethodAsync(deviceId, null, parameters, ct);
        }
    }
}
