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
    /// Pub/sub job description
    /// </summary>
    [DataContract]
    public class WriterGroupApiModel {

        /// <summary>
        /// Dataset writer group identifier
        /// </summary>
        [DataMember(Name = "writerGroupId")]
        public string WriterGroupId { get; set; }

        /// <summary>
        /// Network message types to generate (publisher extension)
        /// </summary>
        [DataMember(Name = "messageType",
            EmitDefaultValue = false)]
        public MessageEncoding? MessageType { get; set; }

        /// <summary>
        /// The data set writers generating dataset messages in the group
        /// </summary>
        [DataMember(Name = "dataSetWriters",
            EmitDefaultValue = false)]
        public List<DataSetWriterApiModel> DataSetWriters { get; set; }

        /// <summary>
        /// Network message configuration
        /// </summary>
        [DataMember(Name = "messageSettings",
            EmitDefaultValue = false)]
        public WriterGroupMessageSettingsApiModel MessageSettings { get; set; }

        /// <summary>
        /// Priority of the writer group
        /// </summary>
        [DataMember(Name = "priority",
            EmitDefaultValue = false)]
        public byte? Priority { get; set; }

        /// <summary>
        /// Name of the writer group
        /// </summary>
        [DataMember(Name = "name",
            EmitDefaultValue = false)]
        public string Name { get; set; }

        /// <summary>
        /// Locales to use
        /// </summary>
        [DataMember(Name = "localeIds",
            EmitDefaultValue = false)]
        public List<string> LocaleIds { get; set; }

        /// <summary>
        /// Header layout uri
        /// </summary>
        [DataMember(Name = "headerLayoutUri",
            EmitDefaultValue = false)]
        public string HeaderLayoutUri { get; set; }

        /// <summary>
        /// Security mode
        /// </summary>
        [DataMember(Name = "securityMode",
            EmitDefaultValue = false)]
        public SecurityMode? SecurityMode { get; set; }

        /// <summary>
        /// Security group to use
        /// </summary>
        [DataMember(Name = "securityGroupId",
            EmitDefaultValue = false)]
        public string SecurityGroupId { get; set; }

        /// <summary>
        /// Security key services to use
        /// </summary>
        [DataMember(Name = "securityKeyServices",
            EmitDefaultValue = false)]
        public List<ConnectionApiModel> SecurityKeyServices { get; set; }

        /// <summary>
        /// Max network message size
        /// </summary>
        [DataMember(Name = "maxNetworkMessageSize",
            EmitDefaultValue = false)]
        public uint? MaxNetworkMessageSize { get; set; }

        /// <summary>
        /// Publishing interval
        /// </summary>
        [DataMember(Name = "publishingInterval",
            EmitDefaultValue = false)]
        public TimeSpan? PublishingInterval { get; set; }

        /// <summary>
        /// Keep alive time
        /// </summary>
        [DataMember(Name = "keepAliveTime",
            EmitDefaultValue = false)]
        public TimeSpan? KeepAliveTime { get; set; }
    }
}