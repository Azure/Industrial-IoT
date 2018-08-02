// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IIoT.Simulation {
    using Microsoft.Azure.IIoT.Net;
    using System.Threading.Tasks;

    /// <summary>
    /// Host of a simulation, e.g. vm, or local environment
    /// </summary>
    public interface ISimulationHost {

        /// <summary>
        /// Open a secure shell
        /// </summary>
        /// <returns></returns>
        Task<ISecureShell> OpenSecureShellAsync();

        /// <summary>
        /// Reset entire simulation
        /// </summary>
        /// <returns></returns>
        Task RestartAsync();
    }
}
