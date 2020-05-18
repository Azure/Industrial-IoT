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

namespace Vehicles.Instances {
    #region DataType Identifiers
    /// <summary>
    /// A class that declares constants for all DataTypes in the Model Design.
    /// </summary>
    /// <exclude />
    [System.CodeDom.Compiler.GeneratedCodeAttribute("Opc.Ua.ModelCompiler", "1.0.0.0")]
    public static partial class DataTypes {
        /// <summary>
        /// The identifier for the ParkingLotType DataType.
        /// </summary>
        public const uint ParkingLotType = 378;

        /// <summary>
        /// The identifier for the BicycleType DataType.
        /// </summary>
        public const uint BicycleType = 365;
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
        /// The identifier for the ParkingLot Object.
        /// </summary>
        public const uint ParkingLot = 281;

        /// <summary>
        /// The identifier for the ParkingLot_DriverOfTheMonth Object.
        /// </summary>
        public const uint ParkingLot_DriverOfTheMonth = 375;

        /// <summary>
        /// The identifier for the BicycleType_Encoding_DefaultBinary Object.
        /// </summary>
        public const uint BicycleType_Encoding_DefaultBinary = 370;

        /// <summary>
        /// The identifier for the BicycleType_Encoding_DefaultXml Object.
        /// </summary>
        public const uint BicycleType_Encoding_DefaultXml = 366;

        /// <summary>
        /// The identifier for the BicycleType_Encoding_DefaultJson Object.
        /// </summary>
        public const uint BicycleType_Encoding_DefaultJson = 15004;
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
        /// The identifier for the ParkingLotType_EnumValues Variable.
        /// </summary>
        public const uint ParkingLotType_EnumValues = 15001;

        /// <summary>
        /// The identifier for the ParkingLot_LotType Variable.
        /// </summary>
        public const uint ParkingLot_LotType = 380;

        /// <summary>
        /// The identifier for the ParkingLot_DriverOfTheMonth_PrimaryVehicle Variable.
        /// </summary>
        public const uint ParkingLot_DriverOfTheMonth_PrimaryVehicle = 376;

        /// <summary>
        /// The identifier for the ParkingLot_DriverOfTheMonth_OwnedVehicles Variable.
        /// </summary>
        public const uint ParkingLot_DriverOfTheMonth_OwnedVehicles = 377;

        /// <summary>
        /// The identifier for the ParkingLot_VehiclesInLot Variable.
        /// </summary>
        public const uint ParkingLot_VehiclesInLot = 283;

        /// <summary>
        /// The identifier for the VehiclesInstances_BinarySchema Variable.
        /// </summary>
        public const uint VehiclesInstances_BinarySchema = 353;

        /// <summary>
        /// The identifier for the VehiclesInstances_BinarySchema_NamespaceUri Variable.
        /// </summary>
        public const uint VehiclesInstances_BinarySchema_NamespaceUri = 355;

        /// <summary>
        /// The identifier for the VehiclesInstances_BinarySchema_Deprecated Variable.
        /// </summary>
        public const uint VehiclesInstances_BinarySchema_Deprecated = 15002;

        /// <summary>
        /// The identifier for the VehiclesInstances_BinarySchema_BicycleType Variable.
        /// </summary>
        public const uint VehiclesInstances_BinarySchema_BicycleType = 371;

        /// <summary>
        /// The identifier for the VehiclesInstances_XmlSchema Variable.
        /// </summary>
        public const uint VehiclesInstances_XmlSchema = 341;

        /// <summary>
        /// The identifier for the VehiclesInstances_XmlSchema_NamespaceUri Variable.
        /// </summary>
        public const uint VehiclesInstances_XmlSchema_NamespaceUri = 343;

        /// <summary>
        /// The identifier for the VehiclesInstances_XmlSchema_Deprecated Variable.
        /// </summary>
        public const uint VehiclesInstances_XmlSchema_Deprecated = 15003;

        /// <summary>
        /// The identifier for the VehiclesInstances_XmlSchema_BicycleType Variable.
        /// </summary>
        public const uint VehiclesInstances_XmlSchema_BicycleType = 367;
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
        /// The identifier for the ParkingLotType DataType.
        /// </summary>
        public static readonly ExpandedNodeId ParkingLotType = new ExpandedNodeId(Vehicles.Instances.DataTypes.ParkingLotType, Vehicles.Instances.Namespaces.VehiclesInstances);

        /// <summary>
        /// The identifier for the BicycleType DataType.
        /// </summary>
        public static readonly ExpandedNodeId BicycleType = new ExpandedNodeId(Vehicles.Instances.DataTypes.BicycleType, Vehicles.Instances.Namespaces.VehiclesInstances);
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
        /// The identifier for the ParkingLot Object.
        /// </summary>
        public static readonly ExpandedNodeId ParkingLot = new ExpandedNodeId(Vehicles.Instances.Objects.ParkingLot, Vehicles.Instances.Namespaces.VehiclesInstances);

        /// <summary>
        /// The identifier for the ParkingLot_DriverOfTheMonth Object.
        /// </summary>
        public static readonly ExpandedNodeId ParkingLot_DriverOfTheMonth = new ExpandedNodeId(Vehicles.Instances.Objects.ParkingLot_DriverOfTheMonth, Vehicles.Instances.Namespaces.VehiclesInstances);

        /// <summary>
        /// The identifier for the BicycleType_Encoding_DefaultBinary Object.
        /// </summary>
        public static readonly ExpandedNodeId BicycleType_Encoding_DefaultBinary = new ExpandedNodeId(Vehicles.Instances.Objects.BicycleType_Encoding_DefaultBinary, Vehicles.Instances.Namespaces.VehiclesInstances);

        /// <summary>
        /// The identifier for the BicycleType_Encoding_DefaultXml Object.
        /// </summary>
        public static readonly ExpandedNodeId BicycleType_Encoding_DefaultXml = new ExpandedNodeId(Vehicles.Instances.Objects.BicycleType_Encoding_DefaultXml, Vehicles.Instances.Namespaces.VehiclesInstances);

        /// <summary>
        /// The identifier for the BicycleType_Encoding_DefaultJson Object.
        /// </summary>
        public static readonly ExpandedNodeId BicycleType_Encoding_DefaultJson = new ExpandedNodeId(Vehicles.Instances.Objects.BicycleType_Encoding_DefaultJson, Vehicles.Instances.Namespaces.VehiclesInstances);
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
        /// The identifier for the ParkingLotType_EnumValues Variable.
        /// </summary>
        public static readonly ExpandedNodeId ParkingLotType_EnumValues = new ExpandedNodeId(Vehicles.Instances.Variables.ParkingLotType_EnumValues, Vehicles.Instances.Namespaces.VehiclesInstances);

        /// <summary>
        /// The identifier for the ParkingLot_LotType Variable.
        /// </summary>
        public static readonly ExpandedNodeId ParkingLot_LotType = new ExpandedNodeId(Vehicles.Instances.Variables.ParkingLot_LotType, Vehicles.Instances.Namespaces.VehiclesInstances);

        /// <summary>
        /// The identifier for the ParkingLot_DriverOfTheMonth_PrimaryVehicle Variable.
        /// </summary>
        public static readonly ExpandedNodeId ParkingLot_DriverOfTheMonth_PrimaryVehicle = new ExpandedNodeId(Vehicles.Instances.Variables.ParkingLot_DriverOfTheMonth_PrimaryVehicle, Vehicles.Instances.Namespaces.VehiclesInstances);

        /// <summary>
        /// The identifier for the ParkingLot_DriverOfTheMonth_OwnedVehicles Variable.
        /// </summary>
        public static readonly ExpandedNodeId ParkingLot_DriverOfTheMonth_OwnedVehicles = new ExpandedNodeId(Vehicles.Instances.Variables.ParkingLot_DriverOfTheMonth_OwnedVehicles, Vehicles.Instances.Namespaces.VehiclesInstances);

        /// <summary>
        /// The identifier for the ParkingLot_VehiclesInLot Variable.
        /// </summary>
        public static readonly ExpandedNodeId ParkingLot_VehiclesInLot = new ExpandedNodeId(Vehicles.Instances.Variables.ParkingLot_VehiclesInLot, Vehicles.Instances.Namespaces.VehiclesInstances);

        /// <summary>
        /// The identifier for the VehiclesInstances_BinarySchema Variable.
        /// </summary>
        public static readonly ExpandedNodeId VehiclesInstances_BinarySchema = new ExpandedNodeId(Vehicles.Instances.Variables.VehiclesInstances_BinarySchema, Vehicles.Instances.Namespaces.VehiclesInstances);

        /// <summary>
        /// The identifier for the VehiclesInstances_BinarySchema_NamespaceUri Variable.
        /// </summary>
        public static readonly ExpandedNodeId VehiclesInstances_BinarySchema_NamespaceUri = new ExpandedNodeId(Vehicles.Instances.Variables.VehiclesInstances_BinarySchema_NamespaceUri, Vehicles.Instances.Namespaces.VehiclesInstances);

        /// <summary>
        /// The identifier for the VehiclesInstances_BinarySchema_Deprecated Variable.
        /// </summary>
        public static readonly ExpandedNodeId VehiclesInstances_BinarySchema_Deprecated = new ExpandedNodeId(Vehicles.Instances.Variables.VehiclesInstances_BinarySchema_Deprecated, Vehicles.Instances.Namespaces.VehiclesInstances);

        /// <summary>
        /// The identifier for the VehiclesInstances_BinarySchema_BicycleType Variable.
        /// </summary>
        public static readonly ExpandedNodeId VehiclesInstances_BinarySchema_BicycleType = new ExpandedNodeId(Vehicles.Instances.Variables.VehiclesInstances_BinarySchema_BicycleType, Vehicles.Instances.Namespaces.VehiclesInstances);

        /// <summary>
        /// The identifier for the VehiclesInstances_XmlSchema Variable.
        /// </summary>
        public static readonly ExpandedNodeId VehiclesInstances_XmlSchema = new ExpandedNodeId(Vehicles.Instances.Variables.VehiclesInstances_XmlSchema, Vehicles.Instances.Namespaces.VehiclesInstances);

        /// <summary>
        /// The identifier for the VehiclesInstances_XmlSchema_NamespaceUri Variable.
        /// </summary>
        public static readonly ExpandedNodeId VehiclesInstances_XmlSchema_NamespaceUri = new ExpandedNodeId(Vehicles.Instances.Variables.VehiclesInstances_XmlSchema_NamespaceUri, Vehicles.Instances.Namespaces.VehiclesInstances);

        /// <summary>
        /// The identifier for the VehiclesInstances_XmlSchema_Deprecated Variable.
        /// </summary>
        public static readonly ExpandedNodeId VehiclesInstances_XmlSchema_Deprecated = new ExpandedNodeId(Vehicles.Instances.Variables.VehiclesInstances_XmlSchema_Deprecated, Vehicles.Instances.Namespaces.VehiclesInstances);

        /// <summary>
        /// The identifier for the VehiclesInstances_XmlSchema_BicycleType Variable.
        /// </summary>
        public static readonly ExpandedNodeId VehiclesInstances_XmlSchema_BicycleType = new ExpandedNodeId(Vehicles.Instances.Variables.VehiclesInstances_XmlSchema_BicycleType, Vehicles.Instances.Namespaces.VehiclesInstances);
    }
    #endregion

    #region BrowseName Declarations
    /// <summary>
    /// Declares all of the BrowseNames used in the Model Design.
    /// </summary>
    [System.CodeDom.Compiler.GeneratedCodeAttribute("Opc.Ua.ModelCompiler", "1.0.0.0")]
    public static partial class BrowseNames {
        /// <summary>
        /// The BrowseName for the BicycleType component.
        /// </summary>
        public const string BicycleType = "BicycleType";

        /// <summary>
        /// The BrowseName for the DriverOfTheMonth component.
        /// </summary>
        public const string DriverOfTheMonth = "DriverOfTheMonth";

        /// <summary>
        /// The BrowseName for the LotType component.
        /// </summary>
        public const string LotType = "LotType";

        /// <summary>
        /// The BrowseName for the ParkingLot component.
        /// </summary>
        public const string ParkingLot = "ParkingLot";

        /// <summary>
        /// The BrowseName for the ParkingLotType component.
        /// </summary>
        public const string ParkingLotType = "ParkingLotType";

        /// <summary>
        /// The BrowseName for the VehiclesInLot component.
        /// </summary>
        public const string VehiclesInLot = "VehiclesInLot";

        /// <summary>
        /// The BrowseName for the VehiclesInstances_BinarySchema component.
        /// </summary>
        public const string VehiclesInstances_BinarySchema = "Vehicles.Instances";

        /// <summary>
        /// The BrowseName for the VehiclesInstances_XmlSchema component.
        /// </summary>
        public const string VehiclesInstances_XmlSchema = "Vehicles.Instances";
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

        /// <summary>
        /// The URI for the VehiclesInstances namespace (.NET code namespace is 'Vehicles.Instances').
        /// </summary>
        public const string VehiclesInstances = "http://opcfoundation.org/UA/Vehicles/Instances";
    }
    #endregion
}