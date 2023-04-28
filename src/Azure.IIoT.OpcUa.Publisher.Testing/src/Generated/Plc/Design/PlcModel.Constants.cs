/* ========================================================================
 * Copyright (c) 2005-2021 The OPC Foundation, Inc. All rights reserved.
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

using Opc.Ua;

namespace PlcModel
{
    #region DataType Identifiers
    /// <summary>
    /// A class that declares constants for all DataTypes in the Model Design.
    /// </summary>
    /// <exclude />
    [System.CodeDom.Compiler.GeneratedCodeAttribute("Opc.Ua.ModelCompiler", "1.0.0.0")]
    public static partial class DataTypes
    {
        /// <summary>
        /// The identifier for the PlcDataType DataType.
        /// </summary>
        public const uint PlcDataType = 15032;

        /// <summary>
        /// The identifier for the PlcTemperatureType DataType.
        /// </summary>
        public const uint PlcTemperatureType = 15001;

        /// <summary>
        /// The identifier for the PlcHeaterStateType DataType.
        /// </summary>
        public const uint PlcHeaterStateType = 15014;
    }
    #endregion

    #region Object Identifiers
    /// <summary>
    /// A class that declares constants for all Objects in the Model Design.
    /// </summary>
    /// <exclude />
    [System.CodeDom.Compiler.GeneratedCodeAttribute("Opc.Ua.ModelCompiler", "1.0.0.0")]
    public static partial class Objects
    {
        /// <summary>
        /// The identifier for the Plc1 Object.
        /// </summary>
        public const uint Plc1 = 15070;

        /// <summary>
        /// The identifier for the PlcDataType_Encoding_DefaultBinary Object.
        /// </summary>
        public const uint PlcDataType_Encoding_DefaultBinary = 15072;

        /// <summary>
        /// The identifier for the PlcTemperatureType_Encoding_DefaultBinary Object.
        /// </summary>
        public const uint PlcTemperatureType_Encoding_DefaultBinary = 15004;

        /// <summary>
        /// The identifier for the PlcDataType_Encoding_DefaultXml Object.
        /// </summary>
        public const uint PlcDataType_Encoding_DefaultXml = 15084;

        /// <summary>
        /// The identifier for the PlcTemperatureType_Encoding_DefaultXml Object.
        /// </summary>
        public const uint PlcTemperatureType_Encoding_DefaultXml = 15008;

        /// <summary>
        /// The identifier for the PlcDataType_Encoding_DefaultJson Object.
        /// </summary>
        public const uint PlcDataType_Encoding_DefaultJson = 15096;

        /// <summary>
        /// The identifier for the PlcTemperatureType_Encoding_DefaultJson Object.
        /// </summary>
        public const uint PlcTemperatureType_Encoding_DefaultJson = 15012;
    }
    #endregion

    #region ObjectType Identifiers
    /// <summary>
    /// A class that declares constants for all ObjectTypes in the Model Design.
    /// </summary>
    /// <exclude />
    [System.CodeDom.Compiler.GeneratedCodeAttribute("Opc.Ua.ModelCompiler", "1.0.0.0")]
    public static partial class ObjectTypes
    {
        /// <summary>
        /// The identifier for the PlcType ObjectType.
        /// </summary>
        public const uint PlcType = 15068;
    }
    #endregion

    #region Variable Identifiers
    /// <summary>
    /// A class that declares constants for all Variables in the Model Design.
    /// </summary>
    /// <exclude />
    [System.CodeDom.Compiler.GeneratedCodeAttribute("Opc.Ua.ModelCompiler", "1.0.0.0")]
    public static partial class Variables
    {
        /// <summary>
        /// The identifier for the PlcHeaterStateType_EnumStrings Variable.
        /// </summary>
        public const uint PlcHeaterStateType_EnumStrings = 15015;

        /// <summary>
        /// The identifier for the PlcType_PlcStatus Variable.
        /// </summary>
        public const uint PlcType_PlcStatus = 15003;

        /// <summary>
        /// The identifier for the Plc1_PlcStatus Variable.
        /// </summary>
        public const uint Plc1_PlcStatus = 15013;

        /// <summary>
        /// The identifier for the Plc_BinarySchema Variable.
        /// </summary>
        public const uint Plc_BinarySchema = 15074;

        /// <summary>
        /// The identifier for the Plc_BinarySchema_NamespaceUri Variable.
        /// </summary>
        public const uint Plc_BinarySchema_NamespaceUri = 15076;

        /// <summary>
        /// The identifier for the Plc_BinarySchema_Deprecated Variable.
        /// </summary>
        public const uint Plc_BinarySchema_Deprecated = 15077;

        /// <summary>
        /// The identifier for the Plc_BinarySchema_PlcDataType Variable.
        /// </summary>
        public const uint Plc_BinarySchema_PlcDataType = 15078;

        /// <summary>
        /// The identifier for the Plc_BinarySchema_PlcTemperatureType Variable.
        /// </summary>
        public const uint Plc_BinarySchema_PlcTemperatureType = 15005;

        /// <summary>
        /// The identifier for the Plc_XmlSchema Variable.
        /// </summary>
        public const uint Plc_XmlSchema = 15086;

        /// <summary>
        /// The identifier for the Plc_XmlSchema_NamespaceUri Variable.
        /// </summary>
        public const uint Plc_XmlSchema_NamespaceUri = 15088;

        /// <summary>
        /// The identifier for the Plc_XmlSchema_Deprecated Variable.
        /// </summary>
        public const uint Plc_XmlSchema_Deprecated = 15089;

        /// <summary>
        /// The identifier for the Plc_XmlSchema_PlcDataType Variable.
        /// </summary>
        public const uint Plc_XmlSchema_PlcDataType = 15090;

        /// <summary>
        /// The identifier for the Plc_XmlSchema_PlcTemperatureType Variable.
        /// </summary>
        public const uint Plc_XmlSchema_PlcTemperatureType = 15009;
    }
    #endregion

    #region DataType Node Identifiers
    /// <summary>
    /// A class that declares constants for all DataTypes in the Model Design.
    /// </summary>
    /// <exclude />
    [System.CodeDom.Compiler.GeneratedCodeAttribute("Opc.Ua.ModelCompiler", "1.0.0.0")]
    public static partial class DataTypeIds
    {
        /// <summary>
        /// The identifier for the PlcDataType DataType.
        /// </summary>
        public static readonly ExpandedNodeId PlcDataType = new ExpandedNodeId(PlcModel.DataTypes.PlcDataType, PlcModel.Namespaces.Plc);

        /// <summary>
        /// The identifier for the PlcTemperatureType DataType.
        /// </summary>
        public static readonly ExpandedNodeId PlcTemperatureType = new ExpandedNodeId(PlcModel.DataTypes.PlcTemperatureType, PlcModel.Namespaces.Plc);

        /// <summary>
        /// The identifier for the PlcHeaterStateType DataType.
        /// </summary>
        public static readonly ExpandedNodeId PlcHeaterStateType = new ExpandedNodeId(PlcModel.DataTypes.PlcHeaterStateType, PlcModel.Namespaces.Plc);
    }
    #endregion

    #region Object Node Identifiers
    /// <summary>
    /// A class that declares constants for all Objects in the Model Design.
    /// </summary>
    /// <exclude />
    [System.CodeDom.Compiler.GeneratedCodeAttribute("Opc.Ua.ModelCompiler", "1.0.0.0")]
    public static partial class ObjectIds
    {
        /// <summary>
        /// The identifier for the Plc1 Object.
        /// </summary>
        public static readonly ExpandedNodeId Plc1 = new ExpandedNodeId(PlcModel.Objects.Plc1, PlcModel.Namespaces.Plc);

        /// <summary>
        /// The identifier for the PlcDataType_Encoding_DefaultBinary Object.
        /// </summary>
        public static readonly ExpandedNodeId PlcDataType_Encoding_DefaultBinary = new ExpandedNodeId(PlcModel.Objects.PlcDataType_Encoding_DefaultBinary, PlcModel.Namespaces.Plc);

        /// <summary>
        /// The identifier for the PlcTemperatureType_Encoding_DefaultBinary Object.
        /// </summary>
        public static readonly ExpandedNodeId PlcTemperatureType_Encoding_DefaultBinary = new ExpandedNodeId(PlcModel.Objects.PlcTemperatureType_Encoding_DefaultBinary, PlcModel.Namespaces.Plc);

        /// <summary>
        /// The identifier for the PlcDataType_Encoding_DefaultXml Object.
        /// </summary>
        public static readonly ExpandedNodeId PlcDataType_Encoding_DefaultXml = new ExpandedNodeId(PlcModel.Objects.PlcDataType_Encoding_DefaultXml, PlcModel.Namespaces.Plc);

        /// <summary>
        /// The identifier for the PlcTemperatureType_Encoding_DefaultXml Object.
        /// </summary>
        public static readonly ExpandedNodeId PlcTemperatureType_Encoding_DefaultXml = new ExpandedNodeId(PlcModel.Objects.PlcTemperatureType_Encoding_DefaultXml, PlcModel.Namespaces.Plc);

        /// <summary>
        /// The identifier for the PlcDataType_Encoding_DefaultJson Object.
        /// </summary>
        public static readonly ExpandedNodeId PlcDataType_Encoding_DefaultJson = new ExpandedNodeId(PlcModel.Objects.PlcDataType_Encoding_DefaultJson, PlcModel.Namespaces.Plc);

        /// <summary>
        /// The identifier for the PlcTemperatureType_Encoding_DefaultJson Object.
        /// </summary>
        public static readonly ExpandedNodeId PlcTemperatureType_Encoding_DefaultJson = new ExpandedNodeId(PlcModel.Objects.PlcTemperatureType_Encoding_DefaultJson, PlcModel.Namespaces.Plc);
    }
    #endregion

    #region ObjectType Node Identifiers
    /// <summary>
    /// A class that declares constants for all ObjectTypes in the Model Design.
    /// </summary>
    /// <exclude />
    [System.CodeDom.Compiler.GeneratedCodeAttribute("Opc.Ua.ModelCompiler", "1.0.0.0")]
    public static partial class ObjectTypeIds
    {
        /// <summary>
        /// The identifier for the PlcType ObjectType.
        /// </summary>
        public static readonly ExpandedNodeId PlcType = new ExpandedNodeId(PlcModel.ObjectTypes.PlcType, PlcModel.Namespaces.Plc);
    }
    #endregion

    #region Variable Node Identifiers
    /// <summary>
    /// A class that declares constants for all Variables in the Model Design.
    /// </summary>
    /// <exclude />
    [System.CodeDom.Compiler.GeneratedCodeAttribute("Opc.Ua.ModelCompiler", "1.0.0.0")]
    public static partial class VariableIds
    {
        /// <summary>
        /// The identifier for the PlcHeaterStateType_EnumStrings Variable.
        /// </summary>
        public static readonly ExpandedNodeId PlcHeaterStateType_EnumStrings = new ExpandedNodeId(PlcModel.Variables.PlcHeaterStateType_EnumStrings, PlcModel.Namespaces.Plc);

        /// <summary>
        /// The identifier for the PlcType_PlcStatus Variable.
        /// </summary>
        public static readonly ExpandedNodeId PlcType_PlcStatus = new ExpandedNodeId(PlcModel.Variables.PlcType_PlcStatus, PlcModel.Namespaces.Plc);

        /// <summary>
        /// The identifier for the Plc1_PlcStatus Variable.
        /// </summary>
        public static readonly ExpandedNodeId Plc1_PlcStatus = new ExpandedNodeId(PlcModel.Variables.Plc1_PlcStatus, PlcModel.Namespaces.Plc);

        /// <summary>
        /// The identifier for the Plc_BinarySchema Variable.
        /// </summary>
        public static readonly ExpandedNodeId Plc_BinarySchema = new ExpandedNodeId(PlcModel.Variables.Plc_BinarySchema, PlcModel.Namespaces.Plc);

        /// <summary>
        /// The identifier for the Plc_BinarySchema_NamespaceUri Variable.
        /// </summary>
        public static readonly ExpandedNodeId Plc_BinarySchema_NamespaceUri = new ExpandedNodeId(PlcModel.Variables.Plc_BinarySchema_NamespaceUri, PlcModel.Namespaces.Plc);

        /// <summary>
        /// The identifier for the Plc_BinarySchema_Deprecated Variable.
        /// </summary>
        public static readonly ExpandedNodeId Plc_BinarySchema_Deprecated = new ExpandedNodeId(PlcModel.Variables.Plc_BinarySchema_Deprecated, PlcModel.Namespaces.Plc);

        /// <summary>
        /// The identifier for the Plc_BinarySchema_PlcDataType Variable.
        /// </summary>
        public static readonly ExpandedNodeId Plc_BinarySchema_PlcDataType = new ExpandedNodeId(PlcModel.Variables.Plc_BinarySchema_PlcDataType, PlcModel.Namespaces.Plc);

        /// <summary>
        /// The identifier for the Plc_BinarySchema_PlcTemperatureType Variable.
        /// </summary>
        public static readonly ExpandedNodeId Plc_BinarySchema_PlcTemperatureType = new ExpandedNodeId(PlcModel.Variables.Plc_BinarySchema_PlcTemperatureType, PlcModel.Namespaces.Plc);

        /// <summary>
        /// The identifier for the Plc_XmlSchema Variable.
        /// </summary>
        public static readonly ExpandedNodeId Plc_XmlSchema = new ExpandedNodeId(PlcModel.Variables.Plc_XmlSchema, PlcModel.Namespaces.Plc);

        /// <summary>
        /// The identifier for the Plc_XmlSchema_NamespaceUri Variable.
        /// </summary>
        public static readonly ExpandedNodeId Plc_XmlSchema_NamespaceUri = new ExpandedNodeId(PlcModel.Variables.Plc_XmlSchema_NamespaceUri, PlcModel.Namespaces.Plc);

        /// <summary>
        /// The identifier for the Plc_XmlSchema_Deprecated Variable.
        /// </summary>
        public static readonly ExpandedNodeId Plc_XmlSchema_Deprecated = new ExpandedNodeId(PlcModel.Variables.Plc_XmlSchema_Deprecated, PlcModel.Namespaces.Plc);

        /// <summary>
        /// The identifier for the Plc_XmlSchema_PlcDataType Variable.
        /// </summary>
        public static readonly ExpandedNodeId Plc_XmlSchema_PlcDataType = new ExpandedNodeId(PlcModel.Variables.Plc_XmlSchema_PlcDataType, PlcModel.Namespaces.Plc);

        /// <summary>
        /// The identifier for the Plc_XmlSchema_PlcTemperatureType Variable.
        /// </summary>
        public static readonly ExpandedNodeId Plc_XmlSchema_PlcTemperatureType = new ExpandedNodeId(PlcModel.Variables.Plc_XmlSchema_PlcTemperatureType, PlcModel.Namespaces.Plc);
    }
    #endregion

    #region BrowseName Declarations
    /// <summary>
    /// Declares all of the BrowseNames used in the Model Design.
    /// </summary>
    [System.CodeDom.Compiler.GeneratedCodeAttribute("Opc.Ua.ModelCompiler", "1.0.0.0")]
    public static partial class BrowseNames
    {
        /// <summary>
        /// The BrowseName for the Plc_BinarySchema component.
        /// </summary>
        public const string Plc_BinarySchema = "PlcModel";

        /// <summary>
        /// The BrowseName for the Plc_XmlSchema component.
        /// </summary>
        public const string Plc_XmlSchema = "PlcModel";

        /// <summary>
        /// The BrowseName for the Plc1 component.
        /// </summary>
        public const string Plc1 = "Plc #1";

        /// <summary>
        /// The BrowseName for the PlcDataType component.
        /// </summary>
        public const string PlcDataType = "PlcDataType";

        /// <summary>
        /// The BrowseName for the PlcHeaterStateType component.
        /// </summary>
        public const string PlcHeaterStateType = "PlcHeaterStateType";

        /// <summary>
        /// The BrowseName for the PlcStatus component.
        /// </summary>
        public const string PlcStatus = "PlcStatus";

        /// <summary>
        /// The BrowseName for the PlcTemperatureType component.
        /// </summary>
        public const string PlcTemperatureType = "PlcTemperatureType";

        /// <summary>
        /// The BrowseName for the PlcType component.
        /// </summary>
        public const string PlcType = "PlcType";
    }
    #endregion

    #region Namespace Declarations
    /// <summary>
    /// Defines constants for all namespaces referenced by the model design.
    /// </summary>
    [System.CodeDom.Compiler.GeneratedCodeAttribute("Opc.Ua.ModelCompiler", "1.0.0.0")]
    public static partial class Namespaces
    {
        /// <summary>
        /// The URI for the OpcUa namespace (.NET code namespace is 'Opc.Ua').
        /// </summary>
        public const string OpcUa = "http://opcfoundation.org/UA/";

        /// <summary>
        /// The URI for the OpcUaXsd namespace (.NET code namespace is 'Opc.Ua').
        /// </summary>
        public const string OpcUaXsd = "http://opcfoundation.org/UA/2008/02/Types.xsd";

        /// <summary>
        /// The URI for the Plc namespace (.NET code namespace is 'PlcModel').
        /// </summary>
        public const string Plc = "http://opcfoundation.org/UA/Plc";
    }
    #endregion
}