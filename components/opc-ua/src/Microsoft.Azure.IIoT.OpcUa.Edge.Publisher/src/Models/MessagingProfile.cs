// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IIoT.OpcUa.Edge.Publisher.Models {
    using Microsoft.Azure.IIoT.OpcUa.Publisher.Models;
    using Microsoft.Azure.IIoT.OpcUa.Protocol;
    using System;
    using System.Collections.Generic;
    using System.Text;

    /// <summary>
    /// Messaging profiles supported by OPC Publisher
    /// </summary>
    public class MessagingProfile {

        /// <summary>
        /// Returns true if json encoding is used
        /// </summary>
        public bool IsJson => MessageEncoding.HasFlag(MessageEncoding.Json);

        /// <summary>
        /// Messaging mode
        /// </summary>
        public MessagingMode MessagingMode { get; }

        /// <summary>
        /// Returns true if messaging profiles supports metadata
        /// </summary>
        public bool SupportsMetadata {
            get {
                switch (MessagingMode) {
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
        public bool SupportsKeyFrames {
            get {
                switch (MessagingMode) {
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
        public DataSetContentMask DataSetMessageContentMask { get; }

        /// <summary>
        /// Network message content mask
        /// </summary>
        public NetworkMessageContentMask NetworkMessageContentMask { get; }

        /// <summary>
        /// Content mask for data set message field
        /// </summary>
        public DataSetFieldContentMask DataSetFieldContentMask { get; }

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
            MessageEncoding encoding) {
            var key = (messageMode, GetMessageEncoding(encoding));
            return kProfiles[key];
        }

        /// <summary>
        /// Is this configuration supported
        /// </summary>
        /// <param name="messageMode"></param>
        /// <param name="encoding"></param>
        /// <returns></returns>
        public static bool IsSupported(MessagingMode messageMode,
            MessageEncoding encoding) {
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
            DataSetContentMask dataSetMessageContentMask,
            NetworkMessageContentMask networkMessageContentMask,
            DataSetFieldContentMask dataSetFieldContentMask) {
            MessagingMode = messagingMode;
            MessageEncoding = messageEncoding;
            DataSetMessageContentMask = dataSetMessageContentMask;
            NetworkMessageContentMask = networkMessageContentMask;
            DataSetFieldContentMask = dataSetFieldContentMask;
        }

        /// <inheritdoc/>
        public override bool Equals(object obj) {
            return obj is MessagingProfile profile &&
                DataSetMessageContentMask == profile.DataSetMessageContentMask &&
                NetworkMessageContentMask == profile.NetworkMessageContentMask &&
                DataSetFieldContentMask == profile.DataSetFieldContentMask;
        }

        /// <inheritdoc/>
        public override int GetHashCode() {
            return HashCode.Combine(
                DataSetMessageContentMask,
                NetworkMessageContentMask,
                DataSetFieldContentMask);
        }

        /// <inheritdoc/>
        public override string ToString() {
            var builder = new StringBuilder("| ");
            builder.Append(MessagingMode);
            builder.Append(" | ");
            builder.Append(MessageEncoding);
            builder.Append(" | ");
            builder.Append(NetworkMessageContentMask);
            builder.Append("<br>(");
            builder.AppendFormat("0x{0:X}", (uint)StackTypesEx.ToStackType(
                NetworkMessageContentMask, MessageEncoding));
            builder.Append(") | ");
            builder.Append(DataSetMessageContentMask);
            builder.Append("<br>(");
            builder.AppendFormat("0x{0:X}", (uint)StackTypesEx.ToStackType(
                DataSetMessageContentMask, DataSetFieldContentMask, MessageEncoding));
            builder.Append(") | ");
            builder.Append(DataSetFieldContentMask);
            builder.Append("<br>(");
            builder.AppendFormat("0x{0:X}", (uint)StackTypesEx.ToStackType(
                DataSetFieldContentMask));
            builder.Append(") | ");
            builder.Append(SupportsMetadata ? "X" : " ");
            builder.Append(" | ");
            builder.Append(SupportsKeyFrames ? "X" : " ");
            builder.AppendLine(" |");
            return builder.ToString();
        }

        static MessagingProfile() {

            //
            // New message profiles supported in 2.5
            //

            // Sample mode
            AddProfile(MessagingMode.Samples, BuildDataSetContentMask(false),
                    BuildNetworkMessageContentMask(true),
                    BuildDataSetFieldContentMask(false),
                    MessageEncoding.Json);
            AddProfile(MessagingMode.FullSamples, BuildDataSetContentMask(true),
                    BuildNetworkMessageContentMask(true),
                    BuildDataSetFieldContentMask(true),
                    MessageEncoding.Json);

            //
            // New message profiles supported in 2.6
            //

            // Pub sub
            AddProfile(MessagingMode.PubSub, BuildDataSetContentMask(false),
                    BuildNetworkMessageContentMask(),
                    BuildDataSetFieldContentMask(false),
                    MessageEncoding.Json);
            AddProfile(MessagingMode.FullNetworkMessages, BuildDataSetContentMask(true),
                    BuildNetworkMessageContentMask(),
                    BuildDataSetFieldContentMask(true),
                    MessageEncoding.Json);

            //
            // New message profiles supported in 2.9
            //

            // Pub sub gzipped
            AddProfile(MessagingMode.PubSub, BuildDataSetContentMask(false),
                    BuildNetworkMessageContentMask(),
                    BuildDataSetFieldContentMask(false),
                    MessageEncoding.JsonGzip);
            AddProfile(MessagingMode.FullNetworkMessages, BuildDataSetContentMask(true),
                    BuildNetworkMessageContentMask(),
                    BuildDataSetFieldContentMask(true),
                    MessageEncoding.JsonGzip);

            // Reversible encodings
            AddProfile(MessagingMode.PubSub, BuildDataSetContentMask(false, true),
                    BuildNetworkMessageContentMask(),
                    BuildDataSetFieldContentMask(false),
                    MessageEncoding.JsonReversible, MessageEncoding.JsonReversibleGzip);
            AddProfile(MessagingMode.FullNetworkMessages, BuildDataSetContentMask(true, true),
                    BuildNetworkMessageContentMask(),
                    BuildDataSetFieldContentMask(true),
                    MessageEncoding.JsonReversible, MessageEncoding.JsonReversibleGzip);
            AddProfile(MessagingMode.Samples, BuildDataSetContentMask(false, true),
                    BuildNetworkMessageContentMask(true),
                    BuildDataSetFieldContentMask(false),
                    MessageEncoding.JsonReversible, MessageEncoding.JsonReversibleGzip);
            AddProfile(MessagingMode.FullSamples, BuildDataSetContentMask(true, true),
                    BuildNetworkMessageContentMask(true),
                    BuildDataSetFieldContentMask(true),
                    MessageEncoding.JsonReversible, MessageEncoding.JsonReversibleGzip);

            // Without network message header
            AddProfile(MessagingMode.DataSetMessages, BuildDataSetContentMask(true, false),
                    NetworkMessageContentMask.DataSetMessageHeader,
                    BuildDataSetFieldContentMask(true),
                    MessageEncoding.Json, MessageEncoding.JsonGzip);
            AddProfile(MessagingMode.DataSetMessages, BuildDataSetContentMask(true, true),
                    NetworkMessageContentMask.DataSetMessageHeader,
                    BuildDataSetFieldContentMask(true),
                    MessageEncoding.JsonReversible, MessageEncoding.JsonReversibleGzip);

            // Raw key value pair datasets, non-reversible
            AddProfile(MessagingMode.RawDataSets, 0,
                    0,
                    DataSetFieldContentMask.RawData,
                    MessageEncoding.Json, MessageEncoding.JsonGzip);

            // Uadp encoding
            AddProfile(MessagingMode.PubSub, BuildDataSetContentMask(false),
                    BuildNetworkMessageContentMask(),
                    BuildDataSetFieldContentMask(false),
                    MessageEncoding.Uadp);
            AddProfile(MessagingMode.FullNetworkMessages, BuildDataSetContentMask(true),
                    BuildNetworkMessageContentMask(),
                    BuildDataSetFieldContentMask(true),
                    MessageEncoding.Uadp);
            AddProfile(MessagingMode.DataSetMessages, BuildDataSetContentMask(true, true),
                    NetworkMessageContentMask.DataSetMessageHeader,
                    BuildDataSetFieldContentMask(true),
                    MessageEncoding.Uadp);
            AddProfile(MessagingMode.RawDataSets, 0,
                    0,
                    DataSetFieldContentMask.RawData,
                    MessageEncoding.Uadp);
        }

        /// <summary>
        /// Get a markdown compatible string of all message profiles
        /// </summary>
        /// <returns></returns>
        public static string GetAllAsMarkdownTable() {
            var builder = new StringBuilder();
            builder.Append(
$@"| Messaging Mode<br>(--mm) | Message Encoding<br>(--me) | NetworkMessageContentMask | DataSetMessageContentMask | DataSetFieldContentMask | Metadata supported | KeyFrames supported |
   |--------------------------|----------------------------|---------------------------|---------------------------|-------------------------|--------------------|---------------------|
");
            foreach (var profile in kProfiles) {
                builder.Append(profile.Value.ToString());
            }
            return builder.ToString();
        }

        /// <summary>
        /// Massage the message encoding
        /// </summary>
        /// <param name="encoding"></param>
        /// <returns></returns>
        private static MessageEncoding GetMessageEncoding(MessageEncoding encoding) {
            if (encoding == 0) {
                return MessageEncoding.Json;
            }
            return encoding;
        }

        private static void AddProfile(MessagingMode messagingMode,
            DataSetContentMask dataSetMessageContentMask,
            NetworkMessageContentMask networkMessageContentMask,
            DataSetFieldContentMask dataSetFieldContentMask,
            params MessageEncoding[] messageEncoding) {
            foreach (var encoding in messageEncoding) {
                kProfiles.Add((messagingMode, encoding),
                    new MessagingProfile(messagingMode, encoding,
                        dataSetMessageContentMask,
                        networkMessageContentMask,
                        dataSetFieldContentMask));
            }
        }

        // From published nodes jobs converter
        private static DataSetFieldContentMask BuildDataSetFieldContentMask(
            bool fullFeaturedMessage) =>
            DataSetFieldContentMask.StatusCode |
            DataSetFieldContentMask.SourceTimestamp |
            (fullFeaturedMessage ?
                 (DataSetFieldContentMask.ServerTimestamp |
                  DataSetFieldContentMask.ApplicationUri |
                  DataSetFieldContentMask.ExtensionFields) : 0) |
            DataSetFieldContentMask.NodeId |
            DataSetFieldContentMask.DisplayName |
            DataSetFieldContentMask.EndpointUrl;

        private static DataSetContentMask BuildDataSetContentMask(
            bool fullFeaturedMessage, bool reversibleEncoding = false) =>
            (reversibleEncoding ?
                 (DataSetContentMask.ReversibleFieldEncoding) : 0) |
            (fullFeaturedMessage ?
                 (DataSetContentMask.Timestamp |
                  DataSetContentMask.DataSetWriterId |
                  DataSetContentMask.SequenceNumber) : 0) |
            DataSetContentMask.MetaDataVersion |
            DataSetContentMask.MajorVersion |
            DataSetContentMask.MinorVersion |
            DataSetContentMask.DataSetWriterName |
            DataSetContentMask.MessageType;

        private static NetworkMessageContentMask BuildNetworkMessageContentMask(
            bool isSampleMessage = false) =>
            (isSampleMessage ? NetworkMessageContentMask.MonitoredItemMessage
                 : (NetworkMessageContentMask.NetworkMessageHeader |
                    NetworkMessageContentMask.PublisherId |
                    NetworkMessageContentMask.SequenceNumber |
                    NetworkMessageContentMask.Timestamp |
                    NetworkMessageContentMask.WriterGroupId |
                    NetworkMessageContentMask.PayloadHeader |
                    NetworkMessageContentMask.DataSetClassId |
                    NetworkMessageContentMask.NetworkMessageNumber)) |
            NetworkMessageContentMask.DataSetMessageHeader;

        private static Dictionary<(MessagingMode, MessageEncoding), MessagingProfile> kProfiles
            = new Dictionary<(MessagingMode, MessageEncoding), MessagingProfile>();
    }
}