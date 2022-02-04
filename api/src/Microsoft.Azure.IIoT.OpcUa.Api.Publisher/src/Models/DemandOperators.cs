// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IIoT.OpcUa.Api.Publisher.Models {
    using System.Runtime.Serialization;

    /// <summary>
    /// Demand operator
    /// </summary>
    [DataContract]
    public enum DemandOperators {

        /// <summary>
        /// Equals
        /// </summary>
        [EnumMember]
        Equals,

        /// <summary>
        /// Match
        /// </summary>
        [EnumMember]
        Match,

        /// <summary>
        /// Exists
        /// </summary>
        [EnumMember]
        Exists
    }
}