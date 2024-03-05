// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Azure.IIoT.OpcUa.Publisher.Models
{
    using System;
    using System.Runtime.Serialization;

    /// <summary>
    /// Published dataset settings - corresponds to SubscriptionModel
    /// </summary>
    [DataContract]
    public sealed record class PublishedDataSetSettingsModel
    {
        /// <summary>
        /// Publishing interval
        /// </summary>
        [DataMember(Name = "publishingInterval", Order = 0,
            EmitDefaultValue = false)]
        public TimeSpan? PublishingInterval { get; init; }

        /// <summary>
        /// Life time
        /// </summary>
        [DataMember(Name = "lifeTimeCount", Order = 1,
            EmitDefaultValue = false)]
        public uint? LifeTimeCount { get; init; }

        /// <summary>
        /// Max keep alive count
        /// </summary>
        [DataMember(Name = "maxKeepAliveCount", Order = 2,
            EmitDefaultValue = false)]
        public uint? MaxKeepAliveCount { get; init; }

        /// <summary>
        /// Max notifications per publish
        /// </summary>
        [DataMember(Name = "maxNotificationsPerPublish", Order = 3,
            EmitDefaultValue = false)]
        public uint? MaxNotificationsPerPublish { get; init; }

        /// <summary>
        /// Priority
        /// </summary>
        [DataMember(Name = "priority", Order = 4,
            EmitDefaultValue = false)]
        public byte? Priority { get; init; }

        /// <summary>
        /// Triggers automatic monitored items display name discovery
        /// </summary>
        [DataMember(Name = "resolveDisplayName", Order = 5,
            EmitDefaultValue = false)]
        public bool? ResolveDisplayName { get; init; }

        /// <summary>
        /// Use deferred acknoledgements
        /// </summary>
        [DataMember(Name = "useDeferredAcknoledgements", Order = 6,
            EmitDefaultValue = false)]
        public bool? UseDeferredAcknoledgements { get; init; }

        /// <summary>
        /// Will set the subscription to have publishing
        /// enabled and every monitored item created to be
        /// in desired monitoring mode.
        /// </summary>
        [DataMember(Name = "enableImmediatePublishing", Order = 8,
            EmitDefaultValue = false)]
        public bool? EnableImmediatePublishing { get; init; }

        /// <summary>
        /// Enable sequential publishing feature in the stack.
        /// </summary>
        [DataMember(Name = "enableSequentialPublishing", Order = 9,
            EmitDefaultValue = false)]
        public bool? EnableSequentialPublishing { get; init; }
    }
}
