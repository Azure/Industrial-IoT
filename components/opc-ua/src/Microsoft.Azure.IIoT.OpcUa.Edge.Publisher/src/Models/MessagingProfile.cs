// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IIoT.OpcUa.Edge.Publisher.Models {
    using Microsoft.Azure.IIoT.OpcUa.Publisher.Models;
    using System;
    using System.Collections.Generic;

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
            return $"({MessagingMode} and {MessageEncoding})";
        }

        static MessagingProfile() {

            //
            // New message profiles supported in 2.5
            //

            // Sample mode
            AddProfile(MessagingMode.Samples, BuildDataSetContentMask(false),
                    BuildNetworkMessageContentMask() | NetworkMessageContentMask.MonitoredItemMessage,
                    BuildDataSetFieldContentMask(false),
                    MessageEncoding.Json);
            AddProfile(MessagingMode.FullSamples, BuildDataSetContentMask(true),
                    BuildNetworkMessageContentMask() | NetworkMessageContentMask.MonitoredItemMessage,
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
                    BuildNetworkMessageContentMask() | NetworkMessageContentMask.MonitoredItemMessage,
                    BuildDataSetFieldContentMask(false),
                    MessageEncoding.JsonReversible, MessageEncoding.JsonReversibleGzip);
            AddProfile(MessagingMode.FullSamples, BuildDataSetContentMask(true, true),
                    BuildNetworkMessageContentMask() | NetworkMessageContentMask.MonitoredItemMessage,
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
            (fullFeaturedMessage ? DataSetFieldContentMask.ServerTimestamp : 0) |
            DataSetFieldContentMask.NodeId |
            DataSetFieldContentMask.DisplayName |
            (fullFeaturedMessage ? DataSetFieldContentMask.ApplicationUri : 0) |
            DataSetFieldContentMask.EndpointUrl |
            (fullFeaturedMessage ? DataSetFieldContentMask.ExtensionFields : 0);

        private static DataSetContentMask BuildDataSetContentMask(
            bool fullFeaturedMessage, bool reversibleEncoding = false) =>
            (reversibleEncoding ? DataSetContentMask.ReversibleFieldEncoding : 0) |
            (fullFeaturedMessage ? DataSetContentMask.Timestamp : 0) |
            DataSetContentMask.MetaDataVersion |
            DataSetContentMask.MajorVersion |
            DataSetContentMask.MinorVersion |
            (fullFeaturedMessage ? DataSetContentMask.DataSetWriterId : 0) |
            DataSetContentMask.DataSetWriterName |
            DataSetContentMask.MessageType |
            (fullFeaturedMessage ? DataSetContentMask.SequenceNumber : 0);

        private static NetworkMessageContentMask BuildNetworkMessageContentMask() =>
            NetworkMessageContentMask.PublisherId |
            NetworkMessageContentMask.WriterGroupId |
            NetworkMessageContentMask.NetworkMessageNumber |
            NetworkMessageContentMask.SequenceNumber |
            NetworkMessageContentMask.PayloadHeader |
            NetworkMessageContentMask.Timestamp |
            NetworkMessageContentMask.DataSetClassId |
            NetworkMessageContentMask.NetworkMessageHeader |
            NetworkMessageContentMask.DataSetMessageHeader;

        private static Dictionary<(MessagingMode, MessageEncoding), MessagingProfile> kProfiles
            = new Dictionary<(MessagingMode, MessageEncoding), MessagingProfile>();
    }
}