// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Opc.Ua.Nodeset {
    using System;
    using System.Collections.Generic;
    using System.Runtime.Serialization;

    /// <summary>
    /// The base class for all variable nodes.
    /// </summary>
    [DataContract(Name = "Variable")]
    public abstract class VariableNodeModel : InstanceNodeModel {

        /// <summary>
        /// The value of the variable.
        /// </summary>
        [DataMember]
        public Variant? Value { get; set; }

        /// <summary>
        /// The timestamp associated with the variable value.
        /// </summary>
        [DataMember]
        public DateTime? Timestamp { get; set; }

        /// <summary>
        /// The status code associated with the variable value.
        /// </summary>
        [DataMember]
        public StatusCode? StatusCode { get; set; }

        /// <summary>
        /// The data type for the variable value.
        /// </summary>
        [DataMember]
        public NodeId DataType { get; set; }

        /// <summary>
        /// The number of array dimensions permitted for the variable value.
        /// </summary>
        [DataMember]
        public int? ValueRank { get; set; }

        /// <summary>
        /// The number of dimensions for an array values
        /// with one or more fixed dimensions.
        /// </summary>
        [DataMember]
        public uint[] ArrayDimensions { get; set; }

        /// <summary>
        /// Extended access level
        /// </summary>
        [DataMember]
        public uint? AccessLevelEx { get; set; }

        /// <summary>
        /// The type of access available for the variable.
        /// </summary>
        [DataMember]
        public byte? AccessLevel { get; set; }

        /// <summary>
        /// The type of access granted to the current user.
        /// </summary>
        [DataMember]
        public byte? UserAccessLevel { get; set; }

        /// <summary>
        /// The minimum sampling interval supported by the variable.
        /// </summary>
        [DataMember]
        public double? MinimumSamplingInterval { get; set; }

        /// <summary>
        /// Whether the server is archiving the value of the variable.
        /// </summary>
        [DataMember]
        public bool? Historizing { get; set; }

        /// <summary>
        /// Whether the value can be set to null.
        /// </summary>
        public bool IsValueType { get; set; }

        /// <summary>
        /// Create variable node
        /// </summary>
        /// <param name="parent"></param>
        protected VariableNodeModel(BaseNodeModel parent = null) :
            base(NodeClass.Variable, parent) {
        }

        /// <inheritdoc/>
        public override bool Equals(object obj) {
            if (!(obj is VariableNodeModel model)) {
                return false;
            }
            if (IsValueType != model.IsValueType) {
                return false;
            }
            if (!Utils.IsEqual(Value ?? Variant.Null, model.Value ?? Variant.Null)) {
                return false;
            }
            if (Timestamp != model.Timestamp) {
                return false;
            }
            if (StatusCode != model.StatusCode) {
                return false;
            }
            if (!Utils.IsEqual(DataType, model.DataType)) { // TODO
                return false;
            }
            if (ValueRank != model.ValueRank) {
                return false;
            }
            if (ArrayDimensions.SequenceEqualsSafe(model.ArrayDimensions)) {
                return false;
            }
            if (AccessLevelEx != model.AccessLevelEx) {
                return false;
            }
            if (AccessLevel != model.AccessLevel) {
                return false;
            }
            if (UserAccessLevel != model.UserAccessLevel) {
                return false;
            }
            if (MinimumSamplingInterval.EqualsSafe(model.MinimumSamplingInterval)) {
                return false;
            }
            if (Historizing != model.Historizing) {
                return false;
            }
            return base.Equals(obj);
        }

        /// <inheritdoc/>
        public override int GetHashCode() {
            var hashCode = base.GetHashCode();
            hashCode = (hashCode *
                -1521134295) + IsValueType.GetHashCode();
            hashCode = (hashCode *
                -1521134295) + Value.GetHashSafe();
            hashCode = (hashCode *
                -1521134295) + Timestamp.GetHashSafe();
            hashCode = (hashCode *
                -1521134295) + StatusCode.GetHashSafe();
            hashCode = (hashCode *
                -1521134295) + DataType.GetHashSafe();
            hashCode = (hashCode *
                -1521134295) + ValueRank.GetHashSafe();
            hashCode = (hashCode *
                -1521134295) + ArrayDimensions.GetHashSafe();
            hashCode = (hashCode *
                -1521134295) + AccessLevelEx.GetHashSafe();
            hashCode = (hashCode *
                -1521134295) + AccessLevel.GetHashSafe();
            hashCode = (hashCode *
                -1521134295) + UserAccessLevel.GetHashSafe();
            hashCode = (hashCode *
                -1521134295) + MinimumSamplingInterval.GetHashSafe();
            hashCode = (hashCode *
                -1521134295) + Historizing.GetHashSafe();
            return hashCode;
        }
    }
}
