// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Azure.IIoT.OpcUa.Publisher.Models
{
    using Furly.Extensions.Messaging;
    using System;
    using System.Runtime.Serialization;

    /// <summary>
    /// Publishing Queue settings
    /// </summary>
    [DataContract]
    public sealed record class PublishingQueueSettingsModel
    {
        /// <summary>
        /// Queue name writer should use to publish messages
        /// to.
        /// </summary>
        [DataMember(Name = "queueName", Order = 1,
            EmitDefaultValue = false)]
        public string? QueueName { get; set; }

        /// <summary>
        /// Desired Quality of service to use in case of broker
        /// transport that supports configuring delivery guarantees.
        /// </summary>
        [DataMember(Name = "requestedDeliveryGuarantee", Order = 2,
            EmitDefaultValue = false)]
        public QoS? RequestedDeliveryGuarantee { get; set; }

        /// <summary>
        /// Desired Time to live to use in case of using a broker
        /// transport that supports ttl.
        /// </summary>
        [DataMember(Name = "ttl", Order = 3,
            EmitDefaultValue = false)]
        public TimeSpan? Ttl { get; set; }

        /// <summary>
        /// If the broker transport supports message retention this
        /// setting determines if the messages should be retained
        /// in the queue.
        /// </summary>
        [DataMember(Name = "retain", Order = 4,
            EmitDefaultValue = false)]
        public bool? Retain { get; set; }
    }
}
