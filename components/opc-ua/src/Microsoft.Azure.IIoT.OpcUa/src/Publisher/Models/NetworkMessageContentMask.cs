// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IIoT.OpcUa.Publisher.Models {
    using Newtonsoft.Json;
    using Newtonsoft.Json.Converters;

    /// <summary>
    /// Network message content
    /// </summary>
    [JsonConverter(typeof(StringEnumConverter))]
    public enum NetworkMessageContentMask {

        /// <summary>
        /// Publisher id
        /// </summary>
        PublisherId = 1,

        /// <summary>
        /// Group header
        /// </summary>
        GroupHeader = 2,

        /// <summary>
        /// Writer group id
        /// </summary>
        WriterGroupId = 4,

        /// <summary>
        /// Group version
        /// </summary>
        GroupVersion = 8,

        /// <summary>
        /// Network message number
        /// </summary>
        NetworkMessageNumber = 16,

        /// <summary>
        /// Sequence number
        /// </summary>
        SequenceNumber = 32,

        /// <summary>
        /// Payload header
        /// </summary>
        PayloadHeader = 64,

        /// <summary>
        /// Timestamp
        /// </summary>
        Timestamp = 128,

        /// <summary>
        /// Picoseconds
        /// </summary>
        Picoseconds = 256,

        /// <summary>
        /// Dataset class id
        /// </summary>
        DataSetClassId = 512,

        /// <summary>
        /// Promoted fields
        /// </summary>
        PromotedFields = 1024,

        /// <summary>
        /// Network message header
        /// </summary>
        NetworkMessageHeader = 2048,

        /// <summary>
        /// Dataset message header
        /// </summary>
        DataSetMessageHeader = 4096,

        /// <summary>
        /// Single dataset messages
        /// </summary>
        SingleDataSetMessage = 8192,

        /// <summary>
        /// Reply to
        /// </summary>
        ReplyTo = 16384
    }
}