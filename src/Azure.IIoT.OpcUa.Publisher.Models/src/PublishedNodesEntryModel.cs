// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Azure.IIoT.OpcUa.Publisher.Models
{
    using System;
    using System.Collections.Generic;
    using System.Runtime.Serialization;

    /// <summary>
    /// Contains the nodes which should be published
    /// </summary>
    [DataContract]
    public sealed record class PublishedNodesEntryModel
    {
        /// <summary>
        /// Version number of the entry
        /// </summary>
        [DataMember(Name = "Version", Order = 0,
            EmitDefaultValue = false)]
        public int? Version { get; set; }

        /// <summary>
        /// Last change to the entry
        /// </summary>
        [DataMember(Name = "LastChangeTimespan", Order = 1,
            EmitDefaultValue = false)]
        public DateTime? LastChangeTimespan { get; set; }

        /// <summary>
        /// Name of the data set writer.
        /// </summary>
        [DataMember(Name = "DataSetWriterId", Order = 2,
            EmitDefaultValue = false)]
        public string? DataSetWriterId { get; set; }

        /// <summary>
        /// The Group the writer belongs to.
        /// </summary>
        [DataMember(Name = "DataSetWriterGroup", Order = 3,
            EmitDefaultValue = false)]
        public string? DataSetWriterGroup { get; set; }

        /// <summary>
        /// Nodes defined in the collection.
        /// </summary>
        [DataMember(Name = "OpcNodes", Order = 4,
            EmitDefaultValue = false)]
        public List<OpcNodeModel>? OpcNodes { get; set; }

        /// <summary>
        /// A dataset class id.
        /// </summary>
        [DataMember(Name = "DataSetClassId", Order = 5,
            EmitDefaultValue = false)]
        public Guid DataSetClassId { get; set; }

        /// <summary>
        /// The optional short name of the dataset.
        /// </summary>
        [DataMember(Name = "DataSetName", Order = 6,
            EmitDefaultValue = false)]
        public string? DataSetName { get; set; }

        /// <summary>
        /// The Publishing interval for a dataset writer
        /// in miliseconds.
        /// </summary>
        [DataMember(Name = "DataSetPublishingInterval", Order = 7,
            EmitDefaultValue = false)]
        public int? DataSetPublishingInterval { get; set; }

        /// <summary>
        /// The Publishing interval for a dataset writer
        /// in timespan format. Takes precedence over
        /// <see cref="DataSetPublishingInterval"/> if defined.
        /// </summary>
        [DataMember(Name = "DataSetPublishingIntervalTimespan", Order = 8,
            EmitDefaultValue = false)]
        public TimeSpan? DataSetPublishingIntervalTimespan { get; set; }

        /// <summary>
        /// Insert a key frame every x messages
        /// </summary>
        [DataMember(Name = "DataSetKeyFrameCount", Order = 9,
            EmitDefaultValue = false)]
        public uint? DataSetKeyFrameCount { get; set; }

        /// <summary>
        /// Send metadata at the configured interval
        /// even when not changing expressed in milliseconds.
        /// </summary>
        [DataMember(Name = "MetaDataUpdateTime", Order = 10,
            EmitDefaultValue = false)]
        public int? MetaDataUpdateTime { get; set; }

        /// <summary>
        /// Send metadata at the configured interval even when not
        /// changing expressed as duration. Takes precedence over
        /// <see cref="MetaDataUpdateTime"/>if defined.
        /// </summary>
        [DataMember(Name = "MetaDataUpdateTimeTimespan", Order = 11,
            EmitDefaultValue = false)]
        public TimeSpan? MetaDataUpdateTimeTimespan { get; set; }

        /// <summary>
        /// The endpoint URL of the OPC UA server.
        /// </summary>
        [DataMember(Name = "EndpointUrl", Order = 13,
            EmitDefaultValue = false)]
        public string? EndpointUrl { get; set; }

        /// <summary>
        /// The optional description of the dataset.
        /// </summary>
        [DataMember(Name = "DataSetDescription", Order = 15,
            EmitDefaultValue = false)]
        public string? DataSetDescription { get; set; }

        /// <summary>
        /// Secure transport should be used to connect to
        /// the opc server.
        /// </summary>
        [DataMember(Name = "UseSecurity", Order = 30)]
        public bool UseSecurity { get; set; }

        /// <summary>
        /// authentication mode
        /// </summary>
        [DataMember(Name = "OpcAuthenticationMode", Order = 31)]
        public OpcAuthenticationMode OpcAuthenticationMode { get; set; }

        /// <summary>
        /// encrypted username
        /// </summary>
        [DataMember(Name = "EncryptedAuthUsername", Order = 32,
            EmitDefaultValue = false)]
        public string? EncryptedAuthUsername { get; set; }

        /// <summary>
        /// encrypted password
        /// </summary>
        [DataMember(Name = "EncryptedAuthPassword", Order = 33,
            EmitDefaultValue = false)]
        public string? EncryptedAuthPassword { get; set; }

        /// <summary>
        /// plain username
        /// </summary>
        [DataMember(Name = "OpcAuthenticationUsername", Order = 34,
            EmitDefaultValue = false)]
        public string? OpcAuthenticationUsername { get; set; }

        /// <summary>
        /// plain password
        /// </summary>
        [DataMember(Name = "OpcAuthenticationPassword", Order = 35,
            EmitDefaultValue = false)]
        public string? OpcAuthenticationPassword { get; set; }

        /// <summary>
        /// The node to monitor in "ns=" syntax.
        /// </summary>
        [DataMember(Name = "NodeId", Order = 40,
            EmitDefaultValue = false)]
        public NodeIdModel? NodeId { get; set; }
    }
}
