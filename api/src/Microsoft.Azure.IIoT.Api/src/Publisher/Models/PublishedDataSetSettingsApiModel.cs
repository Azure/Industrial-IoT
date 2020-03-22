// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IIoT.OpcUa.Api.Publisher.Models {
    using System.Runtime.Serialization;
    using System;

    /// <summary>
    /// Published dataset settings - corresponds to SubscriptionModel
    /// </summary>
    [DataContract]
    public class PublishedDataSetSettingsApiModel {

        /// <summary>
        /// Publishing interval
        /// </summary>
        [DataMember(Name = "publishingInterval",
            EmitDefaultValue = false)]
        public TimeSpan? PublishingInterval { get; set; }

        /// <summary>
        /// Life time
        /// </summary>
        [DataMember(Name = "lifeTimeCount",
            EmitDefaultValue = false)]
        public uint? LifeTimeCount { get; set; }

        /// <summary>
        /// Max keep alive count
        /// </summary>
        [DataMember(Name = "maxKeepAliveCount",
            EmitDefaultValue = false)]
        public uint? MaxKeepAliveCount { get; set; }

        /// <summary>
        /// Max notifications per publish
        /// </summary>
        [DataMember(Name = "maxNotificationsPerPublish",
            EmitDefaultValue = false)]
        public uint? MaxNotificationsPerPublish { get; set; }

        /// <summary>
        /// Priority
        /// </summary>
        [DataMember(Name = "priority",
            EmitDefaultValue = false)]
        public byte? Priority { get; set; }
    }
}