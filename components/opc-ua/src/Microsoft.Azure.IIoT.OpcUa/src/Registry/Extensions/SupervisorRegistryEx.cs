// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IIoT.OpcUa.Registry {
    using Microsoft.Azure.IIoT.Exceptions;
    using Microsoft.Azure.IIoT.OpcUa.Registry.Models;
    using System.Collections.Generic;
    using System.Threading;
    using System.Threading.Tasks;

    /// <summary>
    /// Supervisor registry extensions
    /// </summary>
    public static class SupervisorRegistryEx {

        /// <summary>
        /// Find supervisor.
        /// </summary>
        /// <param name="service"></param>
        /// <param name="supervisorId"></param>
        /// <param name="ct"></param>
        /// <returns></returns>
        public static async Task<SupervisorModel> FindSupervisorAsync(
            this ISupervisorRegistry service, string supervisorId,
            CancellationToken ct = default) {
            try {
                return await service.GetSupervisorAsync(supervisorId, false, ct);
            }
            catch (ResourceNotFoundException) {
                return null;
            }
        }

        /// <summary>
        /// List all supervisors
        /// </summary>
        /// <param name="service"></param>
        /// <param name="onlyServerState"></param>
        /// <param name="ct"></param>
        /// <returns></returns>
        public static async Task<List<SupervisorModel>> ListAllSupervisorsAsync(
            this ISupervisorRegistry service, bool onlyServerState = false,
            CancellationToken ct = default) {
            var supervisors = new List<SupervisorModel>();
            var result = await service.ListSupervisorsAsync(null, onlyServerState, null, ct);
            supervisors.AddRange(result.Items);
            while (result.ContinuationToken != null) {
                result = await service.ListSupervisorsAsync(result.ContinuationToken,
                    onlyServerState, null, ct);
                supervisors.AddRange(result.Items);
            }
            return supervisors;
        }

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
