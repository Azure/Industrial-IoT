// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Opc.Ua {
    using System.Collections.Generic;

    /// <summary>
    /// Represents id based access to node attributes
    /// </summary>
    public interface INodeAttributes : IEnumerable<KeyValuePair<uint, DataValue>> {

        /// <summary>
        /// Indexed access to values
        /// </summary>
        /// <param name="attribute"></param>
        /// <returns></returns>
        object this[uint attribute] { get; set; }

        /// <summary>
        /// Retrieve attribute from node or return a
        /// default as per node class
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="attribute"></param>
        /// <returns></returns>
        T GetAttribute<T>(uint attribute);

        /// <summary>
        /// Retrieve attribute from node or return a
        /// default as per node class
        /// </summary>
        /// <param name="attribute"></param>
        /// <returns></returns>
        DataValue GetAttribute(uint attribute);

        /// <summary>
        /// Set attribute
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="attribute"></param>
        /// <param name="value"></param>
        void SetAttribute<T>(uint attribute, T value);

        /// <summary>
        /// Set attribute
        /// </summary>
        /// <param name="attribute"></param>
        /// <param name="value"></param>
        void SetAttribute(uint attribute, DataValue value);

        /// <summary>
        /// Try get attribute or return false if attribute
        /// value does not exist.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="attribute"></param>
        /// <param name="value"></param>
        /// <returns></returns>
        bool TryGetAttribute<T>(uint attribute, out T value);

        /// <summary>
        /// Try get attribute as data value.
        /// </summary>
        /// <param name="attribute"></param>
        /// <param name="value"></param>
        /// <returns></returns>
        bool TryGetAttribute(uint attribute, out DataValue value);

        /// <summary>
        /// Returns the local node id
        /// </summary>
        NodeId LocalId { get; }

        /// <summary>
        /// Node class
        /// </summary>
        NodeClass NodeClass { get; set; }

        /// <summary>
        /// Browse name
        /// </summary>
        QualifiedName BrowseName { get; set; }

        /// <summary>
        /// Display name
        /// </summary>
        LocalizedText DisplayName { get; set; }

        /// <summary>
        /// Description if any
        /// </summary>
        LocalizedText Description { get; set; }

        /// <summary>
        /// Node access restrictions if any.
        /// </summary>
        ushort? AccessRestrictions { get; set; }

        /// <summary>
        /// Default write mask for the node
        /// </summary>
        uint? WriteMask { get; set; }

        /// <summary>
        /// User write mask for the node
        /// </summary>
        uint? UserWriteMask { get; set; }

        /// <summary>
        /// Whether type is abstract, if type can be abstract.
        /// Null if not type node.
        /// </summary>
        bool? IsAbstract { get; set; }

        /// <summary>
        /// Whether a view contains loops. Null if
        /// not a view.
        /// </summary>
        bool? ContainsNoLoops { get; set; }

        /// <summary>
        /// If object or view and eventing, event notifier to
        /// subscribe to.
        /// </summary>
        byte? EventNotifier { get; set; }

        /// <summary>
        /// If method node class, whether method can
        /// be called.
        /// </summary>
        bool? Executable { get; set; }

        /// <summary>
        /// If method node class, whether method can
        /// be called by current user.
        /// </summary>
        bool? UserExecutable { get; set; }

        /// <summary>
        /// Data type definition in case node is a data type node
        /// and definition is available,
        /// otherwise null.
        /// </summary>
        ExtensionObject DataTypeDefinition { get; set; }

        /// <summary>
        /// Default access level for variable node.
        /// </summary>
        byte? AccessLevel { get; set; }

        /// <summary>
        /// Extended access level for variable node.
        /// </summary>
        uint? AccessLevelEx { get; set; }

        /// <summary>
        /// User access level for variable node or null.
        /// </summary>
        byte? UserAccessLevel { get; set; }

        /// <summary>
        /// If variable the datatype of the variable.
        /// </summary>
        NodeId DataType { get; set; }

        /// <summary>
        /// Value rank of the variable data of a variable or variable
        /// type, otherwise null.
        /// </summary>
        int? ValueRank { get; set; }

        /// <summary>
        /// Array dimensions of variable or variable type.
        /// </summary>
        uint[] ArrayDimensions { get; set; }

        /// <summary>
        /// Whether the value of a variable is historizing.
        /// </summary>
        bool? Historizing { get; set; }

        /// <summary>
        /// Minimum sampling interval for the variable value,
        /// otherwise null if not a variable node.
        /// </summary>
        double? MinimumSamplingInterval { get; set; }

        /// <summary>
        /// Default value of the variable in case node is a variable
        /// type, otherwise null..
        /// </summary>
        Variant? Value { get; set; }

        /// <summary>
        /// Get data value
        /// </summary>
        DataValue DataValue { get; }

        /// <summary>
        /// Inverse name of the reference if the node is a reference
        /// type, otherwise null.
        /// </summary>
        LocalizedText InverseName { get; set; }

        /// <summary>
        /// Whether the reference is symmetric in case the node is
        /// a reference type, otherwise null.
        /// </summary>
        bool? Symmetric { get; set; }

        /// <summary>
        /// Role permissions
        /// </summary>
        IEnumerable<RolePermissionType> RolePermissions { get; set; }

        /// <summary>
        /// User role permissions
        /// </summary>
        IEnumerable<RolePermissionType> UserRolePermissions { get; set; }
    }
}
