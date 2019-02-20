/* ========================================================================
 * Copyright (c) 2005-2016 The OPC Foundation, Inc. All rights reserved.
 *
 * OPC Foundation MIT License 1.00
 *
 * Permission is hereby granted, free of charge, to any person
 * obtaining a copy of this software and associated documentation
 * files (the "Software"), to deal in the Software without
 * restriction, including without limitation the rights to use,
 * copy, modify, merge, publish, distribute, sublicense, and/or sell
 * copies of the Software, and to permit persons to whom the
 * Software is furnished to do so, subject to the following
 * conditions:
 *
 * The above copyright notice and this permission notice shall be
 * included in all copies or substantial portions of the Software.
 * THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND,
 * EXPRESS OR IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES
 * OF MERCHANTABILITY, FITNESS FOR A PARTICULAR PURPOSE AND
 * NONINFRINGEMENT. IN NO EVENT SHALL THE AUTHORS OR COPYRIGHT
 * HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY,
 * WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING
 * FROM, OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR
 * OTHER DEALINGS IN THE SOFTWARE.
 *
 * The complete license agreement can be found here:
 * http://opcfoundation.org/License/MIT/1.00/
 * ======================================================================*/

namespace Opc.Ua.Nodeset {
    using Opc.Ua.Extensions;

    /// <summary>
    /// Encodes legafy type fields
    /// </summary>
    public class EncodeableDataTypeField : IEncodeable {

        /// <summary>
        /// Encoded field
        /// </summary>
        public DataTypeDefinitionField Field { get; private set; }

        /// <summary>
        /// Create field
        /// </summary>
        /// <param name="field"></param>
        public EncodeableDataTypeField(DataTypeDefinitionField field) {
            Field = field;
        }

        /// <inheritdoc/>
        public ExpandedNodeId TypeId =>
            DataTypeIds.StructureField;

        /// <inheritdoc/>
        public ExpandedNodeId BinaryEncodingId =>
            ObjectIds.ReferenceNode_Encoding_DefaultBinary;

        /// <inheritdoc/>
        public ExpandedNodeId XmlEncodingId =>
            ObjectIds.ReferenceNode_Encoding_DefaultXml;

        /// <inheritdoc/>
        public virtual void Encode(IEncoder encoder) {
            encoder.PushNamespace(Namespaces.OpcUaXsd);
            encoder.WriteString(nameof(Field.Name),
                Field.Name);
            encoder.WriteString(nameof(Field.SymbolicName),
                Field.SymbolicName);
            encoder.WriteLocalizedText(nameof(Field.Description),
                Field.Description);
            encoder.WriteNodeId(nameof(Field.DataType),
                Field.DataType);
            encoder.WriteInt32(nameof(Field.ValueRank),
                Field.ValueRank);
            encoder.WriteEncodeable(nameof(Field.Definition),
                new EncodeableDataTypeDefinition(Field.Definition));
            encoder.WriteInt32(nameof(Field.Value),
                Field.Value);
            encoder.PopNamespace();
        }

        /// <inheritdoc/>
        public virtual void Decode(IDecoder decoder) {
            decoder.PushNamespace(Namespaces.OpcUaXsd);
            Field = new DataTypeDefinitionField();
            Field.Name =
                decoder.ReadString(nameof(Field.Name));
            Field.SymbolicName =
                decoder.ReadString(nameof(Field.SymbolicName));
            Field.Description =
                decoder.ReadLocalizedText(nameof(Field.Description));
            Field.DataType =
                decoder.ReadNodeId(nameof(Field.DataType));
            Field.ValueRank =
                decoder.ReadInt32(nameof(Field.ValueRank));
            Field.Definition =
                decoder.ReadEncodeable<EncodeableDataTypeDefinition>(
                    nameof(Field.Definition))?.Definition;
            Field.Value =
                decoder.ReadInt32(nameof(Field.Value));
            decoder.PopNamespace();
        }

        /// <inheritdoc/>
        public virtual bool IsEqual(IEncodeable encodeable) {
            if (ReferenceEquals(this, encodeable)) {
                return true;
            }
            if (!(encodeable is EncodeableDataTypeField encodeableReference)) {
                return false;
            }
            if (!Utils.IsEqual(encodeableReference.Field, Field)) {
                return false;
            }
            return true;
        }
    }
}

