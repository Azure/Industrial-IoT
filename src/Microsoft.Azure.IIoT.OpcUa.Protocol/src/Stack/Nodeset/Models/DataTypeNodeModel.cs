// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Opc.Ua.Nodeset {
    using System.Runtime.Serialization;

    /// <summary>
    /// Data type nodes.
    /// </summary>
    [DataContract(Name = "DataType")]
    public class DataTypeNodeModel : TypeNodeModel {

        /// <summary>
        /// Create data type state.
        /// </summary>
        public DataTypeNodeModel() :
            base(NodeClass.DataType) {
        }

        /// <summary>
        /// The definition of the data type
        /// </summary>
        [DataMember]
        public DataTypeDefinition Definition { get; set; }

        /// <summary>
        /// The purpose of the data type.
        /// </summary>
        [DataMember]
        public Schema.DataTypePurpose Purpose { get; set; }

        /// <inheritdoc/>
        public override bool Equals(object obj) {
            if (!(obj is DataTypeNodeModel model)) {
                return false;
            }
            if (!Utils.IsEqual(Definition, model.Definition)) {
                return false;
            }
            if (Purpose != model.Purpose) {
                return false;
            }
            return base.Equals(obj);
        }

        /// <inheritdoc/>
        public override int GetHashCode() {
            var hashCode = base.GetHashCode();
            // TODO
            // hashCode = hashCode *
            //     -1521134295 + Definition.GetHashSafe();
            hashCode = (hashCode *
                -1521134295) + Purpose.GetHashCode();
            return hashCode;
        }
    }
}
