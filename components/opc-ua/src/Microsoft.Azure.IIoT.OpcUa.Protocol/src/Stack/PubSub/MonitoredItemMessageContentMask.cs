// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Opc.Ua {
    using System;

    /// <summary>
    /// Monitored item message content fields
    /// </summary>
    [Flags]
    public enum MonitoredItemMessageContentMask {

        /// <summary>
        /// Source timestamp included
        /// </summary>
        SourceTimestamp = 0x1,

        /// <summary>
        /// Source picoseconds
        /// </summary>
        SourcePicoSeconds = 0x2,

        /// <summary>
        /// Server timestamp included
        /// </summary>
        ServerTimestamp = 0x4,

        /// <summary>
        /// Server picoseconds
        /// </summary>
        ServerPicoSeconds = 0x8,

        /// <summary>
        /// Message source timestamp
        /// </summary>
        Timestamp = 0x10,

        /// <summary>
        /// Message picoseconds
        /// </summary>
        PicoSeconds = 0x20,

        /// <summary>
        /// Statuscode included
        /// </summary>
        StatusCode = 0x40,

        /// <summary>
        /// Status included
        /// </summary>
        Status = 0x80,

        /// <summary>
        /// Node id included
        /// </summary>
        NodeId = 0x100,

        /// <summary>
        /// Endpoint url included
        /// </summary>
        EndpointUrl = 0x200,

        /// <summary>
        /// Application uri
        /// </summary>
        ApplicationUri = 0x400,

        /// <summary>
        /// Display name included
        /// </summary>
        DisplayName = 0x800,

        /// <summary>
        /// Extra fields included
        /// </summary>
        ExtensionFields = 0x1000,

        /// <summary>
        /// Sequence number included
        /// </summary>
        SequenceNumber = 0x2000
    }
}