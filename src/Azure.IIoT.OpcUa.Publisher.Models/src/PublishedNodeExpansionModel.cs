// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Azure.IIoT.OpcUa.Publisher.Models
{
    using System.Runtime.Serialization;

    /// <summary>
    /// <para>
    /// Node expansion configuration. Configures how an entry should
    /// be expanded into configuration. If a node is an object it is
    /// expanded to all contained variables.
    /// </para>
    /// <para>
    /// If a node is an object type, all objects of that type are
    /// searched from the object root node. These found objects are
    /// then expanded into their variables.
    /// </para>
    /// <para>
    /// If the node is a variable, the variable is expanded to include
    /// all contained variables or properties. All entries will have
    /// the data set field id configured as data set class id.
    /// </para>
    /// <para>
    /// If a node is a variable type, then all variables of this type
    /// are found and added to a single writer entry. Note: That by
    /// themselves these variables are no further expanded.
    /// </para>
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
        /// Max number of levels to expand an instance node
        /// such as an object or variable into resulting
        /// variables.
        /// If the node is a variable instance to start with
        /// but the <see cref="ExcludeRootIfInstanceNode"/>
        /// property is set to excluded it, then setting this
        /// value to 0 is equivalent to a value of 1 to get
        /// the first level of variables contained in the
        /// variable, but not the variable itself. Otherwise
        /// only the variable itelf is returned. If the node
        /// is an object instance, 0 is equivalent to
        /// infinite and all levels are expanded.
        /// </summary>
        [DataMember(Name = "maxLevelsToExpand", Order = 3,
            EmitDefaultValue = false)]
        public uint MaxLevelsToExpand { get; init; }

        /// <summary>
        /// Do not consider subtypes of an object type when
        /// searching for instances of the type.
        /// </summary>
        [DataMember(Name = "noSubTypesOfTypeNodes", Order = 4,
            EmitDefaultValue = false)]
        public bool NoSubTypesOfTypeNodes { get; init; }

        /// <summary>
        /// If the node is an object or variable instance do
        /// not include it but only the instances underneath
        /// them.
        /// </summary>
        [DataMember(Name = "excludeRootIfInstanceNode", Order = 5,
            EmitDefaultValue = false)]
        public bool ExcludeRootIfInstanceNode { get; init; }

        /// <summary>
        /// Max browse depth for object search operation or
        /// when searching for an instance of a type.
        /// To only expand an object to its variables set
        /// this value to 0. The depth of expansion of a
        /// variable itself can be controlled via the
        /// <see cref="MaxLevelsToExpand"/>" property.
        /// If the root object is excluded a value of 0 is
        /// equivalent to a value of 1 to get the first level
        /// of objects contained in the object but not the
        /// object itself, e.g. a folder object.
        /// </summary>
        [DataMember(Name = "maxDepth", Order = 6,
            EmitDefaultValue = false)]
        public uint? MaxDepth { get; init; }

        /// <summary>
        /// If false, treats instance nodes found just like
        /// objects that need to be expanded. In case of a
        /// companion spec object type this could be set to
        /// true, flattening the structure into a single
        /// writer that represents the object in its entirety.
        /// However, when using generic interfaces that can
        /// be implemented across objects in the address
        /// space and only its variables are important, it
        /// might be useful to set this to false.
        /// </summary>
        [DataMember(Name = "flattenTypeInstance", Order = 7,
            EmitDefaultValue = false)]
        public bool FlattenTypeInstance { get; init; }

        /// <summary>
        /// Errors are silently discarded and only
        /// successfully expanded nodes are returned.
        /// </summary>
        [DataMember(Name = "discardErrors", Order = 8,
            EmitDefaultValue = false)]
        public bool DiscardErrors { get; init; }
    }
}
