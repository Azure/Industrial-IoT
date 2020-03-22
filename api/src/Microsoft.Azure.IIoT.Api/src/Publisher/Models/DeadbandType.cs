// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IIoT.OpcUa.Api.Publisher.Models {
    using System.Runtime.Serialization;

    /// <summary>
    /// Deadband type
    /// </summary>
    [DataContract]
    public enum DeadbandType {

        /// <summary>
        /// Absolute
        /// </summary>
        [EnumMember]
        Absolute,

        /// <summary>
        /// Percentage
        /// </summary>
        [EnumMember]
        Percent
    }
}