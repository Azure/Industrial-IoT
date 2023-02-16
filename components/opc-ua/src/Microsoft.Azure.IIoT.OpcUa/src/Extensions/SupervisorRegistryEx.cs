// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IIoT.OpcUa.Registry {
    using Microsoft.Azure.IIoT.Api.Models;
    using System.Collections.Generic;
    using System.Threading;
    using System.Threading.Tasks;

    /// <summary>
    /// Supervisor registry extensions
    /// </summary>
    public static class SupervisorRegistryEx {

        /// <summary>
        /// Query all supervisors
        /// </summary>
        /// <param name="service"></param>
        /// <param name="query"></param>
        /// <param name="onlyServerState"></param>
        /// <param name="ct"></param>
        /// <returns></returns>
        public static async Task<List<SupervisorModel>> QueryAllSupervisorsAsync(
            this ISupervisorRegistry service, SupervisorQueryModel query,
            bool onlyServerState = false, CancellationToken ct = default) {
            var supervisors = new List<SupervisorModel>();
            var result = await service.QuerySupervisorsAsync(query, onlyServerState, null, ct);
            supervisors.AddRange(result.Items);
            while (result.ContinuationToken != null) {
                result = await service.ListSupervisorsAsync(result.ContinuationToken,
                    onlyServerState, null, ct);
                supervisors.AddRange(result.Items);
            }
            return supervisors;
        }
    }
}
