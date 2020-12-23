// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IIoT.OpcUa.Registry.Models {

    /// <summary>
    /// Discovery progress event
    /// </summary>
    public enum DiscoveryProgressType {

        /// <summary>
        /// Discovery run pending
        /// </summary>
        Pending,

        /// <summary>
        /// Discovery run started
        /// </summary>
        Started,

        /// <summary>
        /// Discovery was cancelled
        /// </summary>
        Cancelled,

        /// <summary>
        /// Discovery resulted in error
        /// </summary>
        Error,

        /// <summary>
        /// Discovery finished
        /// </summary>
        Finished,

        /// <summary>
        /// Network scanning started
        /// </summary>
        NetworkScanStarted,

        /// <summary>
        /// Network scanning result
        /// </summary>
        NetworkScanResult,

        /// <summary>
        /// Network scan progress
        /// </summary>
        NetworkScanProgress,

        /// <summary>
        /// Network scan finished
        /// </summary>
        NetworkScanFinished,

        /// <summary>
        /// Port scan started
        /// </summary>
        PortScanStarted,

        /// <summary>
        /// Port scan result
        /// </summary>
        PortScanResult,

        /// <summary>
        /// Port scan progress
        /// </summary>
        PortScanProgress,

        /// <summary>
        /// Port scan finished
        /// </summary>
        PortScanFinished,

        /// <summary>
        /// Server discovery started
        /// </summary>
        ServerDiscoveryStarted,

        /// <summary>
        /// Endpoint discovery started
        /// </summary>
        EndpointsDiscoveryStarted,

        /// <summary>
        /// Endpoint discovery finished
        /// </summary>
        EndpointsDiscoveryFinished,

        /// <summary>
        /// Server discovery finished
        /// </summary>
        ServerDiscoveryFinished,
    }
}
