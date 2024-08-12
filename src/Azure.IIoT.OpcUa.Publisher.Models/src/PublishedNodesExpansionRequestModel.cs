// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Azure.IIoT.OpcUa.Publisher.Models
{
    using System.Runtime.Serialization;

    /// <summary>
    /// Node expansion configuration. Configures how an entry
    /// should be expanded into configuration. If a node is
    /// an object it is expanded to all contained variables.
    /// If a node is an object type, all objects of that type
    /// are found and expanded. All entries will then have
    /// the data set field id configured as data set class id.
    /// </summary>
    [DataContract]
    public sealed record class PublishedNodeExpansionRequestModel
    {
        /// <summary>
        /// The entry to expand
        /// </summary>
        [DataMember(Name = "entry", Order = 0)]
        public required PublishedNodesEntryModel Entry { get; init; }

        /// <summary>
        /// Levels to expand. Default value is 1.
        /// 0 means infinite recursion.
        /// </summary>
        [DataMember(Name = "expand", Order = 1,
            EmitDefaultValue = false)]
        public uint? LevelsToExpand { get; init; }

        /// <summary>
        /// Do not consider subtypes of an object type
        /// when expanding a node object
        /// </summary>
        [DataMember(Name = "noSubtypes", Order = 3,
            EmitDefaultValue = false)]
        public bool NoSubtypes { get; init; }

        /// <summary>
        /// By default the api will create a new distinct
        /// writer per expanded object. Objects that cannot
        /// be expanded are part of the originally provided
        /// writer. The writer id is then post fixed with
        /// the data set field id of the object node field.
        /// If true, all variables of all expanded nodes are
        /// added to the originally provided entry.
        /// </summary>
        [DataMember(Name = "singleWriter", Order = 4,
            EmitDefaultValue = false)]
        public bool SingleWriter { get; init; }

        /// <summary>
        /// Optional request header
        /// </summary>
        [DataMember(Name = "header", Order = 5,
            EmitDefaultValue = false)]
        public RequestHeaderModel? Header { get; init; }
    }
}
