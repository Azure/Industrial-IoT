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
    public sealed record class PublishedNodeExpansionModel
    {
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
        /// Max number of levels to expand the found objects
        /// or variables to. 0 expands all levels.
        /// </summary>
        [DataMember(Name = "maxLevelsToExpand", Order = 3,
            EmitDefaultValue = false)]
        public uint MaxLevelsToExpand { get; init; }

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
        /// Max browse depth for object search operation.
        /// To only expand an object to its variables set
        /// this value to 0. The depth of expansion can be
        /// controlled via the <see cref="MaxLevelsToExpand"/>"
        /// property. If the root object is excluded a value
        /// of 0 is equivalent to a value of 1 to get the
        /// first level of objects contained in the object
        /// but not the object itself, e.g. a folder object.
        /// </summary>
        [DataMember(Name = "maxDepth", Order = 6,
            EmitDefaultValue = false)]
        public uint? MaxDepth { get; init; }

        /// <summary>
        /// If the depth is not limited and the node is a
        /// type definition id set this flag to true to find
        /// only the first instance of this type from the
        /// object root.
        /// </summary>
        [DataMember(Name = "stopAtFirstFoundInstance", Order = 7,
            EmitDefaultValue = false)]
        public bool StopAtFirstFoundInstance { get; init; }

        /// <summary>
        /// Errors are silently discarded and only
        /// successfully expanded nodes are returned.
        /// </summary>
        [DataMember(Name = "discardErrors", Order = 8,
            EmitDefaultValue = false)]
        public bool DiscardErrors { get; init; }
    }
}
