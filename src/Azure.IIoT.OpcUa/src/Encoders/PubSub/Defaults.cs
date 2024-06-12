// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Azure.IIoT.OpcUa.Encoders.PubSub
{
    using Azure.IIoT.OpcUa.Publisher.Models;

    /// <summary>
    /// Type conversion
    /// </summary>
    public static class Defaults
    {
        /// <summary>
        /// Network message defaults
        /// </summary>
        public const NetworkMessageContentFlags NetworkMessageContent =
            NetworkMessageContentFlags.NetworkMessageHeader |
            NetworkMessageContentFlags.NetworkMessageNumber |
            NetworkMessageContentFlags.DataSetMessageHeader |
            NetworkMessageContentFlags.PublisherId |
            NetworkMessageContentFlags.DataSetClassId;

        /// <summary>
        /// Default field mask
        /// </summary>
        public const DataSetFieldContentFlags DataSetFieldContent =
            DataSetFieldContentFlags.StatusCode |
            DataSetFieldContentFlags.SourcePicoSeconds |
            DataSetFieldContentFlags.SourceTimestamp |
            DataSetFieldContentFlags.ServerPicoSeconds |
            DataSetFieldContentFlags.ServerTimestamp;

        /// <summary>
        /// Default message flags
        /// </summary>
        public const DataSetMessageContentFlags DataSetMessageContent =
            DataSetMessageContentFlags.DataSetWriterId |
            DataSetMessageContentFlags.DataSetWriterName |
            DataSetMessageContentFlags.MetaDataVersion |
            DataSetMessageContentFlags.MajorVersion |
            DataSetMessageContentFlags.MinorVersion |
            DataSetMessageContentFlags.SequenceNumber |
            DataSetMessageContentFlags.Timestamp |
            DataSetMessageContentFlags.MessageType |
            DataSetMessageContentFlags.Status;
    }
}
