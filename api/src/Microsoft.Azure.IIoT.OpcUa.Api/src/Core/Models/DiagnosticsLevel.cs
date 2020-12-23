// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IIoT.OpcUa.Api.Core.Models {
    using System.Runtime.Serialization;

    /// <summary>
    /// Level of diagnostics requested in responses
    /// </summary>
    [DataContract]
    public enum DiagnosticsLevel {

        /// <summary>
        /// Include no diagnostics in response
        /// </summary>
        [EnumMember]
        None = 0,

        /// <summary>
        /// Include only status text as array (default)
        /// </summary>
        [EnumMember]
        Status = 1,

        /// <summary>
        /// Include status and operations trace.
        /// </summary>
        [EnumMember]
        Operations = 10,

        /// <summary>
        /// Include diagnostics
        /// </summary>
        [EnumMember]
        Diagnostics = 50,

        /// <summary>
        /// Include full diagnostics trace.
        /// </summary>
        [EnumMember]
        Verbose = 100
    }
}
