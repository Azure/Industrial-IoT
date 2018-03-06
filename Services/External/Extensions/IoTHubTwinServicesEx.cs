// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IoTSolutions.OpcTwin.Services.External {
    using Microsoft.Azure.IoTSolutions.OpcTwin.Services.External.Models;
    using Newtonsoft.Json.Linq;
    using System.Collections.Generic;
    using System.Threading.Tasks;

    public static class IoTHubTwinServicesEx {

        /// <summary>
        /// Query hub for device twins
        /// </summary>
        /// <param name="service"></param>
        /// <param name="query"></param>
        /// <returns></returns>
        public static async Task<List<TwinModel>> QueryAsync(
            this IIoTHubTwinServices service, string query) {
            var result = new List<TwinModel>();
            string continuation = null;
            do {
                var response = await service.QueryAsync(query, continuation);
                result.AddRange(response.Items);
                continuation = response.ContinuationToken;
            }
            while (continuation != null);
            return result;
        }

        /// <summary>
        /// Check whether device is connected
        /// </summary>
        /// <param name="service"></param>
        /// <param name="twinId"></param>
        /// <returns></returns>
        public static async Task<bool> IsConnectedAsync(
            this IIoTHubTwinServices service, string twinId) {
            var device = await service.GetRegistrationAsync(twinId);
            return device.Enabled && device.Connected;
        }

        /// <summary>
        /// Check whether device is enabled
        /// </summary>
        /// <param name="service"></param>
        /// <param name="twinId"></param>
        /// <returns></returns>
        public static async Task<bool> IsEnabledAsync(
            this IIoTHubTwinServices service, string twinId) {
            var device = await service.GetRegistrationAsync(twinId);
            return device.Enabled;
        }


        /// <summary>
        /// Update device property through twin
        /// </summary>
        /// <param name="twinId"></param>
        /// <param name="property"></param>
        /// <param name="value"></param>
        /// <returns></returns>
        public static Task UpdatePropertyAsync(this IIoTHubTwinServices service,
            string twinId, string property, JToken value) {
            return service.UpdatePropertiesAsync(twinId, new Dictionary<string, JToken> {
                [property] = value
            });
        }
    }
}
