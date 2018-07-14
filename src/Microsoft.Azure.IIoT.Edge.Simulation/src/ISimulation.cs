// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IIoT.Edge.Simulation {
    using System.Threading.Tasks;
    using Microsoft.Extensions.Configuration;
    using Renci.SshNet;

    /// <summary>
    /// The client library creates individual Linux Virtual
    /// Machines for each *Simulation Environment* using
    /// Azure Management client and installs the iot edge runtime
    /// and provisions it in iot hub.
    ///
    /// The environment is deleted once it is disposed,
    /// cleaning up all resources, including the IoT Edge
    /// device that was provisioned in IoT Hub.
    /// </summary>
    public interface ISimulation {

        /// <summary>
        /// The id of the simulation
        /// </summary>
        string Id { get; }

        /// <summary>
        /// The device id for the edge device in the environment
        /// </summary>
        string EdgeDeviceId { get; }

        /// <summary>
        /// Get the ssh connection information to open your own
        /// ssh client, or scp, or sftp with.
        /// </summary>
        ConnectionInfo SshConnectionInfo { get; }

        /// <summary>
        /// Create simulated device in environment. The device
        /// can be started and stopped to simulate device
        /// failures.
        /// </summary>
        /// <param name="type"></param>
        /// <param name="configuration"></param>
        /// <returns></returns>
        ISimulatedDevice CreateDevice(DeviceType type,
            IConfiguration configuration);

        /// <summary>
        /// Restarts the edge gateway
        /// </summary>
        /// <returns></returns>
        Task ResetGatewayAsync();

        /// <summary>
        /// Reset entire simulation
        /// </summary>
        /// <returns></returns>
        Task ResetAsync();
    }
}