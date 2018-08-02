// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IIoT.Simulation {
    using Microsoft.Azure.IIoT.Edge.Deployment;
    using System.Threading.Tasks;

    public interface IEdgeSimulation {

        /// <summary>
        /// The id of the simulation
        /// </summary>
        string Name { get; }

        /// <summary>
        /// The device id for the edge device in the
        /// simulation environment
        /// </summary>
        string EdgeDeviceId { get; }

        /// <summary>
        /// Create deployments
        /// </summary>
        IEdgeDeploymentFactory Deployments { get; }

        /// <summary>
        /// Whether the edge device is running correctly
        /// or not.
        /// </summary>
        /// <returns>true if the gateway is running</returns>
        Task<bool> IsEdgeRunningAsync();

        /// <summary>
        /// Check connection status
        /// </summary>
        /// <returns></returns>
        Task<bool> IsEdgeConnectedAsync();

        /// <summary>
        /// Get edge device logs if possible
        /// </summary>
        /// <returns></returns>
        Task<string> GetEdgeLogAsync();

        /// <summary>
        /// Restarts the edge gateway service in the
        /// simulation
        /// </summary>
        /// <returns></returns>
        Task ResetEdgeAsync();
    }
}
