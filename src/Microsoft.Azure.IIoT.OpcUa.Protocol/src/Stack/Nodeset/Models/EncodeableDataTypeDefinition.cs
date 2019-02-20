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
    using System.Linq;

    /// <summary>
    /// Extends a data type definition to provide missing info
    /// </summary>
    public class EncodeableDataTypeDefinition : IEncodeable {

        /// <summary>
        /// Encoded definition
        /// </summary>
        public UADataTypeDefinition Definition { get; private set; }

        /// <summary>
        /// Create definition
        /// </summary>
        /// <param name="definition"></param>
        public EncodeableDataTypeDefinition(UADataTypeDefinition definition) {
            Definition = definition;
        }

        /// <inheritdoc/>
        public ExpandedNodeId TypeId =>
            DataTypeIds.DataTypeDefinition;

        /// <inheritdoc/>
        public ExpandedNodeId BinaryEncodingId =>
            ObjectIds.ReferenceNode_Encoding_DefaultBinary;

        /// <inheritdoc/>
        public ExpandedNodeId XmlEncodingId =>
            ObjectIds.ReferenceNode_Encoding_DefaultXml;

        /// <inheritdoc/>
        public virtual void Encode(IEncoder encoder) {
            encoder.PushNamespace(Namespaces.OpcUaXsd);
            encoder.WriteQualifiedName(nameof(Definition.Name),
                Definition.Name);
            encoder.WriteString(nameof(Definition.SymbolicName),
                Definition.SymbolicName);
            encoder.WriteLocalizedText(nameof(Definition.Description),
                Definition.Description);
            encoder.WriteQualifiedName(nameof(Definition.BaseType),
                Definition.BaseType);
            encoder.PopNamespace();
        }

        /// <inheritdoc/>
        public virtual void Decode(IDecoder decoder) {
            decoder.PushNamespace(Namespaces.OpcUaXsd);
            Definition = new UADataTypeDefinition();
            Definition.Name =
                decoder.ReadQualifiedName(nameof(Definition.Name));
            Definition.SymbolicName =
                decoder.ReadString(nameof(Definition.SymbolicName));
            Definition.Description =
                decoder.ReadLocalizedText(nameof(Definition.Description));
            Definition.BaseType =
                decoder.ReadQualifiedName(nameof(Definition.BaseType));
            var fields = decoder.ReadEncodeableArray<EncodeableDataTypeField>(
                nameof(Definition.Fields));
            Definition.Fields = fields?.Select(f => f.Field).ToList();
            decoder.PopNamespace();
        }

        /// <inheritdoc/>
        public virtual bool IsEqual(IEncodeable encodeable) {
            if (ReferenceEquals(this, encodeable)) {
                return true;
            }
            if (!(encodeable is EncodeableDataTypeDefinition encodeableDefinition)) {
                return false;
            }
            if (!Utils.IsEqual(encodeableDefinition.Definition, Definition)) {
                return false;
            }
            return true;
        }
    }
}
