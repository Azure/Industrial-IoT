// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IIoT.OpcUa.Edge {
    using Microsoft.Azure.IIoT.OpcUa.Registry.Models;
    using System.Threading.Tasks;

    /// <summary>
    /// Enables retrieving status
    /// </summary>
    public interface ISupervisorServices {

        /// <summary>
        /// Get supervisor status
        /// </summary>
        /// <returns></returns>
        Task<SupervisorStatusModel> GetStatusAsync();

        /// <summary>
        /// Reset supervisor
        /// </summary>
        /// <returns></returns>
        Task ResetAsync();
    }
}
