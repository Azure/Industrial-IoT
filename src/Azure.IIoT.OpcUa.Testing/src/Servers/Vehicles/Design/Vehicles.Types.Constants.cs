/* ========================================================================
 * Copyright (c) 2005-2019 The OPC Foundation, Inc. All rights reserved.
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

namespace Vehicles.Types {
    #region DataType Identifiers
    /// <summary>
    /// A class that declares constants for all DataTypes in the Model Design.
    /// </summary>
    /// <exclude />
    [System.CodeDom.Compiler.GeneratedCodeAttribute("Opc.Ua.ModelCompiler", "1.0.0.0")]
    public static partial class DataTypes {
        /// <summary>
        /// The identifier for the VehicleType DataType.
        /// </summary>
        public const uint VehicleType = 314;

        /// <summary>
        /// The identifier for the CarType DataType.
        /// </summary>
        public const uint CarType = 315;

        /// <summary>
        /// The identifier for the TruckType DataType.
        /// </summary>
        public const uint TruckType = 316;
    }
    #endregion

    #region Object Identifiers
    /// <summary>
    /// A class that declares constants for all Objects in the Model Design.
    /// </summary>
    /// <exclude />
    [System.CodeDom.Compiler.GeneratedCodeAttribute("Opc.Ua.ModelCompiler", "1.0.0.0")]
    public static partial class Objects {
        /// <summary>
        /// The identifier for the VehicleType_Encoding_DefaultBinary Object.
        /// </summary>
        public const uint VehicleType_Encoding_DefaultBinary = 329;

        /// <summary>
        /// The identifier for the CarType_Encoding_DefaultBinary Object.
        /// </summary>
        public const uint CarType_Encoding_DefaultBinary = 330;

        /// <summary>
        /// The identifier for the TruckType_Encoding_DefaultBinary Object.
        /// </summary>
        public const uint TruckType_Encoding_DefaultBinary = 331;

        /// <summary>
        /// The identifier for the VehicleType_Encoding_DefaultXml Object.
        /// </summary>
        public const uint VehicleType_Encoding_DefaultXml = 317;

        /// <summary>
        /// The identifier for the CarType_Encoding_DefaultXml Object.
        /// </summary>
        public const uint CarType_Encoding_DefaultXml = 318;

        /// <summary>
        /// The identifier for the TruckType_Encoding_DefaultXml Object.
        /// </summary>
        public const uint TruckType_Encoding_DefaultXml = 319;

        /// <summary>
        /// The identifier for the VehicleType_Encoding_DefaultJson Object.
        /// </summary>
        public const uint VehicleType_Encoding_DefaultJson = 15003;

        /// <summary>
        /// The identifier for the CarType_Encoding_DefaultJson Object.
        /// </summary>
        public const uint CarType_Encoding_DefaultJson = 15004;

        /// <summary>
        /// The identifier for the TruckType_Encoding_DefaultJson Object.
        /// </summary>
        public const uint TruckType_Encoding_DefaultJson = 15005;
    }
    #endregion

    #region ObjectType Identifiers
    /// <summary>
    /// A class that declares constants for all ObjectTypes in the Model Design.
    /// </summary>
    /// <exclude />
    [System.CodeDom.Compiler.GeneratedCodeAttribute("Opc.Ua.ModelCompiler", "1.0.0.0")]
    public static partial class ObjectTypes {
        /// <summary>
        /// The identifier for the DriverType ObjectType.
        /// </summary>
        public const uint DriverType = 341;
    }
    #endregion

    #region Variable Identifiers
    /// <summary>
    /// A class that declares constants for all Variables in the Model Design.
    /// </summary>
    /// <exclude />
    [System.CodeDom.Compiler.GeneratedCodeAttribute("Opc.Ua.ModelCompiler", "1.0.0.0")]
    public static partial class Variables {
        /// <summary>
        /// The identifier for the DriverType_PrimaryVehicle Variable.
        /// </summary>
        public const uint DriverType_PrimaryVehicle = 342;

        /// <summary>
        /// The identifier for the DriverType_OwnedVehicles Variable.
        /// </summary>
        public const uint DriverType_OwnedVehicles = 344;

        /// <summary>
        /// The identifier for the Vehicles_BinarySchema Variable.
        /// </summary>
        public const uint Vehicles_BinarySchema = 302;

        /// <summary>
        /// The identifier for the Vehicles_BinarySchema_NamespaceUri Variable.
        /// </summary>
        public const uint Vehicles_BinarySchema_NamespaceUri = 304;

        /// <summary>
        /// The identifier for the Vehicles_BinarySchema_Deprecated Variable.
        /// </summary>
        public const uint Vehicles_BinarySchema_Deprecated = 15001;

        /// <summary>
        /// The identifier for the Vehicles_BinarySchema_VehicleType Variable.
        /// </summary>
        public const uint Vehicles_BinarySchema_VehicleType = 332;

        /// <summary>
        /// The identifier for the Vehicles_BinarySchema_CarType Variable.
        /// </summary>
        public const uint Vehicles_BinarySchema_CarType = 335;

        /// <summary>
        /// The identifier for the Vehicles_BinarySchema_TruckType Variable.
        /// </summary>
        public const uint Vehicles_BinarySchema_TruckType = 338;

        /// <summary>
        /// The identifier for the Vehicles_XmlSchema Variable.
        /// </summary>
        public const uint Vehicles_XmlSchema = 287;

        /// <summary>
        /// The identifier for the Vehicles_XmlSchema_NamespaceUri Variable.
        /// </summary>
        public const uint Vehicles_XmlSchema_NamespaceUri = 289;

        /// <summary>
        /// The identifier for the Vehicles_XmlSchema_Deprecated Variable.
        /// </summary>
        public const uint Vehicles_XmlSchema_Deprecated = 15002;

        /// <summary>
        /// The identifier for the Vehicles_XmlSchema_VehicleType Variable.
        /// </summary>
        public const uint Vehicles_XmlSchema_VehicleType = 320;

        /// <summary>
        /// The identifier for the Vehicles_XmlSchema_CarType Variable.
        /// </summary>
        public const uint Vehicles_XmlSchema_CarType = 323;

        /// <summary>
        /// The identifier for the Vehicles_XmlSchema_TruckType Variable.
        /// </summary>
        public const uint Vehicles_XmlSchema_TruckType = 326;
    }
    #endregion

    #region DataType Node Identifiers
    /// <summary>
    /// A class that declares constants for all DataTypes in the Model Design.
    /// </summary>
    /// <exclude />
    [System.CodeDom.Compiler.GeneratedCodeAttribute("Opc.Ua.ModelCompiler", "1.0.0.0")]
    public static partial class DataTypeIds {
        /// <summary>
        /// The identifier for the VehicleType DataType.
        /// </summary>
        public static readonly ExpandedNodeId VehicleType = new ExpandedNodeId(Vehicles.Types.DataTypes.VehicleType, Vehicles.Types.Namespaces.Vehicles);

        /// <summary>
        /// The identifier for the CarType DataType.
        /// </summary>
        public static readonly ExpandedNodeId CarType = new ExpandedNodeId(Vehicles.Types.DataTypes.CarType, Vehicles.Types.Namespaces.Vehicles);

        /// <summary>
        /// The identifier for the TruckType DataType.
        /// </summary>
        public static readonly ExpandedNodeId TruckType = new ExpandedNodeId(Vehicles.Types.DataTypes.TruckType, Vehicles.Types.Namespaces.Vehicles);
    }
    #endregion

    #region Object Node Identifiers
    /// <summary>
    /// A class that declares constants for all Objects in the Model Design.
    /// </summary>
    /// <exclude />
    [System.CodeDom.Compiler.GeneratedCodeAttribute("Opc.Ua.ModelCompiler", "1.0.0.0")]
    public static partial class ObjectIds {
        /// <summary>
        /// The identifier for the VehicleType_Encoding_DefaultBinary Object.
        /// </summary>
        public static readonly ExpandedNodeId VehicleType_Encoding_DefaultBinary = new ExpandedNodeId(Vehicles.Types.Objects.VehicleType_Encoding_DefaultBinary, Vehicles.Types.Namespaces.Vehicles);

        /// <summary>
        /// The identifier for the CarType_Encoding_DefaultBinary Object.
        /// </summary>
        public static readonly ExpandedNodeId CarType_Encoding_DefaultBinary = new ExpandedNodeId(Vehicles.Types.Objects.CarType_Encoding_DefaultBinary, Vehicles.Types.Namespaces.Vehicles);

        /// <summary>
        /// The identifier for the TruckType_Encoding_DefaultBinary Object.
        /// </summary>
        public static readonly ExpandedNodeId TruckType_Encoding_DefaultBinary = new ExpandedNodeId(Vehicles.Types.Objects.TruckType_Encoding_DefaultBinary, Vehicles.Types.Namespaces.Vehicles);

        /// <summary>
        /// The identifier for the VehicleType_Encoding_DefaultXml Object.
        /// </summary>
        public static readonly ExpandedNodeId VehicleType_Encoding_DefaultXml = new ExpandedNodeId(Vehicles.Types.Objects.VehicleType_Encoding_DefaultXml, Vehicles.Types.Namespaces.Vehicles);

        /// <summary>
        /// The identifier for the CarType_Encoding_DefaultXml Object.
        /// </summary>
        public static readonly ExpandedNodeId CarType_Encoding_DefaultXml = new ExpandedNodeId(Vehicles.Types.Objects.CarType_Encoding_DefaultXml, Vehicles.Types.Namespaces.Vehicles);

        /// <summary>
        /// The identifier for the TruckType_Encoding_DefaultXml Object.
        /// </summary>
        public static readonly ExpandedNodeId TruckType_Encoding_DefaultXml = new ExpandedNodeId(Vehicles.Types.Objects.TruckType_Encoding_DefaultXml, Vehicles.Types.Namespaces.Vehicles);

        /// <summary>
        /// The identifier for the VehicleType_Encoding_DefaultJson Object.
        /// </summary>
        public static readonly ExpandedNodeId VehicleType_Encoding_DefaultJson = new ExpandedNodeId(Vehicles.Types.Objects.VehicleType_Encoding_DefaultJson, Vehicles.Types.Namespaces.Vehicles);

        /// <summary>
        /// The identifier for the CarType_Encoding_DefaultJson Object.
        /// </summary>
        public static readonly ExpandedNodeId CarType_Encoding_DefaultJson = new ExpandedNodeId(Vehicles.Types.Objects.CarType_Encoding_DefaultJson, Vehicles.Types.Namespaces.Vehicles);

        /// <summary>
        /// The identifier for the TruckType_Encoding_DefaultJson Object.
        /// </summary>
        public static readonly ExpandedNodeId TruckType_Encoding_DefaultJson = new ExpandedNodeId(Vehicles.Types.Objects.TruckType_Encoding_DefaultJson, Vehicles.Types.Namespaces.Vehicles);
    }
    #endregion

    #region ObjectType Node Identifiers
    /// <summary>
    /// A class that declares constants for all ObjectTypes in the Model Design.
    /// </summary>
    /// <exclude />
    [System.CodeDom.Compiler.GeneratedCodeAttribute("Opc.Ua.ModelCompiler", "1.0.0.0")]
    public static partial class ObjectTypeIds {
        /// <summary>
        /// The identifier for the DriverType ObjectType.
        /// </summary>
        public static readonly ExpandedNodeId DriverType = new ExpandedNodeId(Vehicles.Types.ObjectTypes.DriverType, Vehicles.Types.Namespaces.Vehicles);
    }
    #endregion

    #region Variable Node Identifiers
    /// <summary>
    /// A class that declares constants for all Variables in the Model Design.
    /// </summary>
    /// <exclude />
    [System.CodeDom.Compiler.GeneratedCodeAttribute("Opc.Ua.ModelCompiler", "1.0.0.0")]
    public static partial class VariableIds {
        /// <summary>
        /// The identifier for the DriverType_PrimaryVehicle Variable.
        /// </summary>
        public static readonly ExpandedNodeId DriverType_PrimaryVehicle = new ExpandedNodeId(Vehicles.Types.Variables.DriverType_PrimaryVehicle, Vehicles.Types.Namespaces.Vehicles);

        /// <summary>
        /// The identifier for the DriverType_OwnedVehicles Variable.
        /// </summary>
        public static readonly ExpandedNodeId DriverType_OwnedVehicles = new ExpandedNodeId(Vehicles.Types.Variables.DriverType_OwnedVehicles, Vehicles.Types.Namespaces.Vehicles);

        /// <summary>
        /// The identifier for the Vehicles_BinarySchema Variable.
        /// </summary>
        public static readonly ExpandedNodeId Vehicles_BinarySchema = new ExpandedNodeId(Vehicles.Types.Variables.Vehicles_BinarySchema, Vehicles.Types.Namespaces.Vehicles);

        /// <summary>
        /// The identifier for the Vehicles_BinarySchema_NamespaceUri Variable.
        /// </summary>
        public static readonly ExpandedNodeId Vehicles_BinarySchema_NamespaceUri = new ExpandedNodeId(Vehicles.Types.Variables.Vehicles_BinarySchema_NamespaceUri, Vehicles.Types.Namespaces.Vehicles);

        /// <summary>
        /// The identifier for the Vehicles_BinarySchema_Deprecated Variable.
        /// </summary>
        public static readonly ExpandedNodeId Vehicles_BinarySchema_Deprecated = new ExpandedNodeId(Vehicles.Types.Variables.Vehicles_BinarySchema_Deprecated, Vehicles.Types.Namespaces.Vehicles);

        /// <summary>
        /// The identifier for the Vehicles_BinarySchema_VehicleType Variable.
        /// </summary>
        public static readonly ExpandedNodeId Vehicles_BinarySchema_VehicleType = new ExpandedNodeId(Vehicles.Types.Variables.Vehicles_BinarySchema_VehicleType, Vehicles.Types.Namespaces.Vehicles);

        /// <summary>
        /// The identifier for the Vehicles_BinarySchema_CarType Variable.
        /// </summary>
        public static readonly ExpandedNodeId Vehicles_BinarySchema_CarType = new ExpandedNodeId(Vehicles.Types.Variables.Vehicles_BinarySchema_CarType, Vehicles.Types.Namespaces.Vehicles);

        /// <summary>
        /// The identifier for the Vehicles_BinarySchema_TruckType Variable.
        /// </summary>
        public static readonly ExpandedNodeId Vehicles_BinarySchema_TruckType = new ExpandedNodeId(Vehicles.Types.Variables.Vehicles_BinarySchema_TruckType, Vehicles.Types.Namespaces.Vehicles);

        /// <summary>
        /// The identifier for the Vehicles_XmlSchema Variable.
        /// </summary>
        public static readonly ExpandedNodeId Vehicles_XmlSchema = new ExpandedNodeId(Vehicles.Types.Variables.Vehicles_XmlSchema, Vehicles.Types.Namespaces.Vehicles);

        /// <summary>
        /// The identifier for the Vehicles_XmlSchema_NamespaceUri Variable.
        /// </summary>
        public static readonly ExpandedNodeId Vehicles_XmlSchema_NamespaceUri = new ExpandedNodeId(Vehicles.Types.Variables.Vehicles_XmlSchema_NamespaceUri, Vehicles.Types.Namespaces.Vehicles);

        /// <summary>
        /// The identifier for the Vehicles_XmlSchema_Deprecated Variable.
        /// </summary>
        public static readonly ExpandedNodeId Vehicles_XmlSchema_Deprecated = new ExpandedNodeId(Vehicles.Types.Variables.Vehicles_XmlSchema_Deprecated, Vehicles.Types.Namespaces.Vehicles);

        /// <summary>
        /// The identifier for the Vehicles_XmlSchema_VehicleType Variable.
        /// </summary>
        public static readonly ExpandedNodeId Vehicles_XmlSchema_VehicleType = new ExpandedNodeId(Vehicles.Types.Variables.Vehicles_XmlSchema_VehicleType, Vehicles.Types.Namespaces.Vehicles);

        /// <summary>
        /// The identifier for the Vehicles_XmlSchema_CarType Variable.
        /// </summary>
        public static readonly ExpandedNodeId Vehicles_XmlSchema_CarType = new ExpandedNodeId(Vehicles.Types.Variables.Vehicles_XmlSchema_CarType, Vehicles.Types.Namespaces.Vehicles);

        /// <summary>
        /// The identifier for the Vehicles_XmlSchema_TruckType Variable.
        /// </summary>
        public static readonly ExpandedNodeId Vehicles_XmlSchema_TruckType = new ExpandedNodeId(Vehicles.Types.Variables.Vehicles_XmlSchema_TruckType, Vehicles.Types.Namespaces.Vehicles);
    }
    #endregion

    #region BrowseName Declarations
    /// <summary>
    /// Declares all of the BrowseNames used in the Model Design.
    /// </summary>
    [System.CodeDom.Compiler.GeneratedCodeAttribute("Opc.Ua.ModelCompiler", "1.0.0.0")]
    public static partial class BrowseNames {
        /// <summary>
        /// The BrowseName for the CarType component.
        /// </summary>
        public const string CarType = "CarType";

        /// <summary>
        /// The BrowseName for the DriverType component.
        /// </summary>
        public const string DriverType = "DriverType";

        /// <summary>
        /// The BrowseName for the OwnedVehicles component.
        /// </summary>
        public const string OwnedVehicles = "OwnedVehicles";

        /// <summary>
        /// The BrowseName for the PrimaryVehicle component.
        /// </summary>
        public const string PrimaryVehicle = "PrimaryVehicle";

        /// <summary>
        /// The BrowseName for the TruckType component.
        /// </summary>
        public const string TruckType = "TruckType";

        /// <summary>
        /// The BrowseName for the Vehicles_BinarySchema component.
        /// </summary>
        public const string Vehicles_BinarySchema = "Vehicles.Types";

        /// <summary>
        /// The BrowseName for the Vehicles_XmlSchema component.
        /// </summary>
        public const string Vehicles_XmlSchema = "Vehicles.Types";

        /// <summary>
        /// The BrowseName for the VehicleType component.
        /// </summary>
        public const string VehicleType = "VehicleType";
    }
    #endregion

    #region Namespace Declarations
    /// <summary>
    /// Defines constants for all namespaces referenced by the model design.
    /// </summary>
    [System.CodeDom.Compiler.GeneratedCodeAttribute("Opc.Ua.ModelCompiler", "1.0.0.0")]
    public static partial class Namespaces {
        /// <summary>
        /// The URI for the OpcUa namespace (.NET code namespace is 'Opc.Ua').
        /// </summary>
        public const string OpcUa = "http://opcfoundation.org/UA/";

        /// <summary>
        /// The URI for the OpcUaXsd namespace (.NET code namespace is 'Opc.Ua').
        /// </summary>
        public const string OpcUaXsd = "http://opcfoundation.org/UA/2008/02/Types.xsd";

        /// <summary>
        /// The URI for the Vehicles namespace (.NET code namespace is 'Vehicles.Types').
        /// </summary>
        public const string Vehicles = "http://opcfoundation.org/UA/Vehicles/Types";
    }
    #endregion
}