// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Opc.Ua.Nodeset {
    using System;
    using System.Collections.Generic;
    using System.Runtime.Serialization;

    /// <summary>
    /// The base class for all variable type nodes.
    /// </summary>
    [DataContract(Name = "VariableType")]
    public abstract class VariableTypeNodeModel : TypeNodeModel {

        /// <summary>
        /// The value of the variable.
        /// </summary>
        [DataMember]
        public Variant? Value { get; set; }

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
        /// The number of dimensions for an array values with one or more fixed dimensions.
        /// </summary>
        [DataMember]
        public uint[] ArrayDimensions { get; set; }

        /// <summary>
        /// Initializes the instance with its defalt attribute values.
        /// </summary>
        protected VariableTypeNodeModel() :
            base(NodeClass.VariableType) {
        }

        /// <inheritdoc/>
        public override bool Equals(object obj) {
            if (!(obj is VariableTypeNodeModel model)) {
                return false;
            }
            if (!Utils.IsEqual(Value ?? Variant.Null, model.Value ?? Variant.Null)) {
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
            return base.Equals(obj);
        }

        /// <inheritdoc/>
        public override int GetHashCode() {
            var hashCode = base.GetHashCode();
            hashCode = (hashCode *
               -1521134295) + Value.GetHashSafe();
            hashCode = (hashCode *
                -1521134295) + DataType.GetHashSafe();
            hashCode = (hashCode *
                -1521134295) + ValueRank.GetHashSafe();
            hashCode = (hashCode *
                -1521134295) + ArrayDimensions.GetHashSafe();
            return hashCode;
        }
    }
}
