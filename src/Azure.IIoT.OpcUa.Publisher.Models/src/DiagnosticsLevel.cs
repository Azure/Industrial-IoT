// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Azure.IIoT.OpcUa.Publisher.Models
{
    using System.Runtime.Serialization;

    /// <summary>
    /// Level of diagnostics requested in responses
    /// </summary>
    [DataContract]
    public enum DiagnosticsLevel
    {
        /// <summary>
        /// Include no diagnostics in response
        /// </summary>
        [EnumMember(Value = "None")]
        None = 0,

        /// <summary>
        /// Include status and symbol text (default)
        /// </summary>
        [EnumMember(Value = "Status")]
        Status = 1,

        /// <summary>
        /// Include additional information
        /// </summary>
        [EnumMember(Value = "Information")]
        Information = 10,

        /// <summary>
        /// Include inner diagnostics for tracing
        /// </summary>
        [EnumMember(Value = "Debug")]
        Debug = 50,

        /// <summary>
        /// Include full diagnostics trace.
        /// </summary>
        [EnumMember(Value = "Verbose")]
        Verbose = 100
    }
}
