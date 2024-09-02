// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Azure.IIoT.OpcUa.Publisher.Models
{
    using Azure.IIoT.OpcUa.Publisher.Stack;
    using System;
    using System.Collections.Generic;
    using System.Globalization;
    using System.Linq;
    using System.Text;

    /// <summary>
    /// Messaging profiles supported by OPC Publisher
    /// </summary>
    public sealed class MessagingProfile
    {
        /// <summary>
        /// Messaging mode
        /// </summary>
        public MessagingMode MessagingMode { get; }

        /// <summary>
        /// Returns true if messaging profiles supports metadata
        /// </summary>
        public bool SupportsMetadata
        {
            get
            {
                switch (MessagingMode)
                {
                    case MessagingMode.FullSamples:
                    case MessagingMode.Samples:
                    case MessagingMode.RawDataSets:
                        return false;
                    default:
                        return true;
                }
            }
        }

        /// <summary>
        /// Returns true if messaging profiles supports keyframes
        /// </summary>
        public bool SupportsKeyFrames
        {
            get
            {
                switch (MessagingMode)
                {
                    case MessagingMode.FullSamples:
                    case MessagingMode.Samples:
                        return false;
                    default:
                        return true;
                }
            }
        }

        /// <summary>
        /// Returns true if messaging profiles supports keep alive
        /// </summary>
        public bool SupportsKeepAlive
        {
            get
            {
                switch (MessagingMode)
                {
                    case MessagingMode.FullSamples:
                    case MessagingMode.Samples:
                        return false;
                    default:
                        return true;
                }
            }
        }

        /// <summary>
        /// Messaging encoding
        /// </summary>
        public MessageEncoding MessageEncoding { get; }

        /// <summary>
        /// Dataset message content mask
        /// </summary>
        public DataSetMessageContentFlags DataSetMessageContentMask { get; }

        /// <summary>
        /// Network message content mask
        /// </summary>
        public NetworkMessageContentFlags NetworkMessageContentMask { get; }

        /// <summary>
        /// Content mask for data set message field
        /// </summary>
        public DataSetFieldContentFlags DataSetFieldContentMask { get; }

        /// <summary>
        /// Get supported options
        /// </summary>
        public static IEnumerable<MessagingProfile> Supported => kProfiles.Values;

        /// <summary>
        /// Get encoding
        /// </summary>
        /// <param name="messageMode"></param>
        /// <param name="encoding"></param>
        /// <returns></returns>
        public static MessagingProfile Get(MessagingMode messageMode,
            MessageEncoding encoding)
        {
            var key = (messageMode, GetMessageEncoding(encoding));
            return kProfiles[key];
        }

        /// <summary>
        /// Find profile
        /// </summary>
        /// <param name="messageType"></param>
        /// <param name="networkMessageContentMask"></param>
        /// <param name="dataSetMessageContentMask"></param>
        /// <param name="dataSetFieldContentMask"></param>
        /// <returns></returns>
        public static MessagingProfile? Find(MessageEncoding? messageType,
            NetworkMessageContentFlags? networkMessageContentMask,
            DataSetMessageContentFlags? dataSetMessageContentMask,
            DataSetFieldContentFlags? dataSetFieldContentMask)
        {
            // TODO: Use hash code from custom messaging profile?
            return kProfiles.Values
                .FirstOrDefault(p =>
                    (messageType == null ||
                        p.MessageEncoding == messageType) &&
                    (networkMessageContentMask == null ||
                        p.NetworkMessageContentMask == networkMessageContentMask) &&
                    (dataSetMessageContentMask == null ||
                        p.DataSetMessageContentMask == dataSetMessageContentMask) &&
                    (dataSetFieldContentMask == null ||
                        p.DataSetFieldContentMask == dataSetFieldContentMask))
                ;
        }

        /// <summary>
        /// Is this configuration supported
        /// </summary>
        /// <param name="messageMode"></param>
        /// <param name="encoding"></param>
        /// <returns></returns>
        public static bool IsSupported(MessagingMode messageMode,
            MessageEncoding encoding)
        {
            var key = (messageMode, GetMessageEncoding(encoding));
            return kProfiles.ContainsKey(key);
        }

        /// <summary>
        /// Create message encoding profile
        /// </summary>
        /// <param name="messagingMode"></param>
        /// <param name="messageEncoding"></param>
        /// <param name="dataSetMessageContentMask"></param>
        /// <param name="networkMessageContentMask"></param>
        /// <param name="dataSetFieldContentMask"></param>
        private MessagingProfile(MessagingMode messagingMode,
            MessageEncoding messageEncoding,
            DataSetMessageContentFlags dataSetMessageContentMask,
            NetworkMessageContentFlags networkMessageContentMask,
            DataSetFieldContentFlags dataSetFieldContentMask)
        {
            MessagingMode = messagingMode;
            MessageEncoding = messageEncoding;
            DataSetMessageContentMask = dataSetMessageContentMask;
            NetworkMessageContentMask = networkMessageContentMask;
            DataSetFieldContentMask = dataSetFieldContentMask;
        }

        /// <inheritdoc/>
        public override bool Equals(object? obj)
        {
            return obj is MessagingProfile profile &&
                DataSetMessageContentMask == profile.DataSetMessageContentMask &&
                NetworkMessageContentMask == profile.NetworkMessageContentMask &&
                DataSetFieldContentMask == profile.DataSetFieldContentMask;
        }

        /// <inheritdoc/>
        public override int GetHashCode()
        {
            return HashCode.Combine(
                DataSetMessageContentMask,
                NetworkMessageContentMask,
                DataSetFieldContentMask);
        }

        /// <inheritdoc/>
        public override string ToString()
        {
            return $"{MessagingMode}|{MessageEncoding}";
        }

        /// <inheritdoc/>
        public string ToExpandedString()
        {
            return new StringBuilder("| ")
                .Append(MessagingMode)
                .Append(" | ")
                .Append(MessageEncoding)
                .Append(" | ")
                .Append(NetworkMessageContentMask)
                .Append("<br>(")
                .AppendFormat(CultureInfo.InvariantCulture, "0x{0:X}", StackTypesEx.ToStackType(
                    NetworkMessageContentMask, MessageEncoding))
                .Append(") | ")
                .Append(DataSetMessageContentMask)
                .Append("<br>(")
                .AppendFormat(CultureInfo.InvariantCulture, "0x{0:X}", StackTypesEx.ToStackType(
                    DataSetMessageContentMask, DataSetFieldContentMask, MessageEncoding))
                .Append(") | ")
                .Append(DataSetFieldContentMask)
                .Append("<br>(")
                .AppendFormat(CultureInfo.InvariantCulture, "0x{0:X}", (uint)StackTypesEx.ToStackType(
                    DataSetFieldContentMask))
                .Append(") | ")
                .Append(SupportsMetadata ? "X" : " ")
                .Append(" | ")
                .Append(SupportsKeyFrames ? "X" : " ")
                .Append(" | ")
                .Append(SupportsKeepAlive ? "X" : " ")
                .AppendLine(" |")
                .ToString();
        }

        static MessagingProfile()
        {
            //
            // New message profiles supported in 2.5
            //

            // Sample mode
            AddProfile(MessagingMode.Samples, BuildDataSetContentMask(),
                    BuildNetworkMessageContentMask(true),
                    BuildDataSetFieldContentMask(),
                    MessageEncoding.Json);
            AddProfile(MessagingMode.FullSamples, BuildDataSetContentMask(true),
                    BuildNetworkMessageContentMask(true),
                    BuildDataSetFieldContentMask(true),
                    MessageEncoding.Json);

            //
            // New message profiles supported in 2.6
            //

            // Pub sub
            AddProfile(MessagingMode.PubSub, BuildDataSetContentMask(),
                    BuildNetworkMessageContentMask(),
                    BuildDataSetFieldContentMask(),
                    MessageEncoding.Json);
            AddProfile(MessagingMode.FullNetworkMessages, BuildDataSetContentMask(true),
                    BuildNetworkMessageContentMask(),
                    BuildDataSetFieldContentMask(true),
                    MessageEncoding.Json);

            //
            // New message profiles supported in 2.9
            //

            // Pub sub gzipped
            AddProfile(MessagingMode.PubSub, BuildDataSetContentMask(),
                    BuildNetworkMessageContentMask(),
                    BuildDataSetFieldContentMask(),
                    MessageEncoding.JsonGzip);
            AddProfile(MessagingMode.FullNetworkMessages, BuildDataSetContentMask(true),
                    BuildNetworkMessageContentMask(),
                    BuildDataSetFieldContentMask(true),
                    MessageEncoding.JsonGzip);

            // Reversible encodings
            AddProfile(MessagingMode.PubSub, BuildDataSetContentMask(false, true),
                    BuildNetworkMessageContentMask(),
                    BuildDataSetFieldContentMask(),
                    MessageEncoding.JsonReversible, MessageEncoding.JsonReversibleGzip);
            AddProfile(MessagingMode.FullNetworkMessages, BuildDataSetContentMask(true, true),
                    BuildNetworkMessageContentMask(),
                    BuildDataSetFieldContentMask(true),
                    MessageEncoding.JsonReversible, MessageEncoding.JsonReversibleGzip);
            AddProfile(MessagingMode.Samples, BuildDataSetContentMask(false, true),
                    BuildNetworkMessageContentMask(true),
                    BuildDataSetFieldContentMask(),
                    MessageEncoding.JsonReversible, MessageEncoding.JsonReversibleGzip);
            AddProfile(MessagingMode.FullSamples, BuildDataSetContentMask(true, true),
                    BuildNetworkMessageContentMask(true),
                    BuildDataSetFieldContentMask(true),
                    MessageEncoding.JsonReversible, MessageEncoding.JsonReversibleGzip);

            // Without network message header
            AddProfile(MessagingMode.DataSetMessages, BuildDataSetContentMask(),
                    NetworkMessageContentFlags.DataSetMessageHeader,
                    BuildDataSetFieldContentMask(),
                    MessageEncoding.Json, MessageEncoding.JsonGzip);
            AddProfile(MessagingMode.DataSetMessages, BuildDataSetContentMask(false, true),
                    NetworkMessageContentFlags.DataSetMessageHeader,
                    BuildDataSetFieldContentMask(),
                    MessageEncoding.JsonReversible, MessageEncoding.JsonReversibleGzip);
            AddProfile(MessagingMode.SingleDataSetMessage, BuildDataSetContentMask(),
                    NetworkMessageContentFlags.DataSetMessageHeader | NetworkMessageContentFlags.SingleDataSetMessage,
                    BuildDataSetFieldContentMask(),
                    MessageEncoding.Json, MessageEncoding.JsonGzip);
            AddProfile(MessagingMode.SingleDataSetMessage, BuildDataSetContentMask(false, true),
                    NetworkMessageContentFlags.DataSetMessageHeader | NetworkMessageContentFlags.SingleDataSetMessage,
                    BuildDataSetFieldContentMask(),
                    MessageEncoding.JsonReversible, MessageEncoding.JsonReversibleGzip);

            AddProfile(MessagingMode.DataSets, 0,
                    0,
                    BuildDataSetFieldContentMask(),
                    MessageEncoding.Json, MessageEncoding.JsonGzip);
            AddProfile(MessagingMode.SingleDataSet, 0,
                    NetworkMessageContentFlags.SingleDataSetMessage,
                    BuildDataSetFieldContentMask(),
                    MessageEncoding.Json, MessageEncoding.JsonGzip);
            AddProfile(MessagingMode.DataSets, 0,
                    0,
                    BuildDataSetFieldContentMask(),
                    MessageEncoding.JsonReversible, MessageEncoding.JsonReversibleGzip);
            AddProfile(MessagingMode.SingleDataSet, 0,
                    NetworkMessageContentFlags.SingleDataSetMessage,
                    BuildDataSetFieldContentMask(),
                    MessageEncoding.JsonReversible, MessageEncoding.JsonReversibleGzip);
            AddProfile(MessagingMode.RawDataSets, 0,
                    0,
                    DataSetFieldContentFlags.RawData,
                    MessageEncoding.Json, MessageEncoding.JsonGzip);
            AddProfile(MessagingMode.SingleRawDataSet, 0,
                    NetworkMessageContentFlags.SingleDataSetMessage,
                    DataSetFieldContentFlags.RawData,
                    MessageEncoding.Json, MessageEncoding.JsonGzip);
            AddProfile(MessagingMode.RawDataSets, 0,
                    0,
                    DataSetFieldContentFlags.RawData,
                    MessageEncoding.JsonReversible, MessageEncoding.JsonReversibleGzip);
            AddProfile(MessagingMode.SingleRawDataSet, 0,
                    NetworkMessageContentFlags.SingleDataSetMessage,
                    DataSetFieldContentFlags.RawData,
                    MessageEncoding.JsonReversible, MessageEncoding.JsonReversibleGzip);

            // Uadp encoding
            AddProfile(MessagingMode.PubSub, BuildDataSetContentMask(),
                    BuildNetworkMessageContentMask(),
                    BuildDataSetFieldContentMask(),
                    MessageEncoding.Uadp);
            AddProfile(MessagingMode.FullNetworkMessages, BuildDataSetContentMask(true),
                    BuildNetworkMessageContentMask(),
                    BuildDataSetFieldContentMask(),
                    MessageEncoding.Uadp);
            AddProfile(MessagingMode.DataSetMessages, BuildDataSetContentMask(),
                    NetworkMessageContentFlags.DataSetMessageHeader,
                    BuildDataSetFieldContentMask(),
                    MessageEncoding.Uadp);
            AddProfile(MessagingMode.SingleDataSetMessage, BuildDataSetContentMask(),
                    NetworkMessageContentFlags.DataSetMessageHeader | NetworkMessageContentFlags.SingleDataSetMessage,
                    BuildDataSetFieldContentMask(),
                    MessageEncoding.Uadp);
            AddProfile(MessagingMode.RawDataSets, 0,
                    0,
                    DataSetFieldContentFlags.RawData,
                    MessageEncoding.Uadp);
            AddProfile(MessagingMode.SingleRawDataSet, 0,
                    NetworkMessageContentFlags.SingleDataSetMessage,
                    DataSetFieldContentFlags.RawData,
                    MessageEncoding.Uadp);
        }

        /// <summary>
        /// Get a markdown compatible string of all message profiles
        /// </summary>
        /// <returns></returns>
        public static string GetAllAsMarkdownTable()
        {
            var builder = new StringBuilder();
            builder.Append(
@"| Messaging Mode<br>(--mm) | Message Encoding<br>(--me) | NetworkMessageContentMask | DataSetMessageContentMask | DataSetFieldContentMask | Metadata supported | KeyFrames supported | KeepAlive supported | Schema publishing |
   |--------------------------|----------------------------|---------------------------|---------------------------|-------------------------|--------------------|---------------------|---------------------|-------------------|
");
            foreach (var profile in kProfiles)
            {
                builder.Append(profile.Value.ToExpandedString());
            }
            return builder.ToString();
        }

        /// <summary>
        /// Massage the message encoding
        /// </summary>
        /// <param name="encoding"></param>
        /// <returns></returns>
        private static MessageEncoding GetMessageEncoding(MessageEncoding encoding)
        {
            if (encoding == 0)
            {
                return MessageEncoding.Json;
            }
            return encoding;
        }

        private static void AddProfile(MessagingMode messagingMode,
            DataSetMessageContentFlags dataSetMessageContentMask,
            NetworkMessageContentFlags networkMessageContentMask,
            DataSetFieldContentFlags dataSetFieldContentMask,
            params MessageEncoding[] messageEncoding)
        {
            foreach (var encoding in messageEncoding)
            {
                kProfiles.Add((messagingMode, encoding),
                    new MessagingProfile(messagingMode, encoding,
                        dataSetMessageContentMask,
                        networkMessageContentMask,
                        dataSetFieldContentMask));
            }
        }

        /// <summary>
        /// From published nodes jobs converter
        /// </summary>
        /// <param name="fullFeaturedMessage"></param>
        /// <returns></returns>
        private static DataSetFieldContentFlags BuildDataSetFieldContentMask(
            bool fullFeaturedMessage = false)
        {
            return
                DataSetFieldContentFlags.StatusCode |
                DataSetFieldContentFlags.SourceTimestamp |
                (fullFeaturedMessage ?
                     (DataSetFieldContentFlags.ServerTimestamp |
                      DataSetFieldContentFlags.ApplicationUri |
                      DataSetFieldContentFlags.ExtensionFields) : 0) |
                DataSetFieldContentFlags.NodeId |
                DataSetFieldContentFlags.DisplayName |
                DataSetFieldContentFlags.EndpointUrl;
        }

        private static DataSetMessageContentFlags BuildDataSetContentMask(
            bool fullFeaturedMessage = false, bool reversibleEncoding = false)
        {
            return
                (reversibleEncoding ?
                     DataSetMessageContentFlags.ReversibleFieldEncoding : 0) |
                (fullFeaturedMessage ?
                    (DataSetMessageContentFlags.Timestamp |
                     DataSetMessageContentFlags.DataSetWriterId |
                     DataSetMessageContentFlags.SequenceNumber) : 0) |
                DataSetMessageContentFlags.MetaDataVersion |
                DataSetMessageContentFlags.MajorVersion |
                DataSetMessageContentFlags.MinorVersion |
                DataSetMessageContentFlags.DataSetWriterName |
                DataSetMessageContentFlags.MessageType;
        }

        private static NetworkMessageContentFlags BuildNetworkMessageContentMask(
            bool isSampleMessage = false)
        {
            return (isSampleMessage ?
                NetworkMessageContentFlags.MonitoredItemMessage
                 : (NetworkMessageContentFlags.NetworkMessageHeader |
                    NetworkMessageContentFlags.PublisherId |
                    NetworkMessageContentFlags.SequenceNumber |
                    NetworkMessageContentFlags.Timestamp |
                    NetworkMessageContentFlags.WriterGroupId |
                    NetworkMessageContentFlags.PayloadHeader |
                    NetworkMessageContentFlags.DataSetClassId |
                    NetworkMessageContentFlags.NetworkMessageNumber)) |
                NetworkMessageContentFlags.DataSetMessageHeader;
        }

        private static readonly Dictionary<(MessagingMode, MessageEncoding), MessagingProfile> kProfiles = new();
    }
}
