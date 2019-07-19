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

namespace Opc.Ua.Models {

    /// <summary>
    /// The set of basic data types
    /// </summary>
    public enum BasicDataType {
        /// <remarks/>
        Boolean = BuiltInType.Boolean,
        /// <remarks/>
        SByte = BuiltInType.SByte,
        /// <remarks/>
        Byte = BuiltInType.Byte,
        /// <remarks/>
        Int16 = BuiltInType.Int16,
        /// <remarks/>
        UInt16 = BuiltInType.UInt16,
        /// <remarks/>
        Int32 = BuiltInType.Int32,
        /// <remarks/>
        UInt32 = BuiltInType.UInt32,
        /// <remarks/>
        Int64 = BuiltInType.Int64,
        /// <remarks/>
        UInt64 = BuiltInType.UInt64,
        /// <remarks/>
        Float = BuiltInType.Float,
        /// <remarks/>
        Double = BuiltInType.Double,
        /// <remarks/>
        String = BuiltInType.String,
        /// <remarks/>
        DateTime = BuiltInType.DateTime,
        /// <remarks/>
        Guid = BuiltInType.Guid,
        /// <remarks/>
        ByteString = BuiltInType.ByteString,
        /// <remarks/>
        XmlElement = BuiltInType.XmlElement,
        /// <remarks/>
        NodeId = BuiltInType.NodeId,
        /// <remarks/>
        ExpandedNodeId = BuiltInType.ExpandedNodeId,
        /// <remarks/>
        StatusCode = BuiltInType.StatusCode,
        /// <remarks/>
        DiagnosticInfo = BuiltInType.DiagnosticInfo,
        /// <remarks/>
        QualifiedName = BuiltInType.QualifiedName,
        /// <remarks/>
        LocalizedText = BuiltInType.LocalizedText,
        /// <remarks/>
        DataValue = BuiltInType.DataValue,
        /// <remarks/>
        Number = BuiltInType.Number,
        /// <remarks/>
        Integer = BuiltInType.Integer,
        /// <remarks/>
        UInteger = BuiltInType.UInteger,
        /// <remarks/>
        Enumeration = BuiltInType.Enumeration,
        /// <remarks/>
        Structure = BuiltInType.ExtensionObject,
        /// <remarks/>
        BaseDataType = BuiltInType.Variant,
        /// <remarks/>
        UserDefined = -1
    }
}
