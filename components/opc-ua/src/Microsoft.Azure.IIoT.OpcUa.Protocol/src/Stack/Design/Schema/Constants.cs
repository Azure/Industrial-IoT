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
    using System.Xml;

    /// <summary>
    /// Xml constants
    /// </summary>
    public static class Constants {

        /// <summary>
        /// References
        /// </summary>
        public static readonly XmlQualifiedName References =
            new XmlQualifiedName("References", Namespaces.OpcUa);

        /// <summary>
        /// NonHierarchicalReferences
        /// </summary>
        public static readonly XmlQualifiedName NonHierarchicalReferences =
            new XmlQualifiedName("NonHierarchicalReferences", Namespaces.OpcUa);

        /// <summary>
        /// Has property
        /// </summary>
        public static readonly XmlQualifiedName HasProperty =
            new XmlQualifiedName("HasProperty", Namespaces.OpcUa);

        /// <summary>
        /// Organizes
        /// </summary>
        public static readonly XmlQualifiedName Organizes =
            new XmlQualifiedName("Organizes", Namespaces.OpcUa);

        /// <summary>
        /// HasComponent
        /// </summary>
        public static readonly XmlQualifiedName HasComponent =
            new XmlQualifiedName("HasComponent", Namespaces.OpcUa);

        /// <summary>
        /// HasModelParent
        /// </summary>
        public static readonly XmlQualifiedName HasModelParent =
            new XmlQualifiedName("HasModelParent", Namespaces.OpcUa);

        /// <summary>
        /// HasDescription
        /// </summary>
        public static readonly XmlQualifiedName HasDescription =
            new XmlQualifiedName("HasDescription", Namespaces.OpcUa);

        /// <summary>
        /// HasEncoding
        /// </summary>
        public static readonly XmlQualifiedName HasEncoding =
            new XmlQualifiedName("HasEncoding", Namespaces.OpcUa);

        /// <summary>
        /// PropertyType
        /// </summary>
        public static readonly XmlQualifiedName PropertyType =
            new XmlQualifiedName("PropertyType", Namespaces.OpcUa);

        /// <summary>
        /// BaseDataVariableType
        /// </summary>
        public static readonly XmlQualifiedName BaseDataVariableType =
            new XmlQualifiedName("BaseDataVariableType", Namespaces.OpcUa);

        /// <summary>
        /// BaseObjectType
        /// </summary>
        public static readonly XmlQualifiedName BaseObjectType =
            new XmlQualifiedName("BaseObjectType", Namespaces.OpcUa);

        /// <summary>
        /// BaseEventType
        /// </summary>
        public static readonly XmlQualifiedName BaseEventType =
            new XmlQualifiedName("BaseEventType", Namespaces.OpcUa);

        /// <summary>
        /// FolderType
        /// </summary>
        public static readonly XmlQualifiedName FolderType =
            new XmlQualifiedName("FolderType", Namespaces.OpcUa);

        /// <summary>
        /// EventTypesFolder
        /// </summary>
        public static readonly XmlQualifiedName EventTypesFolder =
            new XmlQualifiedName("EventTypesFolder", Namespaces.OpcUa);

        /// <summary>
        /// BaseDataType
        /// </summary>
        public static readonly XmlQualifiedName BaseDataType =
            new XmlQualifiedName("BaseDataType", Namespaces.OpcUa);

        /// <summary>
        /// BaseVariableType
        /// </summary>
        public static readonly XmlQualifiedName BaseVariableType =
            new XmlQualifiedName("BaseVariableType", Namespaces.OpcUa);

        /// <summary>
        /// DataTypeEncodingType
        /// </summary>
        public static readonly XmlQualifiedName DataTypeEncodingType =
            new XmlQualifiedName("DataTypeEncodingType", Namespaces.OpcUa);

        /// <summary>
        /// DataTypeDictionaryType
        /// </summary>
        public static readonly XmlQualifiedName DataTypeDictionaryType =
            new XmlQualifiedName("DataTypeDictionaryType", Namespaces.OpcUa);

        /// <summary>
        /// DataTypeDescriptionType
        /// </summary>
        public static readonly XmlQualifiedName DataTypeDescriptionType =
            new XmlQualifiedName("DataTypeDescriptionType", Namespaces.OpcUa);

        /// <summary>
        /// Argument
        /// </summary>
        public static readonly XmlQualifiedName Argument =
            new XmlQualifiedName("Argument", Namespaces.OpcUa);

        /// <summary>
        /// DefaultXml
        /// </summary>
        public static readonly XmlQualifiedName DefaultXml =
            new XmlQualifiedName("DefaultXml", Namespaces.OpcUa);

        /// <summary>
        /// DefaultBinary
        /// </summary>
        public static readonly XmlQualifiedName DefaultBinary =
            new XmlQualifiedName("DefaultBinary", Namespaces.OpcUa);

        /// <summary>
        /// DefaultJson
        /// </summary>
        public static readonly XmlQualifiedName DefaultJson =
            new XmlQualifiedName("DefaultJson", Namespaces.OpcUa);

        /// <summary>
        /// InputArguments
        /// </summary>
        public static readonly XmlQualifiedName InputArguments =
            new XmlQualifiedName("InputArguments", Namespaces.OpcUa);

        /// <summary>
        /// OutputArguments
        /// </summary>
        public static readonly XmlQualifiedName OutputArguments =
            new XmlQualifiedName("OutputArguments", Namespaces.OpcUa);

        /// <summary>
        /// NamespaceUri
        /// </summary>
        public static readonly XmlQualifiedName NamespaceUri =
            new XmlQualifiedName("NamespaceUri", Namespaces.OpcUa);

        /// <summary>
        /// Deprecated
        /// </summary>
        public static readonly XmlQualifiedName Deprecated =
            new XmlQualifiedName("Deprecated", Namespaces.OpcUa);

        /// <summary>
        /// OptionSetValues
        /// </summary>
        public static readonly XmlQualifiedName OptionSetValues =
            new XmlQualifiedName("OptionSetValues", Namespaces.OpcUa);

        /// <summary>
        /// ByteString
        /// </summary>
        public static readonly XmlQualifiedName ByteString =
            new XmlQualifiedName("ByteString", Namespaces.OpcUa);

        /// <summary>
        /// LocalizedText
        /// </summary>
        public static readonly XmlQualifiedName LocalizedText =
            new XmlQualifiedName("LocalizedText", Namespaces.OpcUa);

        /// <summary>
        /// Boolean
        /// </summary>
        public static readonly XmlQualifiedName Boolean =
            new XmlQualifiedName("Boolean", Namespaces.OpcUa);

        /// <summary>
        /// String
        /// </summary>
        public static readonly XmlQualifiedName String =
            new XmlQualifiedName("String", Namespaces.OpcUa);

        /// <summary>
        /// EnumStrings
        /// </summary>
        public static readonly XmlQualifiedName EnumStrings =
            new XmlQualifiedName("EnumStrings", Namespaces.OpcUa);

        /// <summary>
        /// EnumValues
        /// </summary>
        public static readonly XmlQualifiedName EnumValues =
            new XmlQualifiedName("EnumValues", Namespaces.OpcUa);

        /// <summary>
        /// EnumValueType
        /// </summary>
        public static readonly XmlQualifiedName EnumValueType =
            new XmlQualifiedName("EnumValueType", Namespaces.OpcUa);

        /// <summary>
        /// Enumeration
        /// </summary>
        public static readonly XmlQualifiedName Enumeration =
            new XmlQualifiedName("Enumeration", Namespaces.OpcUa);

        /// <summary>
        /// UInt32
        /// </summary>
        public static readonly XmlQualifiedName UInt32 =
            new XmlQualifiedName("UInt32", Namespaces.OpcUa);

        /// <summary>
        /// Structure
        /// </summary>
        public static readonly XmlQualifiedName Structure =
            new XmlQualifiedName("Structure", Namespaces.OpcUa);

        /// <summary>
        /// Union
        /// </summary>
        public static readonly XmlQualifiedName Union =
            new XmlQualifiedName("Union", Namespaces.OpcUa);
    }
}
