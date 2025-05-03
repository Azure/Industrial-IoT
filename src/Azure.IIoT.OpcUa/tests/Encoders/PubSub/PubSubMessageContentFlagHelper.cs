// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Azure.IIoT.OpcUa.Publisher.Stack
{
    using Azure.IIoT.OpcUa.Publisher.Models;
    using Opc.Ua;

    /// <summary>
    /// Stack types conversions
    /// </summary>
    internal static class PubSubMessageContentFlagHelper
    {
        /// <summary>
        /// Get network message content mask
        /// </summary>
        /// <param name="mask"></param>
        /// <returns></returns>
        public static NetworkMessageContentFlags StackToNetworkMessageContentFlags(uint mask)
        {
            NetworkMessageContentFlags result = 0u;
            if ((mask & (uint)JsonNetworkMessageContentMask.PublisherId) != 0)
            {
                result |= NetworkMessageContentFlags.PublisherId;
            }
            if ((mask & (uint)JsonNetworkMessageContentMask.DataSetClassId) != 0)
            {
                result |= NetworkMessageContentFlags.DataSetClassId;
            }
            if ((mask & (uint)JsonNetworkMessageContentMask.ReplyTo) != 0)
            {
                result |= NetworkMessageContentFlags.ReplyTo;
            }
            if ((mask & (uint)JsonNetworkMessageContentMask.NetworkMessageHeader) != 0)
            {
                result |= NetworkMessageContentFlags.NetworkMessageHeader;
            }
            else
            {
                // If not set, bits 3, 4 and 5 can also not be set
                result = 0;
            }
            if ((mask & 0x8000000) != 0)
            {
                // If monitored item message, then no network message header
                result = 0;
            }
            if ((mask & (uint)JsonNetworkMessageContentMask.DataSetMessageHeader) != 0)
            {
                result |= NetworkMessageContentFlags.DataSetMessageHeader;
            }
            if ((mask & (uint)JsonNetworkMessageContentMask.SingleDataSetMessage) != 0)
            {
                result |= NetworkMessageContentFlags.SingleDataSetMessage;
            }
            return result;
        }

        /// <summary>
        /// Get dataset message content mask
        /// </summary>
        /// <param name="mask"></param>
        /// <returns></returns>
        public static DataSetMessageContentFlags StackToDataSetMessageContentFlags(uint mask)
        {
            DataSetMessageContentFlags result = 0u;
            if ((mask & (uint)JsonDataSetMessageContentMask.Timestamp) != 0)
            {
                result |= DataSetMessageContentFlags.Timestamp;
            }
            if ((mask & (uint)JsonDataSetMessageContentMask.Status) != 0)
            {
                result |= DataSetMessageContentFlags.Status;
            }
            if ((mask & (uint)JsonDataSetMessageContentMask.MetaDataVersion) != 0)
            {
                result |= DataSetMessageContentFlags.MetaDataVersion;
            }
            if ((mask & (uint)JsonDataSetMessageContentMask.SequenceNumber) != 0)
            {
                result |= DataSetMessageContentFlags.SequenceNumber;
            }
            if ((mask & (uint)JsonDataSetMessageContentMask.DataSetWriterId) != 0)
            {
                result |= DataSetMessageContentFlags.DataSetWriterId;
            }
            if ((mask & (uint)JsonDataSetMessageContentMask.MessageType) != 0)
            {
                result |= DataSetMessageContentFlags.MessageType;
            }
            if ((mask & (uint)JsonDataSetMessageContentMask.DataSetWriterName) != 0)
            {
                result |= DataSetMessageContentFlags.DataSetWriterName;
            }
            if ((mask & (uint)JsonDataSetMessageContentMask.FieldEncoding1) != 0)
            {
                result |= DataSetMessageContentFlags.ReversibleFieldEncoding;
            }

            //     if (fieldMask != null)
            //     {
            //         if ((fieldMask & DataSetFieldContentFlags.NodeId) != 0)
            //         {
            //             result |= JsonDataSetMessageContentMaskEx.NodeId;
            //         }
            //         if ((fieldMask & DataSetFieldContentFlags.DisplayName) != 0)
            //         {
            //             result |= JsonDataSetMessageContentMaskEx.DisplayName;
            //         }
            //         if ((fieldMask & DataSetFieldContentFlags.ExtensionFields) != 0)
            //         {
            //             result |= JsonDataSetMessageContentMaskEx.ExtensionFields;
            //         }
            //         if ((fieldMask & DataSetFieldContentFlags.EndpointUrl) != 0)
            //         {
            //             result |= JsonDataSetMessageContentMaskEx.EndpointUrl;
            //         }
            //         if ((fieldMask & DataSetFieldContentFlags.ApplicationUri) != 0)
            //         {
            //             result |= JsonDataSetMessageContentMaskEx.ApplicationUri;
            //         }
            //     }
            return result;
        }

        /// <summary>
        /// Get dataset message content mask
        /// </summary>
        /// <param name="mask"></param>
        /// <returns></returns>
        public static DataSetFieldContentFlags StackToDataSetFieldContentFlags(uint mask)
        {
            DataSetFieldContentFlags result = 0;
            if ((mask & (uint)DataSetFieldContentMask.StatusCode) != 0)
            {
                result |= DataSetFieldContentFlags.StatusCode;
            }
            if ((mask & (uint)DataSetFieldContentMask.SourceTimestamp) != 0)
            {
                result |= DataSetFieldContentFlags.SourceTimestamp;
            }
            if ((mask & (uint)DataSetFieldContentMask.ServerTimestamp) != 0)
            {
                result |= DataSetFieldContentFlags.ServerTimestamp;
            }
            if ((mask & (uint)DataSetFieldContentMask.SourcePicoSeconds) != 0)
            {
                result |= DataSetFieldContentFlags.SourcePicoSeconds;
            }
            if ((mask & (uint)DataSetFieldContentMask.ServerPicoSeconds) != 0)
            {
                result |= DataSetFieldContentFlags.ServerPicoSeconds;
            }
            if ((mask & (uint)DataSetFieldContentMask.RawData) != 0)
            {
                result |= DataSetFieldContentFlags.RawData;
            }
            if ((mask & 64) != 0)
            {
                result |= DataSetFieldContentFlags.SingleFieldDegradeToValue;
            }
            return result;
        }
    }
}
