// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Azure.IIoT.OpcUa.Publisher.Models
{
    using System.Runtime.Serialization;

    /// <summary>
    /// Discovery progress event type
    /// </summary>
    [DataContract]
    public enum DiscoveryProgressType
    {
        /// <summary>
        /// Discovery run pending
        /// </summary>
        [EnumMember(Value = "Pending")]
        Pending,

        /// <summary>
        /// Discovery run started
        /// </summary>
        [EnumMember(Value = "Started")]
        Started,

        /// <summary>
        /// Discovery was cancelled
        /// </summary>
        [EnumMember(Value = "Cancelled")]
        Cancelled,

        /// <summary>
        /// Discovery resulted in error
        /// </summary>
        [EnumMember(Value = "Error")]
        Error,

        /// <summary>
        /// Discovery finished
        /// </summary>
        [EnumMember(Value = "Finished")]
        Finished,

        /// <summary>
        /// Network scanning started
        /// </summary>
        [EnumMember(Value = "NetworkScanStarted")]
        NetworkScanStarted,

        /// <summary>
        /// Network scanning result
        /// </summary>
        [EnumMember(Value = "NetworkScanResult")]
        NetworkScanResult,

        /// <summary>
        /// Network scan progress
        /// </summary>
        [EnumMember(Value = "NetworkScanProgress")]
        NetworkScanProgress,

        /// <summary>
        /// Network scan finished
        /// </summary>
        [EnumMember(Value = "NetworkScanFinished")]
        NetworkScanFinished,

        /// <summary>
        /// Port scan started
        /// </summary>
        [EnumMember(Value = "PortScanStarted")]
        PortScanStarted,

        /// <summary>
        /// Port scan result
        /// </summary>
        [EnumMember(Value = "PortScanResult")]
        PortScanResult,

        /// <summary>
        /// Port scan progress
        /// </summary>
        [EnumMember(Value = "PortScanProgress")]
        PortScanProgress,

        /// <summary>
        /// Port scan finished
        /// </summary>
        [EnumMember(Value = "PortScanFinished")]
        PortScanFinished,

        /// <summary>
        /// Server discovery started
        /// </summary>
        [EnumMember(Value = "ServerDiscoveryStarted")]
        ServerDiscoveryStarted,

        /// <summary>
        /// Endpoint discovery started
        /// </summary>
        [EnumMember(Value = "EndpointsDiscoveryStarted")]
        EndpointsDiscoveryStarted,

        /// <summary>
        /// Endpoint discovery finished
        /// </summary>
        [EnumMember(Value = "EndpointsDiscoveryFinished")]
        EndpointsDiscoveryFinished,

        /// <summary>
        /// Server discovery finished
        /// </summary>
        [EnumMember(Value = "ServerDiscoveryFinished")]
        ServerDiscoveryFinished,
    }
}
