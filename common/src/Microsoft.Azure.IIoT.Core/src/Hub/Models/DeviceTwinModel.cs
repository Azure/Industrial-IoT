// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IIoT.Hub.Models {
    using System;
    using System.Collections.Generic;
    using System.Runtime.Serialization;
    using Microsoft.Azure.IIoT.Serializers;

    /// <summary>
    /// Model of device registry / twin document
    /// </summary>
    [DataContract]
    public class DeviceTwinModel {

        /// <summary>
        /// Device id
        /// </summary>
        [DataMember(Name = "deviceId")]
        public string Id { get; set; }

        /// <summary>
        /// Module id
        /// </summary>
        [DataMember(Name = "moduleId",
            EmitDefaultValue = false)]
        public string ModuleId { get; set; }

        /// <summary>
        /// Etag for comparison
        /// </summary>
        [DataMember(Name = "etag",
            EmitDefaultValue = false)]
        public string Etag { get; set; }

        /// <summary>
        /// Tags
        /// </summary>
        [DataMember(Name = "tags",
            EmitDefaultValue = false)]
        public Dictionary<string, VariantValue> Tags { get; set; }

        /// <summary>
        /// Settings
        /// </summary>
        [DataMember(Name = "properties",
            EmitDefaultValue = false)]
        public TwinPropertiesModel Properties { get; set; }

        /// <summary>
        /// Capabilities
        /// </summary>
        [DataMember(Name = "capabilities",
            EmitDefaultValue = false)]
        public DeviceCapabilitiesModel Capabilities { get; set; }

        /// <summary>
        /// Twin's Version
        /// </summary>
        [DataMember(Name = "version",
            EmitDefaultValue = false)]
        public long? Version { get; set; }

        /// <summary>
        /// Gets the corresponding Device's Status.
        /// </summary>
        [DataMember(Name = "status",
            EmitDefaultValue = false)]
        public string Status { get; set; }

        /// <summary>
        /// Reason, if any, for the corresponding Device
        /// to be in specified <see cref="Status"/>
        /// </summary>
        [DataMember(Name = "statusReason",
            EmitDefaultValue = false)]
        public string StatusReason { get; set; }

        /// <summary>
        /// Time when the corresponding Device's
        /// <see cref="Status"/> was last updated
        /// </summary>
        [DataMember(Name = "statusUpdatedTime",
            EmitDefaultValue = false)]
        public DateTimeOffset? StatusUpdatedTime { get; set; }

        /// <summary>
        /// Corresponding Device's ConnectionState
        /// </summary>
        [DataMember(Name = "connectionState",
            EmitDefaultValue = false)]
        public string ConnectionState { get; set; }

        /// <summary>
        /// Time when the corresponding Device was last active
        /// </summary>
        [DataMember(Name = "lastActivityTime",
            EmitDefaultValue = false)]
        public DateTimeOffset? LastActivityTime { get; set; }
    }
}
