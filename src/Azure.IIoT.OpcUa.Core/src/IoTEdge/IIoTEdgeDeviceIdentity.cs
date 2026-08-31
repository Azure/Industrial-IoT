// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Azure.IIoT.OpcUa.Core.IoTEdge
{
    /// <summary>
    /// Edge device identity.
    /// </summary>
    public interface IIoTEdgeDeviceIdentity
    {
        /// <summary>
        /// Hub of the identity.
        /// </summary>
        string? Hub { get; }

        /// <summary>
        /// Device id.
        /// </summary>
        string DeviceId { get; }

        /// <summary>
        /// Module id.
        /// </summary>
        string? ModuleId { get; }

        /// <summary>
        /// Edge gateway hostname.
        /// </summary>
        string? Gateway { get; }
    }
}
