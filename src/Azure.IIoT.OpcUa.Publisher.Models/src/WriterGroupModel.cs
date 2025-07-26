// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Azure.IIoT.OpcUa.Publisher.Models
{
    using Furly.Extensions.Serializers;
    using System;
    using System.Collections.Generic;
    using System.ComponentModel.DataAnnotations;
    using System.Runtime.Serialization;

    /// <summary>
    /// Network message writer group model
    /// </summary>
    [DataContract]
    public sealed record class WriterGroupModel
    {
        /// <summary>
        /// Writer group identifier
        /// </summary>
        [DataMember(Name = "id", Order = 0)]
        [Required]
        public required string Id { get; set; }

        /// <summary>
        /// Network message types to generate
        /// (publisher extension)
        /// </summary>
        [DataMember(Name = "messageType", Order = 1,
            EmitDefaultValue = false)]
        public MessageEncoding? MessageType { get; set; }

        /// <summary>
        /// The data set writers generating
        /// dataset messages in the group
        /// </summary>
        [DataMember(Name = "dataSetWriters", Order = 2,
            EmitDefaultValue = false)]
        public IReadOnlyList<DataSetWriterModel>? DataSetWriters { get; set; }

        /// <summary>
        /// Network message configuration
        /// </summary>
        [DataMember(Name = "messageSettings", Order = 3,
            EmitDefaultValue = false)]
        public WriterGroupMessageSettingsModel? MessageSettings { get; set; }

        /// <summary>
        /// Priority of the writer group
        /// </summary>
        [DataMember(Name = "priority", Order = 4,
            EmitDefaultValue = false)]
        public byte? Priority { get; set; }

        /// <summary>
        /// Name of the writer group
        /// </summary>
        [DataMember(Name = "name", Order = 5,
            EmitDefaultValue = false)]
        public string? Name { get; set; }

        /// <summary>
        /// Locales to use
        /// </summary>
        [DataMember(Name = "localeIds", Order = 6,
            EmitDefaultValue = false)]
        public IReadOnlyList<string>? LocaleIds { get; set; }

        /// <summary>
        /// Header layout uri
        /// </summary>
        [DataMember(Name = "headerLayoutUri", Order = 7,
            EmitDefaultValue = false)]
        public string? HeaderLayoutUri { get; set; }

        /// <summary>
        /// Security mode
        /// </summary>
        [DataMember(Name = "securityMode", Order = 8,
            EmitDefaultValue = false)]
        public SecurityMode? SecurityMode { get; set; }

        /// <summary>
        /// Security group to use
        /// </summary>
        [DataMember(Name = "securityGroupId", Order = 9,
            EmitDefaultValue = false)]
        public string? SecurityGroupId { get; set; }

        /// <summary>
        /// Security key services to use
        /// </summary>
        [DataMember(Name = "securityKeyServices", Order = 10,
            EmitDefaultValue = false)]
        public IReadOnlyList<ConnectionModel>? SecurityKeyServices { get; set; }

        /// <summary>
        /// Max network message size. The max size is limited
        /// by the capabilities of the underlying event client
        /// transport, e.g., 256k in the case of IoT Hub.
        /// </summary>
        [DataMember(Name = "maxNetworkMessageSize", Order = 11,
            EmitDefaultValue = false)]
        public uint? MaxNetworkMessageSize { get; set; }

        /// <summary>
        /// Publishing interval is the time to wait before
        /// generating a network message from the queue of
        /// collected notifications. The default is set to
        /// the value of the BatchTriggerInterval option.
        /// </summary>
        [DataMember(Name = "publishingInterval", Order = 12,
            EmitDefaultValue = false)]
        public TimeSpan? PublishingInterval { get; set; }

        /// <summary>
        /// Keep alive time
        /// </summary>
        [DataMember(Name = "keepAliveTime", Order = 13,
            EmitDefaultValue = false)]
        public TimeSpan? KeepAliveTime { get; set; }

        /// <summary>
        /// Number of notifications to queue before sending a batch
        /// (Publisher extension).
        /// The default is set to the value of the BatchSize option.
        /// </summary>
        [DataMember(Name = "notificationPublishThreshold", Order = 14,
            EmitDefaultValue = false)]
        public uint? NotificationPublishThreshold { get; set; }

        /// <summary>
        /// Max publish queue size.
        /// </summary>
        [DataMember(Name = "publishQueueSize", Order = 15,
            EmitDefaultValue = false)]
        public uint? PublishQueueSize { get; set; }

        /// <summary>
        /// Desired Transport to use. If transport is not registered
        /// the default (first) registered transport is used.
        /// (Publisher extension)
        /// </summary>
        [DataMember(Name = "transport", Order = 16,
            EmitDefaultValue = false)]
        public WriterGroupTransport? Transport { get; set; }

        /// <summary>
        /// Queue settings to use for messages in the writer group.
        /// (Publisher extension)
        /// </summary>
        [DataMember(Name = "publishing", Order = 17,
            EmitDefaultValue = false)]
        public PublishingQueueSettingsModel? Publishing { get; set; }

        /// <summary>
        /// Number of partitions to create of the publish queue.
        /// (Publisher extension)
        /// </summary>
        [DataMember(Name = "publishQueuePartitions", Order = 18,
            EmitDefaultValue = false)]
        public int? PublishQueuePartitions { get; set; }

        /// <summary>
        /// Root node of the writer group
        /// </summary>
        [DataMember(Name = "rootNode", Order = 19,
            EmitDefaultValue = false)]
        public string? RootNode { get; set; }

        /// <summary>
        /// Type of the writer group
        /// </summary>
        [DataMember(Name = "type", Order = 20,
            EmitDefaultValue = false)]
        public string? Type { get; set; }

        /// <summary>
        /// Writer group properties and attributes
        /// (Publisher extension)
        /// </summary>
        [DataMember(Name = "properties", Order = 21,
            EmitDefaultValue = false)]
        public Dictionary<string, VariantValue>? Properties { get; set; }

        /// <summary>
        /// Publisher identifier if different from the global one
        /// (Publisher extension)
        /// </summary>
        [DataMember(Name = "publisherId", Order = 22,
            EmitDefaultValue = false)]
        public string? PublisherId { get; set; }
    }
}
