// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IIoT.OpcUa.Registry {
    using Microsoft.Azure.IIoT.OpcUa.Registry.Models;
    using System.Threading.Tasks;

    /// <summary>
    /// Supervisor status and diagnostic services
    /// </summary>
    public interface ISupervisorDiagnostics {

        /// <summary>
        /// Get supervisor runtime status
        /// </summary>
        /// <param name="supervisorId"></param>
        /// <returns>Supervisor diagnostics</returns>
        Task<SupervisorStatusModel> GetSupervisorStatusAsync(
            string supervisorId);

        /// <summary>
        /// Reset and restart supervisor
        /// </summary>
        /// <param name="supervisorId"></param>
        Task ResetSupervisorAsync(string supervisorId);
    }
}
