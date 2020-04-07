// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IIoT.OpcUa.Api.Publisher.Models {
    using System.Runtime.Serialization;
    using System;

    /// <summary>
    /// Content for dataset field
    /// </summary>
    [Flags]
    [DataContract]
    public enum DataSetFieldContentMask {

        /// <summary>
        /// Status code
        /// </summary>
        [EnumMember]
        StatusCode = 0x1,

        /// <summary>
        /// Source timestamp
        /// </summary>
        [EnumMember]
        SourceTimestamp = 0x2,

        /// <summary>
        /// Server timestamp
        /// </summary>
        [EnumMember]
        ServerTimestamp = 0x4,

        /// <summary>
        /// Source picoseconds
        /// </summary>
        [EnumMember]
        SourcePicoSeconds = 0x8,

        /// <summary>
        /// Server picoseconds
        /// </summary>
        [EnumMember]
        ServerPicoSeconds = 0x10,

        /// <summary>
        /// Raw value
        /// </summary>
        [EnumMember]
        RawData = 0x20,

        /// <summary>
        /// Node id included
        /// </summary>
        [EnumMember]
        NodeId = 0x10000,

        /// <summary>
        /// Display name included
        /// </summary>
        [EnumMember]
        DisplayName = 0x20000,

        /// <summary>
        /// Endpoint url included
        /// </summary>
        [EnumMember]
        EndpointUrl = 0x40000,

        /// <summary>
        /// Application uri
        /// </summary>
        [EnumMember]
        ApplicationUri = 0x80000,

        /// <summary>
        /// Subscription id included
        /// </summary>
        [EnumMember]
        SubscriptionId = 0x100000,

        /// <summary>
        /// Extra fields included
        /// </summary>
        [EnumMember]
        ExtraFields = 0x200000
    }
}