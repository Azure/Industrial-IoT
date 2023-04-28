// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Azure.IIoT.OpcUa.Publisher.Service
{
    using Azure.IIoT.OpcUa.Publisher.Models;
    using System.Threading;
    using System.Threading.Tasks;

    /// <summary>
    /// Supervisor registry
    /// </summary>
    public interface ISupervisorRegistry
    {
        /// <summary>
        /// Get all supervisors in paged form
        /// </summary>
        /// <param name="continuation"></param>
        /// <param name="onlyServerState"></param>
        /// <param name="pageSize"></param>
        /// <param name="ct"></param>
        /// <returns></returns>
        Task<SupervisorListModel> ListSupervisorsAsync(
            string? continuation, bool onlyServerState = false,
            int? pageSize = null, CancellationToken ct = default);

        /// <summary>
        /// Find supervisors using specific criterias.
        /// </summary>
        /// <param name="query"></param>
        /// <param name="onlyServerState"></param>
        /// <param name="pageSize"></param>
        /// <param name="ct"></param>
        /// <returns></returns>
        Task<SupervisorListModel> QuerySupervisorsAsync(
            SupervisorQueryModel query, bool onlyServerState = false,
            int? pageSize = null,
            CancellationToken ct = default);

        /// <summary>
        /// Get supervisor registration by identifer.
        /// </summary>
        /// <param name="supervisorId"></param>
        /// <param name="onlyServerState"></param>
        /// <param name="ct"></param>
        /// <returns></returns>
        Task<SupervisorModel> GetSupervisorAsync(
            string supervisorId, bool onlyServerState = false,
            CancellationToken ct = default);

        /// <summary>
        /// Update supervisor, e.g. set discovery mode
        /// </summary>
        /// <param name="supervisorId"></param>
        /// <param name="request"></param>
        /// <param name="ct"></param>
        /// <returns></returns>
        Task UpdateSupervisorAsync(string supervisorId,
            SupervisorUpdateModel request,
            CancellationToken ct = default);
    }
}
