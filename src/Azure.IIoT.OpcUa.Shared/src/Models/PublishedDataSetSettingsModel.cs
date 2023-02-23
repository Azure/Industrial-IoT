// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Azure.IIoT.OpcUa.Shared.Models {
    using System;
    using System.Runtime.Serialization;

    /// <summary>
    /// Published dataset settings - corresponds to SubscriptionModel
    /// </summary>
    [DataContract]
    public sealed record class PublishedDataSetSettingsModel {
        /// <summary>
        /// Publishing interval
        /// </summary>
        [DataMember(Name = "publishingInterval", Order = 0,
            EmitDefaultValue = false)]
        public TimeSpan? PublishingInterval { get; set; }

        /// <summary>
        /// Life time
        /// </summary>
        [DataMember(Name = "lifeTimeCount", Order = 1,
            EmitDefaultValue = false)]
        public uint? LifeTimeCount { get; set; }

        /// <summary>
        /// Max keep alive count
        /// </summary>
        [DataMember(Name = "maxKeepAliveCount", Order = 2,
            EmitDefaultValue = false)]
        public uint? MaxKeepAliveCount { get; set; }

        /// <summary>
        /// Max notifications per publish
        /// </summary>
        [DataMember(Name = "maxNotificationsPerPublish", Order = 3,
            EmitDefaultValue = false)]
        public uint? MaxNotificationsPerPublish { get; set; }

        /// <summary>
        /// Priority
        /// </summary>
        [DataMember(Name = "priority", Order = 4,
            EmitDefaultValue = false)]
        public byte? Priority { get; set; }

        /// <summary>
        /// Triggers automatic monitored items display name discovery
        /// </summary>
        [DataMember(Name = "resolveDisplayName", Order = 5,
            EmitDefaultValue = false)]
        public bool? ResolveDisplayName { get; set; }
    }
}
