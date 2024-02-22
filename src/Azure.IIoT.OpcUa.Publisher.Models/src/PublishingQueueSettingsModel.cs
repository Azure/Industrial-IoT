// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Azure.IIoT.OpcUa.Publisher.Models
{
    using Furly.Extensions.Messaging;
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
        /// transport.
        /// </summary>
        [DataMember(Name = "requestedDeliveryGuarantee", Order = 2,
            EmitDefaultValue = false)]
        public QoS? RequestedDeliveryGuarantee { get; set; }
    }
}
