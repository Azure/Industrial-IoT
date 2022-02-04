// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IIoT.OpcUa.Api.Publisher.Models {
    using System.Runtime.Serialization;

    /// <summary>
    /// Monitoring modes
    /// </summary>
    [DataContract]
    public enum MonitoringMode {

        /// <summary>
        /// Disabled
        /// </summary>
        [EnumMember]
        Disabled,

        /// <summary>
        /// Sampling
        /// </summary>
        [EnumMember]
        Sampling,

        /// <summary>
        /// Reporting
        /// </summary>
        [EnumMember]
        Reporting
    }
}