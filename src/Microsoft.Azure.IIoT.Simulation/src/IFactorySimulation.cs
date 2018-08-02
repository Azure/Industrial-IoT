// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IIoT.Simulation {
    using Microsoft.Azure.IIoT.Edge.Deployment;
    using Newtonsoft.Json.Linq;
    using System;
    using System.Collections.Generic;
    using System.Threading.Tasks;

    public interface IFactorySimulation : IDisposable {

        /// <summary>
        /// Create deployments
        /// </summary>
        IEdgeDeploymentFactory Deployments { get; }

        /// <summary>
        /// Creates a Simulation Environment consisting
        /// of a IoT Edge device and simulated devices,
        /// which can be individually controlled.
        /// </summary>
        /// <param name="tags">Tags to use when
        /// creating iot-edge instances</param>
        /// <returns></returns>
        Task<ISimulationHost> CreateAsync(
            Dictionary<string, JToken> tags);

        /// <summary>
        /// List all created simulations as identifiers.
        /// </summary>
        /// <returns></returns>
        Task<IEnumerable<string>> ListAsync();

        /// <summary>
        /// Get simulation by named identifier.
        /// </summary>
        /// <param name="id"></param>
        /// <returns></returns>
        Task<ISimulationHost> GetAsync(string id);

        /// <summary>
        /// Delete simulation
        /// </summary>
        /// <param name="id"></param>
        /// <returns></returns>
        Task DeleteAsync(string id);

        /// <summary>
        /// Stop simulator and dispose of all created
        /// simulation environments.
        /// </summary>
        /// <returns></returns>
        Task CloseAsync();
    }
}
