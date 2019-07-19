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

namespace Opc.Ua.Design.Schema {
    using Opc.Ua.Types;
    using Opc.Ua.Types.Schema;
    using System.Linq;
    using System.Xml;

    /// <summary>
    /// Data type design extensions
    /// </summary>
    public static class DataTypeDesignEx {

        /// <summary>
        /// Import fields
        /// </summary>
        /// <param name="design"></param>
        /// <param name="simpleType"></param>
        public static void SetDataType(this DataTypeDesign design, TypeDeclaration simpleType) {
            if (simpleType == null) {
                return;
            }
            if (simpleType.SourceType != null) {
                design.BaseType = simpleType.SourceType.ToDesignBaseDataType();
            }
        }

        /// <summary>
        /// Import fields
        /// </summary>
        /// <param name="design"></param>
        /// <param name="complexType"></param>
        public static void SetDataType(this DataTypeDesign design, ComplexType complexType) {
            if (complexType == null) {
                return;
            }
            if (complexType.BaseType != null) {
                design.BaseType = complexType.BaseType.ToDesignBaseDataType();
            }
            else {
                design.BaseType = Constants.Structure;
            }
            design.IsAbstract = complexType.IsAbstract;

            if (complexType.Field != null && complexType.Field.Length > 0) {
                design.Fields = complexType.Field.Select(v => v.ToParameter()).ToArray();
            }
        }

        /// <summary>
        /// Set enumerated type fields
        /// </summary>
        /// <param name="design"></param>
        /// <param name="enumeratedType"></param>
        public static void SetDataType(this DataTypeDesign design, EnumeratedType enumeratedType) {
            if (enumeratedType == null) {
                return;
            }
            if (enumeratedType.IsOptionSet) {
                design.IsOptionSet = true;
                if (enumeratedType.BaseType != null) {
                    design.BaseType = enumeratedType.BaseType.ToDesignBaseDataType();
                }
                else {
                    design.BaseType = Constants.UInt32;
                }
            }
            else {
                design.BaseType = Constants.Enumeration;
            }
            if (enumeratedType.Value != null && enumeratedType.Value.Length > 0) {
                design.Fields = enumeratedType.Value.Select(v => v.ToParameter()).ToArray();
            }
        }

        /// <summary>
        /// Convert type declartion type to design type
        /// </summary>
        /// <param name="baseType"></param>
        /// <returns></returns>
        private static XmlQualifiedName ToDesignBaseDataType(this XmlQualifiedName baseType) {
            if (baseType.IsNullOrEmpty()) {
                return null;
            }
            switch (baseType.Name) {
                case "ExtensionObject":
                    return Constants.Structure;
                case "Variant":
                    return Constants.BaseDataType;
            }
            return new XmlQualifiedName(baseType.Name, Namespaces.OpcUa);
        }

        /// <summary>
        /// Convert field to design parameter
        /// </summary>
        /// <param name="field"></param>
        /// <returns></returns>
        private static Parameter ToParameter(this FieldType field) {
            if (field == null) {
                return null;
            }
            var parameter = new Parameter {
                Name = field.Name,
                Description = field.Documentation.ToLocalizedText()
            };
            if (field.DataType != null) {
                parameter.DataType = field.DataType.ToDesignBaseDataType();
            }
            else {
                parameter.DataType = Constants.BaseDataType;
            }
            if (field.ValueRank == 0) {
                parameter.ValueRank = ValueRank.Array;
            }
            else {
                parameter.ValueRank = ValueRank.Scalar;
            }
            return parameter;
        }

        /// <summary>
        /// Convert enumerated value to design parameter
        /// </summary>
        /// <param name="value"></param>
        /// <returns></returns>
        private static Parameter ToParameter(this EnumeratedValue value) {
            if (value == null) {
                return null;
            }
            var parameter = new Parameter {
                Name = value.Name,
                Description = value.Documentation.ToLocalizedText()
            };
            if (value.ValueSpecified) {
                parameter.Identifier = value.Value;
                parameter.IdentifierSpecified = true;
            }
            if (!string.IsNullOrEmpty(value.BitMask)) {
                parameter.BitMask = value.BitMask;
                parameter.IdentifierSpecified = false;
            }
            return parameter;
        }
    }
}

