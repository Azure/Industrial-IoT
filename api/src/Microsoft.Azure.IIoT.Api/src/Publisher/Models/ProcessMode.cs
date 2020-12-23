// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IIoT.OpcUa.Api.Publisher.Models {
    using System.Runtime.Serialization;

    /// <summary>
    /// Processing mode for processing engine
    /// </summary>
    [DataContract]
    public enum ProcessMode {

        /// <summary>
        /// Active processing
        /// </summary>
        [EnumMember]
        Active,

        /// <summary>
        /// Passive
        /// </summary>
        [EnumMember]
        Passive
    }
}