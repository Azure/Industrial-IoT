// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IoTSolutions.OpcUaExplorer.Services.External {
    using Microsoft.Azure.IoTSolutions.OpcUaExplorer.Services.External.Models;
    using System.Collections.Generic;
    using System.Threading.Tasks;

    public static class IoTHubTwinServicesEx {

        /// <summary>
        /// Query hub for device twins
        /// </summary>
        /// <param name="query"></param>
        /// <returns></returns>
        public static async Task<List<DeviceTwinModel>> QueryAsync(
            this IIoTHubTwinServices service, string query) {
            var result = new List<DeviceTwinModel>();
            string continuation = null;
            do {
                var response = await service.QueryAsync(query, continuation);
                result.AddRange(response.Items);
                continuation = response.ContinuationToken;
            }
            while (continuation != null);
            return result;
        }
    }
}
