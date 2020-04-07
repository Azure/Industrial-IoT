// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IIoT.OpcUa.Api.Registry.Models {
    using System.Runtime.Serialization;

    /// <summary>
    /// Log level
    /// </summary>
    [DataContract]
    public enum TraceLogLevel {

        /// <summary>
        /// Error only
        /// </summary>
        [EnumMember]
        Error = 0,

        /// <summary>
        /// Default
        /// </summary>
        [EnumMember]
        Information = 1,

        /// <summary>
        /// Debug log
        /// </summary>
        [EnumMember]
        Debug = 2,

        /// <summary>
        /// Verbose
        /// </summary>
        [EnumMember]
        Verbose = 3
    }
}
