// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Azure.IIoT.OpcUa.Encoders
{
    using Azure.IIoT.OpcUa.Publisher.Models;

    /// <summary>
    /// What a network message, a data set message and a data set field carry
    /// when a writer does not say.
    /// </summary>
    /// <remarks>
    /// These describe the message rather than the encoder that writes it, so
    /// they outlived the custom encoder they were declared on. Schema
    /// generation reads them to describe a writer that leaves its content mask
    /// unset, and it must describe the same message the runtime publishes.
    /// </remarks>
    public static class PubSubMessageDefaults
    {
        /// <summary>
        /// What a network message carries when the writer group does not say.
        /// </summary>
        public const NetworkMessageContentFlags DefaultNetworkMessageContentFlags =
            NetworkMessageContentFlags.NetworkMessageHeader |
            NetworkMessageContentFlags.NetworkMessageNumber |
            NetworkMessageContentFlags.DataSetMessageHeader |
            NetworkMessageContentFlags.PublisherId |
            NetworkMessageContentFlags.DataSetClassId;

        /// <summary>
        /// What a data set field carries when the writer does not say.
        /// </summary>
        public const DataSetFieldContentFlags DefaultDataSetFieldContentFlags =
            DataSetFieldContentFlags.StatusCode |
            DataSetFieldContentFlags.SourcePicoSeconds |
            DataSetFieldContentFlags.SourceTimestamp |
            DataSetFieldContentFlags.ServerPicoSeconds |
            DataSetFieldContentFlags.ServerTimestamp;

        /// <summary>
        /// What a data set message carries when the writer does not say.
        /// </summary>
        public const DataSetMessageContentFlags DefaultDataSetMessageContentFlags =
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
