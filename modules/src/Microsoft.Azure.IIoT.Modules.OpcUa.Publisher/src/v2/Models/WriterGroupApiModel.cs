// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IIoT.Modules.OpcUa.Publisher.v2.Models {
    using Microsoft.Azure.IIoT.OpcUa.Publisher.Models;
    using Microsoft.Azure.IIoT.OpcUa.Registry.Models;
    using Newtonsoft.Json;
    using System;
    using System.Collections.Generic;
    using System.Linq;

    /// <summary>
    /// Pub/sub job description
    /// </summary>
    public class WriterGroupApiModel {

        /// <summary>
        /// Default constructor
        /// </summary>
        public WriterGroupApiModel() { }

        /// <summary>
        /// Create api model from service model
        /// </summary>
        /// <param name="model"></param>
        public WriterGroupApiModel(WriterGroupModel model) {
            if (model == null) {
                throw new ArgumentNullException(nameof(model));
            }
            WriterGroupId = model.WriterGroupId;
            HeaderLayoutUri = model.HeaderLayoutUri;
            KeepAliveTime = model.KeepAliveTime;
            LocaleIds = model.LocaleIds?.ToList();
            MaxNetworkMessageSize = model.MaxNetworkMessageSize;
            MessageSettings = model.MessageSettings == null ? null :
                new WriterGroupMessageSettingsApiModel(model.MessageSettings);
            MessageType = model.MessageType;
            Name = model.Name;
            Priority = model.Priority;
            SecurityGroupId = model.SecurityGroupId;
            SecurityKeyServices = model.SecurityKeyServices?
                .Select(s => new ConnectionApiModel(s))
                .ToList();
            DataSetWriters = model.DataSetWriters?
                .Select(s => new DataSetWriterApiModel(s))
                .ToList();
            PublishingInterval = model.PublishingInterval;
            SecurityMode = model.SecurityMode;
        }

        /// <summary>
        /// Create service model from api model
        /// </summary>
        public WriterGroupModel ToServiceModel() {
            return new WriterGroupModel {
                WriterGroupId = WriterGroupId,
                HeaderLayoutUri = HeaderLayoutUri,
                KeepAliveTime = KeepAliveTime,
                LocaleIds = LocaleIds?.ToList(),
                MaxNetworkMessageSize = MaxNetworkMessageSize,
                MessageSettings = MessageSettings?.ToServiceModel(),
                MessageType = MessageType,
                Name = Name,
                Priority = Priority,
                SecurityGroupId = SecurityGroupId,
                SecurityKeyServices = SecurityKeyServices?
                    .Select(s => s.ToServiceModel())
                    .ToList(),
                DataSetWriters = DataSetWriters?
                    .Select(s => s.ToServiceModel())
                    .ToList(),
                PublishingInterval = PublishingInterval,
                SecurityMode = SecurityMode
            };
        }

        /// <summary>
        /// Dataset writer group identifier
        /// </summary>
        [JsonProperty(PropertyName = "writerGroupId")]
        public string WriterGroupId { get; set; }

        /// <summary>
        /// Network message types to generate (publisher extension)
        /// </summary>
        [JsonProperty(PropertyName = "messageType",
            NullValueHandling = NullValueHandling.Ignore)]
        public MessageEncoding? MessageType { get; set; }

        /// <summary>
        /// The data set writers generating dataset messages in the group
        /// </summary>
        [JsonProperty(PropertyName = "dataSetWriters",
            NullValueHandling = NullValueHandling.Ignore)]
        public List<DataSetWriterApiModel> DataSetWriters { get; set; }

        /// <summary>
        /// Network message configuration
        /// </summary>
        [JsonProperty(PropertyName = "messageSettings",
            NullValueHandling = NullValueHandling.Ignore)]
        public WriterGroupMessageSettingsApiModel MessageSettings { get; set; }

        /// <summary>
        /// Priority of the writer group
        /// </summary>
        [JsonProperty(PropertyName = "priority",
            NullValueHandling = NullValueHandling.Ignore)]
        public byte? Priority { get; set; }

        /// <summary>
        /// Name of the writer group
        /// </summary>
        [JsonProperty(PropertyName = "name",
            NullValueHandling = NullValueHandling.Ignore)]
        public string Name { get; set; }

        /// <summary>
        /// Locales to use
        /// </summary>
        [JsonProperty(PropertyName = "localeIds",
            NullValueHandling = NullValueHandling.Ignore)]
        public List<string> LocaleIds { get; set; }

        /// <summary>
        /// Header layout uri
        /// </summary>
        [JsonProperty(PropertyName = "headerLayoutUri",
            NullValueHandling = NullValueHandling.Ignore)]
        public string HeaderLayoutUri { get; set; }

        /// <summary>
        /// Security mode
        /// </summary>
        [JsonProperty(PropertyName = "securityMode",
            NullValueHandling = NullValueHandling.Ignore)]
        public SecurityMode? SecurityMode { get; set; }

        /// <summary>
        /// Security group to use
        /// </summary>
        [JsonProperty(PropertyName = "securityGroupId",
            NullValueHandling = NullValueHandling.Ignore)]
        public string SecurityGroupId { get; set; }

        /// <summary>
        /// Security key services to use
        /// </summary>
        [JsonProperty(PropertyName = "securityKeyServices",
            NullValueHandling = NullValueHandling.Ignore)]
        public List<ConnectionApiModel> SecurityKeyServices { get; set; }

        /// <summary>
        /// Max network message size
        /// </summary>
        [JsonProperty(PropertyName = "maxNetworkMessageSize",
            NullValueHandling = NullValueHandling.Ignore)]
        public uint? MaxNetworkMessageSize { get; set; }

        /// <summary>
        /// Publishing interval
        /// </summary>
        [JsonProperty(PropertyName = "publishingInterval",
            NullValueHandling = NullValueHandling.Ignore)]
        public TimeSpan? PublishingInterval { get; set; }

        /// <summary>
        /// Keep alive time
        /// </summary>
        [JsonProperty(PropertyName = "keepAliveTime",
            NullValueHandling = NullValueHandling.Ignore)]
        public TimeSpan? KeepAliveTime { get; set; }
    }
}