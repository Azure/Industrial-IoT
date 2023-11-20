// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Azure.IIoT.OpcUa.Publisher.Models
{
    using System.Runtime.Serialization;

    /// <summary>
    /// Target to sent publisher diagnostics to
    /// </summary>
    [DataContract]
    public enum PublisherDiagnosticTargetType
    {
        /// <summary>
        /// Diagnostics are emitted to logger
        /// </summary>
        [EnumMember(Value = "Logger")]
        Logger,

        /// <summary>
        /// Diagnostics are sent as events
        /// </summary>
        [EnumMember(Value = "Events")]
        Events
    }
}
