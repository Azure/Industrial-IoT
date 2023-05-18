// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Azure.IIoT.OpcUa.Publisher.Models
{
    using System.Runtime.Serialization;

    /// <summary>
    /// Monitoring modes
    /// </summary>
    [DataContract]
    public enum MonitoringMode
    {
        /// <summary>
        /// Disabled
        /// </summary>
        [EnumMember(Value = "Disabled")]
        Disabled,

        /// <summary>
        /// Sampling
        /// </summary>
        [EnumMember(Value = "Sampling")]
        Sampling,

        /// <summary>
        /// Reporting
        /// </summary>
        [EnumMember(Value = "Reporting")]
        Reporting
    }
}
