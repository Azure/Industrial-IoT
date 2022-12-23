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
        /// Returns true if messaging profiles supports metadata
        /// </summary>
        public bool IsJson {
            get {
                switch (MessageEncoding) {
                    case MessageEncoding.JsonReversible:
                    case MessageEncoding.Json:
                        return true;
                    default:
                        return false;
                }
            }
        }

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
        /// <param name="fullFeaturedMessage"></param>
        /// <returns></returns>
        public static MessagingProfile Get(MessagingMode messageMode,
            MessageEncoding encoding, bool fullFeaturedMessage = false) {
            var key = (GetMessageMode(messageMode, fullFeaturedMessage), encoding);
            return kProfiles[key];
        }

        /// <summary>
        /// Is this configuration supported
        /// </summary>
        /// <param name="messageMode"></param>
        /// <param name="encoding"></param>
        /// <param name="fullFeaturedMessage"></param>
        /// <returns></returns>
        public static bool IsSupported(MessagingMode messageMode,
            MessageEncoding encoding, bool fullFeaturedMessage = false) {
            var key = (GetMessageMode(messageMode, fullFeaturedMessage), encoding);
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
            AddProfile(MessagingMode.Samples, MessageEncoding.Json,
                    BuildDataSetContentMask(false),
                    BuildNetworkMessageContentMask(),
                    BuildDataSetFieldContentMask(false));
            AddProfile(MessagingMode.FullSamples, MessageEncoding.Json,
                    BuildDataSetContentMask(true),
                    BuildNetworkMessageContentMask(),
                    BuildDataSetFieldContentMask(true));

            //
            // New message profiles supported in 2.6
            //

            // Pub sub
            AddProfile(MessagingMode.PubSub, MessageEncoding.Json,
                    BuildDataSetContentMask(false),
                    BuildNetworkMessageContentMask(),
                    BuildDataSetFieldContentMask(false));
            AddProfile(MessagingMode.FullNetworkMessages, MessageEncoding.Json,
                    BuildDataSetContentMask(true),
                    BuildNetworkMessageContentMask(),
                    BuildDataSetFieldContentMask(true));

            //
            // New message profiles supported in 2.8
            //

            AddProfile(MessagingMode.Samples, MessageEncoding.Binary,
                    BuildDataSetContentMask(false),
                    BuildNetworkMessageContentMask(),
                    BuildDataSetFieldContentMask(false));
            AddProfile(MessagingMode.FullSamples, MessageEncoding.Binary,
                    BuildDataSetContentMask(true),
                    BuildNetworkMessageContentMask(),
                    BuildDataSetFieldContentMask(true));

            //
            // New message profiles supported in 2.9
            //

            // Reversible encodings
            AddProfile(MessagingMode.PubSub, MessageEncoding.JsonReversible,
                    BuildDataSetContentMask(false, true),
                    BuildNetworkMessageContentMask(),
                    BuildDataSetFieldContentMask(false));
            AddProfile(MessagingMode.FullNetworkMessages, MessageEncoding.JsonReversible,
                    BuildDataSetContentMask(true, true),
                    BuildNetworkMessageContentMask(),
                    BuildDataSetFieldContentMask(true));
            AddProfile(MessagingMode.Samples, MessageEncoding.JsonReversible,
                    BuildDataSetContentMask(false, true),
                    BuildNetworkMessageContentMask(),
                    BuildDataSetFieldContentMask(false));
            AddProfile(MessagingMode.FullSamples, MessageEncoding.JsonReversible,
                    BuildDataSetContentMask(true, true),
                    BuildNetworkMessageContentMask(),
                    BuildDataSetFieldContentMask(true));

#if DEBUG
            // Without network message header
            AddProfile(MessagingMode.DataSetMessages, MessageEncoding.Json,
                    BuildDataSetContentMask(true, false),
                    NetworkMessageContentMask.DataSetMessageHeader,
                    BuildDataSetFieldContentMask(true));
            AddProfile(MessagingMode.DataSetMessages, MessageEncoding.JsonReversible,
                    BuildDataSetContentMask(true, true),
                    NetworkMessageContentMask.DataSetMessageHeader,
                    BuildDataSetFieldContentMask(true));

            // Raw key value pairs
            AddProfile(MessagingMode.RawDataSets, MessageEncoding.Json,
                    0,
                    0,
                    DataSetFieldContentMask.RawData);
            AddProfile(MessagingMode.RawDataSets, MessageEncoding.JsonReversible,
                    0,
                    0,
                    0);

            // Uadp encoding
            AddProfile(MessagingMode.PubSub, MessageEncoding.Uadp,
                    BuildDataSetContentMask(false),
                    BuildNetworkMessageContentMask(),
                    BuildDataSetFieldContentMask(false));
            AddProfile(MessagingMode.FullNetworkMessages, MessageEncoding.Uadp,
                    BuildDataSetContentMask(true),
                    BuildNetworkMessageContentMask(),
                    BuildDataSetFieldContentMask(true));
            AddProfile(MessagingMode.DataSetMessages, MessageEncoding.Uadp,
                    BuildDataSetContentMask(true, true),
                    NetworkMessageContentMask.DataSetMessageHeader,
                    BuildDataSetFieldContentMask(true));
            AddProfile(MessagingMode.RawDataSets, MessageEncoding.Uadp,
                    0,
                    0,
                    DataSetFieldContentMask.RawData);
#endif
        }

        /// <summary>
        /// Massage the message mode based on the legacy flag
        /// </summary>
        /// <param name="messageMode"></param>
        /// <param name="fullFeaturedMessage"></param>
        /// <returns></returns>
        private static MessagingMode GetMessageMode(MessagingMode messageMode, bool fullFeaturedMessage) {
            if (fullFeaturedMessage) {
                switch (messageMode) {
                    case MessagingMode.PubSub:
                        return MessagingMode.FullNetworkMessages;
                    case MessagingMode.Samples:
                        return MessagingMode.FullSamples;
                }
            }
            return messageMode;
        }

        private static void AddProfile(MessagingMode messagingMode,
            MessageEncoding messageEncoding,
            DataSetContentMask dataSetMessageContentMask,
            NetworkMessageContentMask networkMessageContentMask,
            DataSetFieldContentMask dataSetFieldContentMask) {
            kProfiles.Add((messagingMode, messageEncoding),
                new MessagingProfile(messagingMode, messageEncoding,
                    dataSetMessageContentMask,
                    networkMessageContentMask,
                    dataSetFieldContentMask));
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
            (fullFeaturedMessage ? DataSetContentMask.DataSetWriterId : 0) |
            DataSetContentMask.DataSetWriterName |
            DataSetContentMask.MessageType |
            DataSetContentMask.MajorVersion |
            DataSetContentMask.MinorVersion |
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