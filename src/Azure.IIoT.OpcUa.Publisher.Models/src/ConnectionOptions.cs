// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Azure.IIoT.OpcUa.Publisher.Models
{
    using System;
    using System.Runtime.Serialization;

    /// <summary>
    /// Options that can be applied to a connection
    /// </summary>
    [Flags]
    [DataContract]
    public enum ConnectionOptions
    {
        /// <summary>
        /// No options
        /// </summary>
        [EnumMember(Value = "None")]
        None = 0x0,

        /// <summary>
        /// Create a reverse connection
        /// </summary>
        [EnumMember(Value = "UseReverseConnect")]
        UseReverseConnect = 0x1,

        /// <summary>
        /// Do not load complex types
        /// </summary>
        [EnumMember(Value = "NoComplexTypeSystem")]
        NoComplexTypeSystem = 0x10,

        /// <summary>
        /// Do not transfer subscription on reconnect
        /// </summary>
        [EnumMember(Value = "NoSubscriptionTransfer")]
        NoSubscriptionTransfer = 0x20,

        /// <summary>
        /// Dump diagnostics on a timer
        /// </summary>
        [EnumMember(Value = "DumpDiagnostics")]
        DumpDiagnostics = 0x100
    }
}
