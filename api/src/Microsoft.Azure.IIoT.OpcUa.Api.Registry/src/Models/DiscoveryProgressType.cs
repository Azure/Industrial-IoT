// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IIoT.OpcUa.Api.Registry.Models {
    using System.Runtime.Serialization;

    /// <summary>
    /// Discovery progress event type
    /// </summary>
    [DataContract]
    public enum DiscoveryProgressType {

        /// <summary>
        /// Discovery Pending
        /// </summary>
        [EnumMember]
        Pending,

        /// <summary>
        /// Discovery run started
        /// </summary>
        [EnumMember]
        Started,

        /// <summary>
        /// Discovery was cancelled
        /// </summary>
        [EnumMember]
        Cancelled,

        /// <summary>
        /// Discovery resulted in error
        /// </summary>
        [EnumMember]
        Error,

        /// <summary>
        /// Discovery finished
        /// </summary>
        [EnumMember]
        Finished,

        /// <summary>
        /// Network scanning started
        /// </summary>
        [EnumMember]
        NetworkScanStarted,

        /// <summary>
        /// Network scanning result
        /// </summary>
        [EnumMember]
        NetworkScanResult,

        /// <summary>
        /// Network scan progress
        /// </summary>
        [EnumMember]
        NetworkScanProgress,

        /// <summary>
        /// Network scan finished
        /// </summary>
        [EnumMember]
        NetworkScanFinished,

        /// <summary>
        /// Port scan started
        /// </summary>
        [EnumMember]
        PortScanStarted,

        /// <summary>
        /// Port scan result
        /// </summary>
        [EnumMember]
        PortScanResult,

        /// <summary>
        /// Port scan progress
        /// </summary>
        [EnumMember]
        PortScanProgress,

        /// <summary>
        /// Port scan finished
        /// </summary>
        [EnumMember]
        PortScanFinished,

        /// <summary>
        /// Server discovery started
        /// </summary>
        [EnumMember]
        ServerDiscoveryStarted,

        /// <summary>
        /// Endpoint discovery started
        /// </summary>
        [EnumMember]
        EndpointsDiscoveryStarted,

        /// <summary>
        /// Endpoint discovery finished
        /// </summary>
        [EnumMember]
        EndpointsDiscoveryFinished,

        /// <summary>
        /// Server discovery finished
        /// </summary>
        [EnumMember]
        ServerDiscoveryFinished,
    }
}
