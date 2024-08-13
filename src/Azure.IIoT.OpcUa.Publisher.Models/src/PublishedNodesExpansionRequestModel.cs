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
        /// Optional request header to use for all operations
        /// against the server.
        /// </summary>
        [DataMember(Name = "header", Order = 1,
            EmitDefaultValue = false)]
        public RequestHeaderModel? Header { get; init; }

        /// <summary>
        /// By default the api will create a new distinct
        /// writer per expanded object. Objects that cannot
        /// be expanded are part of the originally provided
        /// writer. The writer id is then post fixed with
        /// the data set field id of the object node field.
        /// If true, all variables of all expanded nodes are
        /// added to the originally provided entry.
        /// </summary>
        [DataMember(Name = "createSingleWriter", Order = 2,
            EmitDefaultValue = false)]
        public bool CreateSingleWriter { get; init; }

        /// <summary>
        /// Do not consider subtypes of an object type
        /// when expanding a node object
        /// </summary>
        [DataMember(Name = "noSubtypes", Order = 4,
            EmitDefaultValue = false)]
        public bool NoSubtypes { get; init; }

        /// <summary>
        /// If the node is an object do not include it
        /// but only the objects underneath it.
        /// </summary>
        [DataMember(Name = "excludeRootObject", Order = 5,
            EmitDefaultValue = false)]
        public bool ExcludeRootObject { get; init; }

        /// <summary>
        /// If the depth is not limited and the node is an
        /// object type set this flag to true to find only
        /// the first object from the object root.
        /// is expanded.
        /// </summary>
        [DataMember(Name = "stopAtFirstFoundObject", Order = 7,
            EmitDefaultValue = false)]
        public bool StopAtFirstFoundObject { get; init; }

        /// <summary>
        /// Errors are silently discarded and only
        /// successfully expanded nodes are returned.
        /// </summary>
        [DataMember(Name = "discardErrors", Order = 8,
            EmitDefaultValue = false)]
        public bool DiscardErrors { get; init; }
    }
}
