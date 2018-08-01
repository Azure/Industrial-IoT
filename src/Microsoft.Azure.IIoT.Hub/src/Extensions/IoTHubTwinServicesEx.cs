// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IIoT.Hub {
    using Microsoft.Azure.IIoT.Exceptions;
    using Microsoft.Azure.IIoT.Hub.Models;
    using Microsoft.Azure.IIoT.Utils;
    using Newtonsoft.Json.Linq;
    using System.Collections.Generic;
    using System.Linq;
    using System.Threading.Tasks;

    public static class IoTHubTwinServicesEx {

        /// <summary>
        /// Returns device connection string
        /// </summary>
        /// <param name="deviceId"></param>
        /// <param name="primary"></param>
        /// <returns></returns>
        public static async Task<ConnectionString> GetConnectionStringAsync(
            this IIoTHubTwinServices service, string deviceId, bool primary = true) {
            var model = await service.GetRegistrationAsync(deviceId);
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
        /// <param name="deviceId"></param>
        /// <param name="moduleId"></param>
        /// <param name="primary"></param>
        /// <returns></returns>
        public static async Task<ConnectionString> GetConnectionStringAsync(
            this IIoTHubTwinServices service, string deviceId, string moduleId,
            bool primary = true) {
            var model = await service.GetRegistrationAsync(deviceId, moduleId);
            if (model == null) {
                throw new ResourceNotFoundException("Could not find " + moduleId);
            }
            return ConnectionString.CreateModuleConnectionString(service.HostName,
                deviceId, moduleId, primary ?
                    model.Authentication.PrimaryKey : model.Authentication.SecondaryKey);
        }

        /// <summary>
        /// Returns devices matching a query string
        /// </summary>
        /// <param name="query"></param>
        /// <param name="continuation"></param>
        /// <returns></returns>
        public static Task<DeviceTwinListModel> QueryDeviceTwinsAsync(
            this IIoTHubTwinServices service, string query, string continuation) =>
            service.QueryDeviceTwinsAsync(query, continuation, null);

        /// <summary>
        /// Query twins
        /// </summary>
        /// <param name="query"></param>
        /// <param name="continuation"></param>
        /// <returns></returns>
        public static async Task<DeviceTwinListModel> QueryDeviceTwinsAsync(
            this IIoTHubTwinServices service, string query, string continuation,
            int? pageSize) {
            var response = await service.QueryAsync(query, continuation, pageSize);
            return new DeviceTwinListModel {
                ContinuationToken = response.ContinuationToken,
                Items = response.Result
                    .Select(DeviceTwinModelEx.ToDeviceTwinModel)
                    .ToList()
            };
        }

        /// <summary>
        /// Query hub for device twins
        /// </summary>
        /// <param name="service"></param>
        /// <param name="query"></param>
        /// <returns></returns>
        public static async Task<List<DeviceTwinModel>> QueryDeviceTwinsAsync(
            this IIoTHubTwinServices service, string query) {
            var result = new List<DeviceTwinModel>();
            string continuation = null;
            do {
                var response = await service.QueryDeviceTwinsAsync(query, continuation);
                result.AddRange(response.Items);
                continuation = response.ContinuationToken;
            }
            while (continuation != null);
            return result;
        }

        /// <summary>
        /// Returns results matching a query string
        /// </summary>
        /// <param name="query"></param>
        /// <param name="continuation"></param>
        /// <returns></returns>
        public static Task<QueryResultModel> QueryAsync(
            this IIoTHubTwinServices service, string query, string continuation) =>
            service.QueryAsync(query, continuation, null);

        /// <summary>
        /// Query all results
        /// </summary>
        /// <param name="service"></param>
        /// <param name="query"></param>
        /// <returns></returns>
        public static async Task<IEnumerable<JToken>> QueryAsync(
            this IIoTHubTwinServices service, string query) {
            var result = new List<JToken>();
            string continuation = null;
            do {
                var response = await service.QueryAsync(query, continuation);
                result.AddRange(response.Result);
                continuation = response.ContinuationToken;
            }
            while (continuation != null);
            return result;
        }

        /// <summary>
        /// Check whether device is connected
        /// </summary>
        /// <param name="service"></param>
        /// <param name="deviceId"></param>
        /// <param name="moduleId"></param>
        /// <returns></returns>
        public static async Task<bool> IsConnectedAsync(
            this IIoTHubTwinServices service, string deviceId, string moduleId) {
            var device = await service.GetRegistrationAsync(deviceId, moduleId);
            return device.Enabled && device.Connected;
        }

        /// <summary>
        /// Check whether device is connected
        /// </summary>
        /// <param name="service"></param>
        /// <param name="deviceId"></param>
        /// <returns></returns>
        public static Task<bool> IsConnectedAsync(this IIoTHubTwinServices service,
            string deviceId) => service.IsConnectedAsync(deviceId, null);

        /// <summary>
        /// Check whether device is enabled
        /// </summary>
        /// <param name="service"></param>
        /// <param name="deviceId"></param>
        /// <returns></returns>
        public static async Task<bool> IsEnabledAsync(
            this IIoTHubTwinServices service, string deviceId) {
            var device = await service.GetRegistrationAsync(deviceId);
            return device.Enabled;
        }

        /// <summary>
        /// Create or update
        /// </summary>
        /// <param name="service"></param>
        /// <param name="device"></param>
        /// <returns></returns>
        public static Task<DeviceTwinModel> CreateOrUpdateAsync(
            this IIoTHubTwinServices service, DeviceTwinModel device) =>
            service.CreateOrUpdateAsync(device, false);

        /// <summary>
        /// Update device property through twin
        /// </summary>
        /// <param name="deviceId"></param>
        /// <param name="moduleId"></param>
        /// <param name="property"></param>
        /// <param name="value"></param>
        /// <returns></returns>
        public static Task UpdatePropertyAsync(this IIoTHubTwinServices service,
            string deviceId, string moduleId, string property, JToken value) {
            return service.UpdatePropertiesAsync(deviceId, moduleId,
                new Dictionary<string, JToken> {
                    [property] = value
                });
        }

        /// <summary>
        /// Update device property through twin
        /// </summary>
        /// <param name="deviceId"></param>
        /// <param name="property"></param>
        /// <param name="value"></param>
        /// <returns></returns>
        public static Task UpdatePropertyAsync(this IIoTHubTwinServices service,
            string deviceId, string property, JToken value) =>
            service.UpdatePropertyAsync(deviceId, null, property, value);

        /// <summary>
        /// Update device properties through twin
        /// </summary>
        /// <param name="deviceId"></param>
        /// <param name="moduleId"></param>
        /// <param name="properties"></param>
        /// <returns></returns>
        public static Task UpdatePropertiesAsync(this IIoTHubTwinServices service,
            string deviceId, string moduleId, Dictionary<string, JToken> properties) =>
            service.UpdatePropertiesAsync(deviceId, moduleId, properties, null);

        /// <summary>
        /// Update device properties through twin
        /// </summary>
        /// <param name="deviceId"></param>
        /// <param name="properties"></param>
        /// <returns></returns>
        public static Task UpdatePropertiesAsync(this IIoTHubTwinServices service,
            string deviceId, Dictionary<string, JToken> properties) =>
            service.UpdatePropertiesAsync(deviceId, null, properties);

        /// <summary>
        /// Returns twin
        /// </summary>
        /// <param name="deviceId"></param>
        /// <returns></returns>
        public static Task<DeviceTwinModel> GetAsync(this IIoTHubTwinServices service,
            string deviceId) => service.GetAsync(deviceId, null);

        /// <summary>
        /// Returns registration info
        /// </summary>
        /// <param name="deviceId"></param>
        /// <returns></returns>
        public static Task<DeviceModel> GetRegistrationAsync(this IIoTHubTwinServices service,
            string deviceId) => service.GetRegistrationAsync(deviceId, null);

        /// <summary>
        /// Call device method on twin
        /// </summary>
        /// <param name="deviceId"></param>
        /// <param name="parameters"></param>
        /// <returns></returns>
        public static Task<MethodResultModel> CallMethodAsync(this IIoTHubTwinServices service,
            string deviceId, MethodParameterModel parameters) =>
            service.CallMethodAsync(deviceId, null, parameters);

        /// <summary>
        /// Delete twin
        /// </summary>
        /// <param name="deviceId"></param>
        /// <returns></returns>
        public static Task DeleteAsync(this IIoTHubTwinServices service, string deviceId) =>
            service.DeleteAsync(deviceId, null, null);

        /// <summary>
        /// Delete twin
        /// </summary>
        /// <param name="deviceId"></param>
        /// <returns></returns>
        public static Task DeleteAsync(this IIoTHubTwinServices service, string deviceId,
            string moduleId) => service.DeleteAsync(deviceId, moduleId, null);
    }
}
