// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IIoT.OpcUa.Api.Publisher.Models {
    using Microsoft.Azure.IIoT.OpcUa.Api.Core.Models;
    using System.Runtime.Serialization;
    using System;
    using System.Collections.Generic;

    /// <summary>
    /// PubSub job description
    /// </summary>
    [DataContract]
    public class WriterGroupApiModel {

        /// <summary>
        /// Dataset writer group identifier
        /// </summary>
        [DataMember(Name = "writerGroupId", Order = 0)]
        public string WriterGroupId { get; set; }

        /// <summary>
        /// Network message types to generate (publisher extension)
        /// </summary>
        [DataMember(Name = "messageType", Order = 1,
            EmitDefaultValue = false)]
        public MessageEncoding? MessageType { get; set; }

        /// <summary>
        /// The data set writers generating dataset messages in the group
        /// </summary>
        [DataMember(Name = "dataSetWriters", Order = 2,
            EmitDefaultValue = false)]
        public List<DataSetWriterApiModel> DataSetWriters { get; set; }

        /// <summary>
        /// Network message configuration
        /// </summary>
        [DataMember(Name = "messageSettings", Order = 3,
            EmitDefaultValue = false)]
        public WriterGroupMessageSettingsApiModel MessageSettings { get; set; }

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
        public string Name { get; set; }

        /// <summary>
        /// Locales to use
        /// </summary>
        [DataMember(Name = "localeIds", Order = 6,
            EmitDefaultValue = false)]
        public List<string> LocaleIds { get; set; }

        /// <summary>
        /// Header layout uri
        /// </summary>
        [DataMember(Name = "headerLayoutUri", Order = 7,
            EmitDefaultValue = false)]
        public string HeaderLayoutUri { get; set; }

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
        public string SecurityGroupId { get; set; }

        /// <summary>
        /// Security key services to use
        /// </summary>
        [DataMember(Name = "securityKeyServices", Order = 10,
            EmitDefaultValue = false)]
        public List<ConnectionApiModel> SecurityKeyServices { get; set; }

        /// <summary>
        /// Max network message size
        /// </summary>
        [DataMember(Name = "maxNetworkMessageSize", Order = 11,
            EmitDefaultValue = false)]
        public uint? MaxNetworkMessageSize { get; set; }

        /// <summary>
        /// Publishing interval
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
    }
}
