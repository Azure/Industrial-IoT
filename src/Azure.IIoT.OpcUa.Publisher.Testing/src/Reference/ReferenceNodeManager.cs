/* ========================================================================
 * Copyright (c) 2005-2020 The OPC Foundation, Inc. All rights reserved.
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

namespace Reference
{
    using Opc.Ua;
    using Opc.Ua.Server;
    using System;
    using System.Collections.Generic;
    using System.Diagnostics;
    using System.Globalization;
    using System.Numerics;
    using System.Threading;
    using System.Xml;

    /// <summary>
    /// A node manager for a server that exposes several variables.
    /// </summary>
    public class ReferenceNodeManager : CustomNodeManager2
    {
        /// <summary>
        /// Initializes the node manager.
        /// </summary>
        /// <param name="server"></param>
        /// <param name="configuration"></param>
        public ReferenceNodeManager(IServerInternal server, ApplicationConfiguration configuration)
            : base(server, configuration, Namespaces.ReferenceApplications)
        {
            SystemContext.NodeIdFactory = this;
            _dynamicNodes = [];
        }

        /// <summary>
        /// An overrideable version of the Dispose.
        /// </summary>
        /// <param name="disposing"></param>
        protected override void Dispose(bool disposing)
        {
            if (disposing && _simulationTimer != null)
            {
                _simulationTimer.Dispose();
                _simulationTimer = null;
            }
            base.Dispose(disposing);
        }

        /// <summary>
        /// Creates the NodeId for the specified node.
        /// </summary>
        /// <param name="context"></param>
        /// <param name="node"></param>
        public override NodeId New(ISystemContext context, NodeState node)
        {
            if (node is BaseInstanceState instance &&
                instance.Parent?.NodeId.Identifier is string id)
            {
                return new NodeId(id + "_" + instance.SymbolicName, instance.Parent.NodeId.NamespaceIndex);
            }

            return node.NodeId;
        }

        private static bool IsAnalogType(BuiltInType builtInType)
        {
            switch (builtInType)
            {
                case BuiltInType.Byte:
                case BuiltInType.UInt16:
                case BuiltInType.UInt32:
                case BuiltInType.UInt64:
                case BuiltInType.SByte:
                case BuiltInType.Int16:
                case BuiltInType.Int32:
                case BuiltInType.Int64:
                case BuiltInType.Float:
                case BuiltInType.Double:
                    return true;
            }
            return false;
        }

        private static Opc.Ua.Range GetAnalogRange(BuiltInType builtInType)
        {
            switch (builtInType)
            {
                case BuiltInType.UInt16:
                    return new Opc.Ua.Range(ushort.MaxValue, ushort.MinValue);
                case BuiltInType.UInt32:
                    return new Opc.Ua.Range(uint.MaxValue, uint.MinValue);
                case BuiltInType.UInt64:
                    return new Opc.Ua.Range(ulong.MaxValue, ulong.MinValue);
                case BuiltInType.SByte:
                    return new Opc.Ua.Range(sbyte.MaxValue, sbyte.MinValue);
                case BuiltInType.Int16:
                    return new Opc.Ua.Range(short.MaxValue, short.MinValue);
                case BuiltInType.Int32:
                    return new Opc.Ua.Range(int.MaxValue, int.MinValue);
                case BuiltInType.Int64:
                    return new Opc.Ua.Range(long.MaxValue, long.MinValue);
                case BuiltInType.Float:
                    return new Opc.Ua.Range(float.MaxValue, float.MinValue);
                case BuiltInType.Double:
                    return new Opc.Ua.Range(double.MaxValue, double.MinValue);
                case BuiltInType.Byte:
                    return new Opc.Ua.Range(byte.MaxValue, byte.MinValue);
                default:
                    return new Opc.Ua.Range(sbyte.MaxValue, sbyte.MinValue);
            }
        }

        /// <summary>
        /// Does any initialization required before the address space can be used.
        /// </summary>
        /// <param name="externalReferences"></param>
        /// <remarks>
        /// The externalReferences is an out parameter that allows the node manager to link to nodes
        /// in other node managers. For example, the 'Objects' node is managed by the CoreNodeManager and
        /// should have a reference to the root folder node(s) exposed by this node manager.
        /// </remarks>
        public override void CreateAddressSpace(IDictionary<NodeId, IList<IReference>> externalReferences)
        {
            lock (Lock)
            {
                if (!externalReferences.TryGetValue(ObjectIds.ObjectsFolder, out var references))
                {
                    externalReferences[ObjectIds.ObjectsFolder] = references = [];
                }

                var root = CreateFolder(null, "CTT", "CTT");
                root.AddReference(ReferenceTypes.Organizes, true, ObjectIds.ObjectsFolder);
                references.Add(new NodeStateReference(ReferenceTypes.Organizes, false, root.NodeId));
                root.EventNotifier = EventNotifiers.SubscribeToEvents;
                AddRootNotifier(root);

                var variables = new List<BaseDataVariableState>();

                try
                {
                    // Scalar_Static
                    ResetRandomGenerator(1);
                    var scalarFolder = CreateFolder(root, "Scalar", "Scalar");
                    var scalarInstructions = CreateVariable(scalarFolder, "Scalar_Instructions", "Scalar_Instructions", DataTypeIds.String, ValueRanks.Scalar);
                    scalarInstructions.Value = "A library of Read/Write Variables of all supported data-types.";
                    variables.Add(scalarInstructions);

                    var staticFolder = CreateFolder(scalarFolder, "Scalar_Static", "Scalar_Static");
                    const string scalarStatic = "Scalar_Static_";
                    variables.Add(CreateVariable(staticFolder, scalarStatic + "Boolean", "Boolean", DataTypeIds.Boolean, ValueRanks.Scalar));
                    variables.Add(CreateVariable(staticFolder, scalarStatic + "Byte", "Byte", DataTypeIds.Byte, ValueRanks.Scalar));
                    variables.Add(CreateVariable(staticFolder, scalarStatic + "ByteString", "ByteString", DataTypeIds.ByteString, ValueRanks.Scalar));
                    variables.Add(CreateVariable(staticFolder, scalarStatic + "DateTime", "DateTime", DataTypeIds.DateTime, ValueRanks.Scalar));
                    variables.Add(CreateVariable(staticFolder, scalarStatic + "Double", "Double", DataTypeIds.Double, ValueRanks.Scalar));
                    variables.Add(CreateVariable(staticFolder, scalarStatic + "Duration", "Duration", DataTypeIds.Duration, ValueRanks.Scalar));
                    variables.Add(CreateVariable(staticFolder, scalarStatic + "Float", "Float", DataTypeIds.Float, ValueRanks.Scalar).MinimumSamplingInterval(100));
                    variables.Add(CreateVariable(staticFolder, scalarStatic + "Guid", "Guid", DataTypeIds.Guid, ValueRanks.Scalar));
                    variables.Add(CreateVariable(staticFolder, scalarStatic + "Int16", "Int16", DataTypeIds.Int16, ValueRanks.Scalar));
                    variables.Add(CreateVariable(staticFolder, scalarStatic + "Int32", "Int32", DataTypeIds.Int32, ValueRanks.Scalar));
                    variables.Add(CreateVariable(staticFolder, scalarStatic + "Int64", "Int64", DataTypeIds.Int64, ValueRanks.Scalar));
                    variables.Add(CreateVariable(staticFolder, scalarStatic + "Integer", "Integer", DataTypeIds.Integer, ValueRanks.Scalar));
                    variables.Add(CreateVariable(staticFolder, scalarStatic + "LocaleId", "LocaleId", DataTypeIds.LocaleId, ValueRanks.Scalar));
                    variables.Add(CreateVariable(staticFolder, scalarStatic + "LocalizedText", "LocalizedText", DataTypeIds.LocalizedText, ValueRanks.Scalar));
                    variables.Add(CreateVariable(staticFolder, scalarStatic + "NodeId", "NodeId", DataTypeIds.NodeId, ValueRanks.Scalar));
                    variables.Add(CreateVariable(staticFolder, scalarStatic + "Number", "Number", DataTypeIds.Number, ValueRanks.Scalar));
                    variables.Add(CreateVariable(staticFolder, scalarStatic + "QualifiedName", "QualifiedName", DataTypeIds.QualifiedName, ValueRanks.Scalar));
                    variables.Add(CreateVariable(staticFolder, scalarStatic + "SByte", "SByte", DataTypeIds.SByte, ValueRanks.Scalar));
                    variables.Add(CreateVariable(staticFolder, scalarStatic + "String", "String", DataTypeIds.String, ValueRanks.Scalar));
                    variables.Add(CreateVariable(staticFolder, scalarStatic + "TimeString", "TimeString", DataTypeIds.TimeString, ValueRanks.Scalar));
                    variables.Add(CreateVariable(staticFolder, scalarStatic + "UInt16", "UInt16", DataTypeIds.UInt16, ValueRanks.Scalar));
                    variables.Add(CreateVariable(staticFolder, scalarStatic + "UInt32", "UInt32", DataTypeIds.UInt32, ValueRanks.Scalar));
                    variables.Add(CreateVariable(staticFolder, scalarStatic + "UInt64", "UInt64", DataTypeIds.UInt64, ValueRanks.Scalar));
                    variables.Add(CreateVariable(staticFolder, scalarStatic + "UInteger", "UInteger", DataTypeIds.UInteger, ValueRanks.Scalar));
                    variables.Add(CreateVariable(staticFolder, scalarStatic + "UtcTime", "UtcTime", DataTypeIds.UtcTime, ValueRanks.Scalar));
                    variables.Add(CreateVariable(staticFolder, scalarStatic + "Variant", "Variant", BuiltInType.Variant, ValueRanks.Scalar));
                    variables.Add(CreateVariable(staticFolder, scalarStatic + "XmlElement", "XmlElement", DataTypeIds.XmlElement, ValueRanks.Scalar).MinimumSamplingInterval(1000));

                    var decimalVariable = CreateVariable(staticFolder, scalarStatic + "Decimal", "Decimal", DataTypeIds.DecimalDataType, ValueRanks.Scalar);
                    // Set an arbitrary precision decimal value.
                    var largeInteger = BigInteger.Parse("1234567890123546789012345678901234567890123456789012345", CultureInfo.InvariantCulture);
                    decimalVariable.Value = new DecimalDataType
                    {
                        Scale = 100,
                        Value = largeInteger.ToByteArray()
                    };
                    variables.Add(decimalVariable);

                    // Scalar_Static_Arrays
                    ResetRandomGenerator(2);
                    var arraysFolder = CreateFolder(staticFolder, "Scalar_Static_Arrays", "Arrays");
                    const string staticArrays = "Scalar_Static_Arrays_";

                    variables.Add(CreateVariable(arraysFolder, staticArrays + "Boolean", "Boolean", DataTypeIds.Boolean, ValueRanks.OneDimension));
                    variables.Add(CreateVariable(arraysFolder, staticArrays + "Byte", "Byte", DataTypeIds.Byte, ValueRanks.OneDimension));
                    variables.Add(CreateVariable(arraysFolder, staticArrays + "ByteString", "ByteString", DataTypeIds.ByteString, ValueRanks.OneDimension));
                    variables.Add(CreateVariable(arraysFolder, staticArrays + "DateTime", "DateTime", DataTypeIds.DateTime, ValueRanks.OneDimension));

                    var doubleArrayVar = CreateVariable(arraysFolder, staticArrays + "Double", "Double", DataTypeIds.Double, ValueRanks.OneDimension);
                    // Set the first elements of the array to a smaller value.
                    var doubleArrayVal = doubleArrayVar.Value as double[];
                    doubleArrayVal[0] %= 10E+10;
                    doubleArrayVal[1] %= 10E+10;
                    doubleArrayVal[2] %= 10E+10;
                    doubleArrayVal[3] %= 10E+10;
                    variables.Add(doubleArrayVar);

                    variables.Add(CreateVariable(arraysFolder, staticArrays + "Duration", "Duration", DataTypeIds.Duration, ValueRanks.OneDimension));

                    var floatArrayVar = CreateVariable(arraysFolder, staticArrays + "Float", "Float", DataTypeIds.Float, ValueRanks.OneDimension);
                    // Set the first elements of the array to a smaller value.
                    var floatArrayVal = floatArrayVar.Value as float[];
                    floatArrayVal[0] %= 0xf10E + 4;
                    floatArrayVal[1] %= 0xf10E + 4;
                    floatArrayVal[2] %= 0xf10E + 4;
                    floatArrayVal[3] %= 0xf10E + 4;
                    variables.Add(floatArrayVar);

                    variables.Add(CreateVariable(arraysFolder, staticArrays + "Guid", "Guid", DataTypeIds.Guid, ValueRanks.OneDimension));
                    variables.Add(CreateVariable(arraysFolder, staticArrays + "Int16", "Int16", DataTypeIds.Int16, ValueRanks.OneDimension));
                    variables.Add(CreateVariable(arraysFolder, staticArrays + "Int32", "Int32", DataTypeIds.Int32, ValueRanks.OneDimension));
                    variables.Add(CreateVariable(arraysFolder, staticArrays + "Int64", "Int64", DataTypeIds.Int64, ValueRanks.OneDimension));
                    variables.Add(CreateVariable(arraysFolder, staticArrays + "Integer", "Integer", DataTypeIds.Integer, ValueRanks.OneDimension));
                    variables.Add(CreateVariable(arraysFolder, staticArrays + "LocaleId", "LocaleId", DataTypeIds.LocaleId, ValueRanks.OneDimension));
                    variables.Add(CreateVariable(arraysFolder, staticArrays + "LocalizedText", "LocalizedText", DataTypeIds.LocalizedText, ValueRanks.OneDimension));
                    variables.Add(CreateVariable(arraysFolder, staticArrays + "NodeId", "NodeId", DataTypeIds.NodeId, ValueRanks.OneDimension));
                    variables.Add(CreateVariable(arraysFolder, staticArrays + "Number", "Number", DataTypeIds.Number, ValueRanks.OneDimension));
                    variables.Add(CreateVariable(arraysFolder, staticArrays + "QualifiedName", "QualifiedName", DataTypeIds.QualifiedName, ValueRanks.OneDimension));
                    variables.Add(CreateVariable(arraysFolder, staticArrays + "SByte", "SByte", DataTypeIds.SByte, ValueRanks.OneDimension));

                    var stringArrayVar = CreateVariable(arraysFolder, staticArrays + "String", "String", DataTypeIds.String, ValueRanks.OneDimension);
                    stringArrayVar.Value = new string[] {
                        "Лошадь_ Пурпурово( Змейка( Слон",
                        "猪 绿色 绵羊 大象~ 狗 菠萝 猪鼠",
                        "Лошадь Овцы Голубика Овцы Змейка",
                        "Чернота` Дракон Бело Дракон",
                        "Horse# Black Lemon Lemon Grape",
                        "猫< パイナップル; ドラゴン 犬 モモ",
                        "레몬} 빨간% 자주색 쥐 백색; 들" ,
                        "Yellow Sheep Peach Elephant Cow",
                        "Крыса Корова Свинья Собака Кот",
                        "龙_ 绵羊 大象 芒果; 猫'" };
                    variables.Add(stringArrayVar);

                    variables.Add(CreateVariable(arraysFolder, staticArrays + "TimeString", "TimeString", DataTypeIds.TimeString, ValueRanks.OneDimension));
                    variables.Add(CreateVariable(arraysFolder, staticArrays + "UInt16", "UInt16", DataTypeIds.UInt16, ValueRanks.OneDimension));
                    variables.Add(CreateVariable(arraysFolder, staticArrays + "UInt32", "UInt32", DataTypeIds.UInt32, ValueRanks.OneDimension));
                    variables.Add(CreateVariable(arraysFolder, staticArrays + "UInt64", "UInt64", DataTypeIds.UInt64, ValueRanks.OneDimension));
                    variables.Add(CreateVariable(arraysFolder, staticArrays + "UInteger", "UInteger", DataTypeIds.UInteger, ValueRanks.OneDimension));
                    variables.Add(CreateVariable(arraysFolder, staticArrays + "UtcTime", "UtcTime", DataTypeIds.UtcTime, ValueRanks.OneDimension));
                    variables.Add(CreateVariable(arraysFolder, staticArrays + "Variant", "Variant", BuiltInType.Variant, ValueRanks.OneDimension));
                    variables.Add(CreateVariable(arraysFolder, staticArrays + "XmlElement", "XmlElement", DataTypeIds.XmlElement, ValueRanks.OneDimension));

                    // Scalar_Static_Arrays2D
                    ResetRandomGenerator(3);
                    var arrays2DFolder = CreateFolder(staticFolder, "Scalar_Static_Arrays2D", "Arrays2D");
                    const string staticArrays2D = "Scalar_Static_Arrays2D_";
                    variables.Add(CreateVariable(arrays2DFolder, staticArrays2D + "Boolean", "Boolean", DataTypeIds.Boolean, ValueRanks.TwoDimensions));
                    variables.Add(CreateVariable(arrays2DFolder, staticArrays2D + "Byte", "Byte", DataTypeIds.Byte, ValueRanks.TwoDimensions));
                    variables.Add(CreateVariable(arrays2DFolder, staticArrays2D + "ByteString", "ByteString", DataTypeIds.ByteString, ValueRanks.TwoDimensions));
                    variables.Add(CreateVariable(arrays2DFolder, staticArrays2D + "DateTime", "DateTime", DataTypeIds.DateTime, ValueRanks.TwoDimensions));
                    variables.Add(CreateVariable(arrays2DFolder, staticArrays2D + "Double", "Double", DataTypeIds.Double, ValueRanks.TwoDimensions));
                    variables.Add(CreateVariable(arrays2DFolder, staticArrays2D + "Duration", "Duration", DataTypeIds.Duration, ValueRanks.TwoDimensions));
                    variables.Add(CreateVariable(arrays2DFolder, staticArrays2D + "Float", "Float", DataTypeIds.Float, ValueRanks.TwoDimensions));
                    variables.Add(CreateVariable(arrays2DFolder, staticArrays2D + "Guid", "Guid", DataTypeIds.Guid, ValueRanks.TwoDimensions));
                    variables.Add(CreateVariable(arrays2DFolder, staticArrays2D + "Int16", "Int16", DataTypeIds.Int16, ValueRanks.TwoDimensions));
                    variables.Add(CreateVariable(arrays2DFolder, staticArrays2D + "Int32", "Int32", DataTypeIds.Int32, ValueRanks.TwoDimensions));
                    variables.Add(CreateVariable(arrays2DFolder, staticArrays2D + "Int64", "Int64", DataTypeIds.Int64, ValueRanks.TwoDimensions));
                    variables.Add(CreateVariable(arrays2DFolder, staticArrays2D + "Integer", "Integer", DataTypeIds.Integer, ValueRanks.TwoDimensions));
                    variables.Add(CreateVariable(arrays2DFolder, staticArrays2D + "LocaleId", "LocaleId", DataTypeIds.LocaleId, ValueRanks.TwoDimensions));
                    variables.Add(CreateVariable(arrays2DFolder, staticArrays2D + "LocalizedText", "LocalizedText", DataTypeIds.LocalizedText, ValueRanks.TwoDimensions).MinimumSamplingInterval(1000));
                    variables.Add(CreateVariable(arrays2DFolder, staticArrays2D + "NodeId", "NodeId", DataTypeIds.NodeId, ValueRanks.TwoDimensions));
                    variables.Add(CreateVariable(arrays2DFolder, staticArrays2D + "Number", "Number", DataTypeIds.Number, ValueRanks.TwoDimensions));
                    variables.Add(CreateVariable(arrays2DFolder, staticArrays2D + "QualifiedName", "QualifiedName", DataTypeIds.QualifiedName, ValueRanks.TwoDimensions));
                    variables.Add(CreateVariable(arrays2DFolder, staticArrays2D + "SByte", "SByte", DataTypeIds.SByte, ValueRanks.TwoDimensions));
                    variables.Add(CreateVariable(arrays2DFolder, staticArrays2D + "String", "String", DataTypeIds.String, ValueRanks.TwoDimensions));
                    variables.Add(CreateVariable(arrays2DFolder, staticArrays2D + "TimeString", "TimeString", DataTypeIds.TimeString, ValueRanks.TwoDimensions));
                    variables.Add(CreateVariable(arrays2DFolder, staticArrays2D + "UInt16", "UInt16", DataTypeIds.UInt16, ValueRanks.TwoDimensions));
                    variables.Add(CreateVariable(arrays2DFolder, staticArrays2D + "UInt32", "UInt32", DataTypeIds.UInt32, ValueRanks.TwoDimensions));
                    variables.Add(CreateVariable(arrays2DFolder, staticArrays2D + "UInt64", "UInt64", DataTypeIds.UInt64, ValueRanks.TwoDimensions));
                    variables.Add(CreateVariable(arrays2DFolder, staticArrays2D + "UInteger", "UInteger", DataTypeIds.UInteger, ValueRanks.TwoDimensions));
                    variables.Add(CreateVariable(arrays2DFolder, staticArrays2D + "UtcTime", "UtcTime", DataTypeIds.UtcTime, ValueRanks.TwoDimensions));
                    variables.Add(CreateVariable(arrays2DFolder, staticArrays2D + "Variant", "Variant", BuiltInType.Variant, ValueRanks.TwoDimensions));
                    variables.Add(CreateVariable(arrays2DFolder, staticArrays2D + "XmlElement", "XmlElement", DataTypeIds.XmlElement, ValueRanks.TwoDimensions).MinimumSamplingInterval(1000));

                    // Scalar_Static_ArrayDynamic
                    ResetRandomGenerator(4);
                    var arrayDynamicFolder = CreateFolder(staticFolder, "Scalar_Static_ArrayDynamic", "ArrayDynamic");
                    const string staticArraysDynamic = "Scalar_Static_ArrayDynamic_";
                    variables.Add(CreateVariable(arrayDynamicFolder, staticArraysDynamic + "Boolean", "Boolean", DataTypeIds.Boolean, ValueRanks.OneOrMoreDimensions));
                    variables.Add(CreateVariable(arrayDynamicFolder, staticArraysDynamic + "Byte", "Byte", DataTypeIds.Byte, ValueRanks.OneOrMoreDimensions));
                    variables.Add(CreateVariable(arrayDynamicFolder, staticArraysDynamic + "ByteString", "ByteString", DataTypeIds.ByteString, ValueRanks.OneOrMoreDimensions));
                    variables.Add(CreateVariable(arrayDynamicFolder, staticArraysDynamic + "DateTime", "DateTime", DataTypeIds.DateTime, ValueRanks.OneOrMoreDimensions));
                    variables.Add(CreateVariable(arrayDynamicFolder, staticArraysDynamic + "Double", "Double", DataTypeIds.Double, ValueRanks.OneOrMoreDimensions));
                    variables.Add(CreateVariable(arrayDynamicFolder, staticArraysDynamic + "Duration", "Duration", DataTypeIds.Duration, ValueRanks.OneOrMoreDimensions));
                    variables.Add(CreateVariable(arrayDynamicFolder, staticArraysDynamic + "Float", "Float", DataTypeIds.Float, ValueRanks.OneOrMoreDimensions));
                    variables.Add(CreateVariable(arrayDynamicFolder, staticArraysDynamic + "Guid", "Guid", DataTypeIds.Guid, ValueRanks.OneOrMoreDimensions));
                    variables.Add(CreateVariable(arrayDynamicFolder, staticArraysDynamic + "Int16", "Int16", DataTypeIds.Int16, ValueRanks.OneOrMoreDimensions));
                    variables.Add(CreateVariable(arrayDynamicFolder, staticArraysDynamic + "Int32", "Int32", DataTypeIds.Int32, ValueRanks.OneOrMoreDimensions));
                    variables.Add(CreateVariable(arrayDynamicFolder, staticArraysDynamic + "Int64", "Int64", DataTypeIds.Int64, ValueRanks.OneOrMoreDimensions));
                    variables.Add(CreateVariable(arrayDynamicFolder, staticArraysDynamic + "Integer", "Integer", DataTypeIds.Integer, ValueRanks.OneOrMoreDimensions));
                    variables.Add(CreateVariable(arrayDynamicFolder, staticArraysDynamic + "LocaleId", "LocaleId", DataTypeIds.LocaleId, ValueRanks.OneOrMoreDimensions));
                    variables.Add(CreateVariable(arrayDynamicFolder, staticArraysDynamic + "LocalizedText", "LocalizedText", DataTypeIds.LocalizedText, ValueRanks.OneOrMoreDimensions).MinimumSamplingInterval(1000));
                    variables.Add(CreateVariable(arrayDynamicFolder, staticArraysDynamic + "NodeId", "NodeId", DataTypeIds.NodeId, ValueRanks.OneOrMoreDimensions));
                    variables.Add(CreateVariable(arrayDynamicFolder, staticArraysDynamic + "Number", "Number", DataTypeIds.Number, ValueRanks.OneOrMoreDimensions));
                    variables.Add(CreateVariable(arrayDynamicFolder, staticArraysDynamic + "QualifiedName", "QualifiedName", DataTypeIds.QualifiedName, ValueRanks.OneOrMoreDimensions).MinimumSamplingInterval(1000));
                    variables.Add(CreateVariable(arrayDynamicFolder, staticArraysDynamic + "SByte", "SByte", DataTypeIds.SByte, ValueRanks.OneOrMoreDimensions));
                    variables.Add(CreateVariable(arrayDynamicFolder, staticArraysDynamic + "String", "String", DataTypeIds.String, ValueRanks.OneOrMoreDimensions));
                    variables.Add(CreateVariable(arrayDynamicFolder, staticArraysDynamic + "TimeString", "TimeString", DataTypeIds.TimeString, ValueRanks.OneOrMoreDimensions));
                    variables.Add(CreateVariable(arrayDynamicFolder, staticArraysDynamic + "UInt16", "UInt16", DataTypeIds.UInt16, ValueRanks.OneOrMoreDimensions));
                    variables.Add(CreateVariable(arrayDynamicFolder, staticArraysDynamic + "UInt32", "UInt32", DataTypeIds.UInt32, ValueRanks.OneOrMoreDimensions));
                    variables.Add(CreateVariable(arrayDynamicFolder, staticArraysDynamic + "UInt64", "UInt64", DataTypeIds.UInt64, ValueRanks.OneOrMoreDimensions));
                    variables.Add(CreateVariable(arrayDynamicFolder, staticArraysDynamic + "UInteger", "UInteger", DataTypeIds.UInteger, ValueRanks.OneOrMoreDimensions));
                    variables.Add(CreateVariable(arrayDynamicFolder, staticArraysDynamic + "UtcTime", "UtcTime", DataTypeIds.UtcTime, ValueRanks.OneOrMoreDimensions));
                    variables.Add(CreateVariable(arrayDynamicFolder, staticArraysDynamic + "Variant", "Variant", BuiltInType.Variant, ValueRanks.OneOrMoreDimensions));
                    variables.Add(CreateVariable(arrayDynamicFolder, staticArraysDynamic + "XmlElement", "XmlElement", DataTypeIds.XmlElement, ValueRanks.OneOrMoreDimensions).MinimumSamplingInterval(1000));

                    // Scalar_Static_Mass
                    ResetRandomGenerator(5);
                    // create 100 instances of each static scalar type
                    var massFolder = CreateFolder(staticFolder, "Scalar_Static_Mass", "Mass");
                    const string staticMass = "Scalar_Static_Mass_";
                    variables.AddRange(CreateVariables(massFolder, staticMass + "Boolean", "Boolean", DataTypeIds.Boolean, ValueRanks.Scalar, 100));
                    variables.AddRange(CreateVariables(massFolder, staticMass + "Byte", "Byte", DataTypeIds.Byte, ValueRanks.Scalar, 100));
                    variables.AddRange(CreateVariables(massFolder, staticMass + "ByteString", "ByteString", DataTypeIds.ByteString, ValueRanks.Scalar, 100));
                    variables.AddRange(CreateVariables(massFolder, staticMass + "DateTime", "DateTime", DataTypeIds.DateTime, ValueRanks.Scalar, 100));
                    variables.AddRange(CreateVariables(massFolder, staticMass + "Double", "Double", DataTypeIds.Double, ValueRanks.Scalar, 100));
                    variables.AddRange(CreateVariables(massFolder, staticMass + "Duration", "Duration", DataTypeIds.Duration, ValueRanks.Scalar, 100));
                    variables.AddRange(CreateVariables(massFolder, staticMass + "Float", "Float", DataTypeIds.Float, ValueRanks.Scalar, 100));
                    variables.AddRange(CreateVariables(massFolder, staticMass + "Guid", "Guid", DataTypeIds.Guid, ValueRanks.Scalar, 100));
                    variables.AddRange(CreateVariables(massFolder, staticMass + "Int16", "Int16", DataTypeIds.Int16, ValueRanks.Scalar, 100));
                    variables.AddRange(CreateVariables(massFolder, staticMass + "Int32", "Int32", DataTypeIds.Int32, ValueRanks.Scalar, 100));
                    variables.AddRange(CreateVariables(massFolder, staticMass + "Int64", "Int64", DataTypeIds.Int64, ValueRanks.Scalar, 100));
                    variables.AddRange(CreateVariables(massFolder, staticMass + "Integer", "Integer", DataTypeIds.Integer, ValueRanks.Scalar, 100));
                    variables.AddRange(CreateVariables(massFolder, staticMass + "LocalizedText", "LocalizedText", DataTypeIds.LocalizedText, ValueRanks.Scalar, 100));
                    variables.AddRange(CreateVariables(massFolder, staticMass + "NodeId", "NodeId", DataTypeIds.NodeId, ValueRanks.Scalar, 100));
                    variables.AddRange(CreateVariables(massFolder, staticMass + "Number", "Number", DataTypeIds.Number, ValueRanks.Scalar, 100));
                    variables.AddRange(CreateVariables(massFolder, staticMass + "SByte", "SByte", DataTypeIds.SByte, ValueRanks.Scalar, 100));
                    variables.AddRange(CreateVariables(massFolder, staticMass + "String", "String", DataTypeIds.String, ValueRanks.Scalar, 100));
                    variables.AddRange(CreateVariables(massFolder, staticMass + "TimeString", "TimeString", DataTypeIds.TimeString, ValueRanks.Scalar, 100));
                    variables.AddRange(CreateVariables(massFolder, staticMass + "UInt16", "UInt16", DataTypeIds.UInt16, ValueRanks.Scalar, 100));
                    variables.AddRange(CreateVariables(massFolder, staticMass + "UInt32", "UInt32", DataTypeIds.UInt32, ValueRanks.Scalar, 100));
                    variables.AddRange(CreateVariables(massFolder, staticMass + "UInt64", "UInt64", DataTypeIds.UInt64, ValueRanks.Scalar, 100));
                    variables.AddRange(CreateVariables(massFolder, staticMass + "UInteger", "UInteger", DataTypeIds.UInteger, ValueRanks.Scalar, 100));
                    variables.AddRange(CreateVariables(massFolder, staticMass + "UtcTime", "UtcTime", DataTypeIds.UtcTime, ValueRanks.Scalar, 100));
                    variables.AddRange(CreateVariables(massFolder, staticMass + "Variant", "Variant", BuiltInType.Variant, ValueRanks.Scalar, 100));
                    variables.AddRange(CreateVariables(massFolder, staticMass + "XmlElement", "XmlElement", DataTypeIds.XmlElement, ValueRanks.Scalar, 100));

                    // Scalar_Simulation
                    ResetRandomGenerator(6);
                    var simulationFolder = CreateFolder(scalarFolder, "Scalar_Simulation", "Simulation");
                    const string scalarSimulation = "Scalar_Simulation_";
                    CreateDynamicVariable(simulationFolder, scalarSimulation + "Boolean", "Boolean", DataTypeIds.Boolean, ValueRanks.Scalar);
                    CreateDynamicVariable(simulationFolder, scalarSimulation + "Byte", "Byte", DataTypeIds.Byte, ValueRanks.Scalar);
                    CreateDynamicVariable(simulationFolder, scalarSimulation + "ByteString", "ByteString", DataTypeIds.ByteString, ValueRanks.Scalar);
                    CreateDynamicVariable(simulationFolder, scalarSimulation + "DateTime", "DateTime", DataTypeIds.DateTime, ValueRanks.Scalar);
                    CreateDynamicVariable(simulationFolder, scalarSimulation + "Double", "Double", DataTypeIds.Double, ValueRanks.Scalar);
                    CreateDynamicVariable(simulationFolder, scalarSimulation + "Duration", "Duration", DataTypeIds.Duration, ValueRanks.Scalar);
                    CreateDynamicVariable(simulationFolder, scalarSimulation + "Float", "Float", DataTypeIds.Float, ValueRanks.Scalar);
                    CreateDynamicVariable(simulationFolder, scalarSimulation + "Guid", "Guid", DataTypeIds.Guid, ValueRanks.Scalar);
                    CreateDynamicVariable(simulationFolder, scalarSimulation + "Int16", "Int16", DataTypeIds.Int16, ValueRanks.Scalar);
                    CreateDynamicVariable(simulationFolder, scalarSimulation + "Int32", "Int32", DataTypeIds.Int32, ValueRanks.Scalar);
                    CreateDynamicVariable(simulationFolder, scalarSimulation + "Int64", "Int64", DataTypeIds.Int64, ValueRanks.Scalar);
                    CreateDynamicVariable(simulationFolder, scalarSimulation + "Integer", "Integer", DataTypeIds.Integer, ValueRanks.Scalar);
                    CreateDynamicVariable(simulationFolder, scalarSimulation + "LocaleId", "LocaleId", DataTypeIds.LocaleId, ValueRanks.Scalar);
                    CreateDynamicVariable(simulationFolder, scalarSimulation + "LocalizedText", "LocalizedText", DataTypeIds.LocalizedText, ValueRanks.Scalar);
                    CreateDynamicVariable(simulationFolder, scalarSimulation + "NodeId", "NodeId", DataTypeIds.NodeId, ValueRanks.Scalar);
                    CreateDynamicVariable(simulationFolder, scalarSimulation + "Number", "Number", DataTypeIds.Number, ValueRanks.Scalar);
                    CreateDynamicVariable(simulationFolder, scalarSimulation + "QualifiedName", "QualifiedName", DataTypeIds.QualifiedName, ValueRanks.Scalar);
                    CreateDynamicVariable(simulationFolder, scalarSimulation + "SByte", "SByte", DataTypeIds.SByte, ValueRanks.Scalar);
                    CreateDynamicVariable(simulationFolder, scalarSimulation + "String", "String", DataTypeIds.String, ValueRanks.Scalar);
                    CreateDynamicVariable(simulationFolder, scalarSimulation + "TimeString", "TimeString", DataTypeIds.TimeString, ValueRanks.Scalar);
                    CreateDynamicVariable(simulationFolder, scalarSimulation + "UInt16", "UInt16", DataTypeIds.UInt16, ValueRanks.Scalar);
                    CreateDynamicVariable(simulationFolder, scalarSimulation + "UInt32", "UInt32", DataTypeIds.UInt32, ValueRanks.Scalar);
                    CreateDynamicVariable(simulationFolder, scalarSimulation + "UInt64", "UInt64", DataTypeIds.UInt64, ValueRanks.Scalar);
                    CreateDynamicVariable(simulationFolder, scalarSimulation + "UInteger", "UInteger", DataTypeIds.UInteger, ValueRanks.Scalar);
                    CreateDynamicVariable(simulationFolder, scalarSimulation + "UtcTime", "UtcTime", DataTypeIds.UtcTime, ValueRanks.Scalar);
                    CreateDynamicVariable(simulationFolder, scalarSimulation + "Variant", "Variant", BuiltInType.Variant, ValueRanks.Scalar);
                    CreateDynamicVariable(simulationFolder, scalarSimulation + "XmlElement", "XmlElement", DataTypeIds.XmlElement, ValueRanks.Scalar);

                    var intervalVariable = CreateVariable(simulationFolder, scalarSimulation + "Interval", "Interval", DataTypeIds.UInt16, ValueRanks.Scalar);
                    intervalVariable.Value = _simulationInterval;
                    intervalVariable.OnSimpleWriteValue = OnWriteInterval;

                    var enabledVariable = CreateVariable(simulationFolder, scalarSimulation + "Enabled", "Enabled", DataTypeIds.Boolean, ValueRanks.Scalar);
                    enabledVariable.Value = _simulationEnabled;
                    enabledVariable.OnSimpleWriteValue = OnWriteEnabled;

                    // Scalar_Simulation_Arrays
                    ResetRandomGenerator(7);
                    var arraysSimulationFolder = CreateFolder(simulationFolder, "Scalar_Simulation_Arrays", "Arrays");
                    const string simulationArrays = "Scalar_Simulation_Arrays_";
                    CreateDynamicVariable(arraysSimulationFolder, simulationArrays + "Boolean", "Boolean", DataTypeIds.Boolean, ValueRanks.OneDimension);
                    CreateDynamicVariable(arraysSimulationFolder, simulationArrays + "Byte", "Byte", DataTypeIds.Byte, ValueRanks.OneDimension);
                    CreateDynamicVariable(arraysSimulationFolder, simulationArrays + "ByteString", "ByteString", DataTypeIds.ByteString, ValueRanks.OneDimension);
                    CreateDynamicVariable(arraysSimulationFolder, simulationArrays + "DateTime", "DateTime", DataTypeIds.DateTime, ValueRanks.OneDimension);
                    CreateDynamicVariable(arraysSimulationFolder, simulationArrays + "Double", "Double", DataTypeIds.Double, ValueRanks.OneDimension);
                    CreateDynamicVariable(arraysSimulationFolder, simulationArrays + "Duration", "Duration", DataTypeIds.Duration, ValueRanks.OneDimension);
                    CreateDynamicVariable(arraysSimulationFolder, simulationArrays + "Float", "Float", DataTypeIds.Float, ValueRanks.OneDimension);
                    CreateDynamicVariable(arraysSimulationFolder, simulationArrays + "Guid", "Guid", DataTypeIds.Guid, ValueRanks.OneDimension);
                    CreateDynamicVariable(arraysSimulationFolder, simulationArrays + "Int16", "Int16", DataTypeIds.Int16, ValueRanks.OneDimension);
                    CreateDynamicVariable(arraysSimulationFolder, simulationArrays + "Int32", "Int32", DataTypeIds.Int32, ValueRanks.OneDimension);
                    CreateDynamicVariable(arraysSimulationFolder, simulationArrays + "Int64", "Int64", DataTypeIds.Int64, ValueRanks.OneDimension);
                    CreateDynamicVariable(arraysSimulationFolder, simulationArrays + "Integer", "Integer", DataTypeIds.Integer, ValueRanks.OneDimension);
                    CreateDynamicVariable(arraysSimulationFolder, simulationArrays + "LocaleId", "LocaleId", DataTypeIds.LocaleId, ValueRanks.OneDimension);
                    CreateDynamicVariable(arraysSimulationFolder, simulationArrays + "LocalizedText", "LocalizedText", DataTypeIds.LocalizedText, ValueRanks.OneDimension);
                    CreateDynamicVariable(arraysSimulationFolder, simulationArrays + "NodeId", "NodeId", DataTypeIds.NodeId, ValueRanks.OneDimension);
                    CreateDynamicVariable(arraysSimulationFolder, simulationArrays + "Number", "Number", DataTypeIds.Number, ValueRanks.OneDimension);
                    CreateDynamicVariable(arraysSimulationFolder, simulationArrays + "QualifiedName", "QualifiedName", DataTypeIds.QualifiedName, ValueRanks.OneDimension);
                    CreateDynamicVariable(arraysSimulationFolder, simulationArrays + "SByte", "SByte", DataTypeIds.SByte, ValueRanks.OneDimension);
                    CreateDynamicVariable(arraysSimulationFolder, simulationArrays + "String", "String", DataTypeIds.String, ValueRanks.OneDimension);
                    CreateDynamicVariable(arraysSimulationFolder, simulationArrays + "TimeString", "TimeString", DataTypeIds.TimeString, ValueRanks.OneDimension);
                    CreateDynamicVariable(arraysSimulationFolder, simulationArrays + "UInt16", "UInt16", DataTypeIds.UInt16, ValueRanks.OneDimension);
                    CreateDynamicVariable(arraysSimulationFolder, simulationArrays + "UInt32", "UInt32", DataTypeIds.UInt32, ValueRanks.OneDimension);
                    CreateDynamicVariable(arraysSimulationFolder, simulationArrays + "UInt64", "UInt64", DataTypeIds.UInt64, ValueRanks.OneDimension);
                    CreateDynamicVariable(arraysSimulationFolder, simulationArrays + "UInteger", "UInteger", DataTypeIds.UInteger, ValueRanks.OneDimension);
                    CreateDynamicVariable(arraysSimulationFolder, simulationArrays + "UtcTime", "UtcTime", DataTypeIds.UtcTime, ValueRanks.OneDimension);
                    CreateDynamicVariable(arraysSimulationFolder, simulationArrays + "Variant", "Variant", BuiltInType.Variant, ValueRanks.OneDimension);
                    CreateDynamicVariable(arraysSimulationFolder, simulationArrays + "XmlElement", "XmlElement", DataTypeIds.XmlElement, ValueRanks.OneDimension);

                    // Scalar_Simulation_Mass
                    ResetRandomGenerator(8);
                    var massSimulationFolder = CreateFolder(simulationFolder, "Scalar_Simulation_Mass", "Mass");
                    const string massSimulation = "Scalar_Simulation_Mass_";
                    CreateDynamicVariables(massSimulationFolder, massSimulation + "Boolean", "Boolean", DataTypeIds.Boolean, ValueRanks.Scalar, 100);
                    CreateDynamicVariables(massSimulationFolder, massSimulation + "Byte", "Byte", DataTypeIds.Byte, ValueRanks.Scalar, 100);
                    CreateDynamicVariables(massSimulationFolder, massSimulation + "ByteString", "ByteString", DataTypeIds.ByteString, ValueRanks.Scalar, 100);
                    CreateDynamicVariables(massSimulationFolder, massSimulation + "DateTime", "DateTime", DataTypeIds.DateTime, ValueRanks.Scalar, 100);
                    CreateDynamicVariables(massSimulationFolder, massSimulation + "Double", "Double", DataTypeIds.Double, ValueRanks.Scalar, 100);
                    CreateDynamicVariables(massSimulationFolder, massSimulation + "Duration", "Duration", DataTypeIds.Duration, ValueRanks.Scalar, 100);
                    CreateDynamicVariables(massSimulationFolder, massSimulation + "Float", "Float", DataTypeIds.Float, ValueRanks.Scalar, 100);
                    CreateDynamicVariables(massSimulationFolder, massSimulation + "Guid", "Guid", DataTypeIds.Guid, ValueRanks.Scalar, 100);
                    CreateDynamicVariables(massSimulationFolder, massSimulation + "Int16", "Int16", DataTypeIds.Int16, ValueRanks.Scalar, 100);
                    CreateDynamicVariables(massSimulationFolder, massSimulation + "Int32", "Int32", DataTypeIds.Int32, ValueRanks.Scalar, 100);
                    CreateDynamicVariables(massSimulationFolder, massSimulation + "Int64", "Int64", DataTypeIds.Int64, ValueRanks.Scalar, 100);
                    CreateDynamicVariables(massSimulationFolder, massSimulation + "Integer", "Integer", DataTypeIds.Integer, ValueRanks.Scalar, 100);
                    CreateDynamicVariables(massSimulationFolder, massSimulation + "LocaleId", "LocaleId", DataTypeIds.LocaleId, ValueRanks.Scalar, 100);
                    CreateDynamicVariables(massSimulationFolder, massSimulation + "LocalizedText", "LocalizedText", DataTypeIds.LocalizedText, ValueRanks.Scalar, 100);
                    CreateDynamicVariables(massSimulationFolder, massSimulation + "NodeId", "NodeId", DataTypeIds.NodeId, ValueRanks.Scalar, 100);
                    CreateDynamicVariables(massSimulationFolder, massSimulation + "Number", "Number", DataTypeIds.Number, ValueRanks.Scalar, 100);
                    CreateDynamicVariables(massSimulationFolder, massSimulation + "QualifiedName", "QualifiedName", DataTypeIds.QualifiedName, ValueRanks.Scalar, 100);
                    CreateDynamicVariables(massSimulationFolder, massSimulation + "SByte", "SByte", DataTypeIds.SByte, ValueRanks.Scalar, 100);
                    CreateDynamicVariables(massSimulationFolder, massSimulation + "String", "String", DataTypeIds.String, ValueRanks.Scalar, 100);
                    CreateDynamicVariables(massSimulationFolder, massSimulation + "TimeString", "TimeString", DataTypeIds.TimeString, ValueRanks.Scalar, 100);
                    CreateDynamicVariables(massSimulationFolder, massSimulation + "UInt16", "UInt16", DataTypeIds.UInt16, ValueRanks.Scalar, 100);
                    CreateDynamicVariables(massSimulationFolder, massSimulation + "UInt32", "UInt32", DataTypeIds.UInt32, ValueRanks.Scalar, 100);
                    CreateDynamicVariables(massSimulationFolder, massSimulation + "UInt64", "UInt64", DataTypeIds.UInt64, ValueRanks.Scalar, 100);
                    CreateDynamicVariables(massSimulationFolder, massSimulation + "UInteger", "UInteger", DataTypeIds.UInteger, ValueRanks.Scalar, 100);
                    CreateDynamicVariables(massSimulationFolder, massSimulation + "UtcTime", "UtcTime", DataTypeIds.UtcTime, ValueRanks.Scalar, 100);
                    CreateDynamicVariables(massSimulationFolder, massSimulation + "Variant", "Variant", BuiltInType.Variant, ValueRanks.Scalar, 100);
                    CreateDynamicVariables(massSimulationFolder, massSimulation + "XmlElement", "XmlElement", DataTypeIds.XmlElement, ValueRanks.Scalar, 100);

                    // DataAccess_DataItem
                    ResetRandomGenerator(9);
                    var daFolder = CreateFolder(root, "DataAccess", "DataAccess");
                    var daInstructions = CreateVariable(daFolder, "DataAccess_Instructions", "Instructions", DataTypeIds.String, ValueRanks.Scalar);
                    daInstructions.Value = "A library of Read/Write Variables of all supported data-types.";
                    variables.Add(daInstructions);

                    var dataItemFolder = CreateFolder(daFolder, "DataAccess_DataItem", "DataItem");
                    const string daDataItem = "DataAccess_DataIte_";

                    foreach (var name in Enum.GetNames<BuiltInType>())
                    {
                        var item = CreateDataItemVariable(dataItemFolder, daDataItem + name, name, Enum.Parse<BuiltInType>(name), ValueRanks.Scalar);

                        // set initial value to String.Empty for String node.
                        if (name == nameof(BuiltInType.String))
                        {
                            item.Value = string.Empty;
                        }
                    }

                    // DataAccess_AnalogType
                    ResetRandomGenerator(10);
                    var analogItemFolder = CreateFolder(daFolder, "DataAccess_AnalogType", "AnalogType");
                    const string daAnalogItem = "DataAccess_AnalogType_";

                    foreach (var name in Enum.GetNames<BuiltInType>())
                    {
                        var builtInType = Enum.Parse<BuiltInType>(name);
                        if (IsAnalogType(builtInType))
                        {
                            var item = CreateAnalogItemVariable(analogItemFolder, daAnalogItem + name, name, builtInType, ValueRanks.Scalar);

                            if (builtInType == BuiltInType.Int64 ||
                                builtInType == BuiltInType.UInt64)
                            {
                                // make test case without optional ranges
                                item.EngineeringUnits = null;
                                item.InstrumentRange = null;
                            }
                            else if (builtInType == BuiltInType.Float)
                            {
                                item.EURange.Value.High = 0;
                                item.EURange.Value.Low = 0;
                            }

                            //set default value for Definition property
                            if (item.Definition != null)
                            {
                                item.Definition.Value = string.Empty;
                            }
                        }
                    }

                    // DataAccess_AnalogType_Array
                    ResetRandomGenerator(11);
                    var analogArrayFolder = CreateFolder(analogItemFolder, "DataAccess_AnalogType_Array", "Array");
                    const string daAnalogArray = "DataAccess_AnalogType_Array_";

                    CreateAnalogItemVariable(analogArrayFolder, daAnalogArray + "Boolean", "Boolean", BuiltInType.Boolean, ValueRanks.OneDimension, new bool[] { true, false, true, false, true, false, true, false, true });
                    CreateAnalogItemVariable(analogArrayFolder, daAnalogArray + "Byte", "Byte", BuiltInType.Byte, ValueRanks.OneDimension, new byte[] { 0, 1, 2, 3, 4, 5, 6, 7, 8, 9 });
                    CreateAnalogItemVariable(analogArrayFolder, daAnalogArray + "ByteString", "ByteString", BuiltInType.ByteString, ValueRanks.OneDimension, new byte[][] { [0, 1, 2, 3, 4, 5, 6, 7, 8, 9], [0, 1, 2, 3, 4, 5, 6, 7, 8, 9], [0, 1, 2, 3, 4, 5, 6, 7, 8, 9], [0, 1, 2, 3, 4, 5, 6, 7, 8, 9], [0, 1, 2, 3, 4, 5, 6, 7, 8, 9], [0, 1, 2, 3, 4, 5, 6, 7, 8, 9], [0, 1, 2, 3, 4, 5, 6, 7, 8, 9], [0, 1, 2, 3, 4, 5, 6, 7, 8, 9], [0, 1, 2, 3, 4, 5, 6, 7, 8, 9], [0, 1, 2, 3, 4, 5, 6, 7, 8, 9] });
                    CreateAnalogItemVariable(analogArrayFolder, daAnalogArray + "DateTime", "DateTime", BuiltInType.DateTime, ValueRanks.OneDimension, new DateTime[] { DateTime.MinValue, DateTime.MaxValue, DateTime.MinValue, DateTime.MaxValue, DateTime.MinValue, DateTime.MaxValue, DateTime.MinValue, DateTime.MaxValue, DateTime.MinValue });
                    CreateAnalogItemVariable(analogArrayFolder, daAnalogArray + "Double", "Double", BuiltInType.Double, ValueRanks.OneDimension, new double[] { 9.00001d, 9.0002d, 9.003d, 9.04d, 9.5d, 9.06d, 9.007d, 9.008d, 9.0009d });
                    CreateAnalogItemVariable(analogArrayFolder, daAnalogArray + "Duration", "Duration", DataTypeIds.Duration, ValueRanks.OneDimension, new double[] { 9.00001d, 9.0002d, 9.003d, 9.04d, 9.5d, 9.06d, 9.007d, 9.008d, 9.0009d }, null);
                    CreateAnalogItemVariable(analogArrayFolder, daAnalogArray + "Float", "Float", BuiltInType.Float, ValueRanks.OneDimension, new float[] { 0.1f, 0.2f, 0.3f, 0.4f, 0.5f, 1.1f, 2.2f, 3.3f, 4.4f, 5.5f });
                    CreateAnalogItemVariable(analogArrayFolder, daAnalogArray + "Guid", "Guid", BuiltInType.Guid, ValueRanks.OneDimension, new Guid[] { Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid() });
                    CreateAnalogItemVariable(analogArrayFolder, daAnalogArray + "Int16", "Int16", BuiltInType.Int16, ValueRanks.OneDimension, new short[] { 0, 1, 2, 3, 4, 5, 6, 7, 8, 9 });
                    CreateAnalogItemVariable(analogArrayFolder, daAnalogArray + "Int32", "Int32", BuiltInType.Int32, ValueRanks.OneDimension, new int[] { 10, 11, 12, 13, 14, 15, 16, 17, 18, 19 });
                    CreateAnalogItemVariable(analogArrayFolder, daAnalogArray + "Int64", "Int64", BuiltInType.Int64, ValueRanks.OneDimension, new long[] { 10, 11, 12, 13, 14, 15, 16, 17, 18, 19 });
                    CreateAnalogItemVariable(analogArrayFolder, daAnalogArray + "Integer", "Integer", BuiltInType.Integer, ValueRanks.OneDimension, new long[] { 10, 11, 12, 13, 14, 15, 16, 17, 18, 19 });
                    CreateAnalogItemVariable(analogArrayFolder, daAnalogArray + "LocaleId", "LocaleId", DataTypeIds.LocaleId, ValueRanks.OneDimension, new string[] { "en", "fr", "de", "en", "fr", "de", "en", "fr", "de", "en" }, null);
                    CreateAnalogItemVariable(analogArrayFolder, daAnalogArray + "LocalizedText", "LocalizedText", BuiltInType.LocalizedText, ValueRanks.OneDimension, new LocalizedText[] { new("en", "Hello World1"), new("en", "Hello World2"), new("en", "Hello World3"), new("en", "Hello World4"), new("en", "Hello World5"), new("en", "Hello World6"), new("en", "Hello World7"), new("en", "Hello World8"), new("en", "Hello World9"), new("en", "Hello World10") });
                    CreateAnalogItemVariable(analogArrayFolder, daAnalogArray + "NodeId", "NodeId", BuiltInType.NodeId, ValueRanks.OneDimension, new NodeId[] { new(Guid.NewGuid()), new(Guid.NewGuid()), new(Guid.NewGuid()), new(Guid.NewGuid()), new(Guid.NewGuid()), new(Guid.NewGuid()), new(Guid.NewGuid()), new(Guid.NewGuid()), new(Guid.NewGuid()), new(Guid.NewGuid()) });
                    CreateAnalogItemVariable(analogArrayFolder, daAnalogArray + "Number", "Number", BuiltInType.Number, ValueRanks.OneDimension, new short[] { 1, 2, 3, 4, 5, 6, 7, 8, 9, 10 });
                    CreateAnalogItemVariable(analogArrayFolder, daAnalogArray + "QualifiedName", "QualifiedName", BuiltInType.QualifiedName, ValueRanks.OneDimension, new short[] { 1, 2, 3, 4, 5, 6, 7, 8, 9, 10 });
                    CreateAnalogItemVariable(analogArrayFolder, daAnalogArray + "SByte", "SByte", BuiltInType.SByte, ValueRanks.OneDimension, new sbyte[] { 10, 20, 30, 40, 50, 60, 70, 80, 90 });
                    CreateAnalogItemVariable(analogArrayFolder, daAnalogArray + "String", "String", BuiltInType.String, ValueRanks.OneDimension, new string[] { "a00", "b10", "c20", "d30", "e40", "f50", "g60", "h70", "i80", "j90" });
                    CreateAnalogItemVariable(analogArrayFolder, daAnalogArray + "TimeString", "TimeString", DataTypeIds.TimeString, ValueRanks.OneDimension, new string[] { DateTime.MinValue.ToString(CultureInfo.InvariantCulture), DateTime.MaxValue.ToString(CultureInfo.InvariantCulture), DateTime.MinValue.ToString(CultureInfo.InvariantCulture), DateTime.MaxValue.ToString(CultureInfo.InvariantCulture), DateTime.MinValue.ToString(CultureInfo.InvariantCulture), DateTime.MaxValue.ToString(CultureInfo.InvariantCulture), DateTime.MinValue.ToString(CultureInfo.InvariantCulture), DateTime.MaxValue.ToString(CultureInfo.InvariantCulture), DateTime.MinValue.ToString(CultureInfo.InvariantCulture), DateTime.MaxValue.ToString(CultureInfo.InvariantCulture) }, null);
                    CreateAnalogItemVariable(analogArrayFolder, daAnalogArray + "UInt16", "UInt16", BuiltInType.UInt16, ValueRanks.OneDimension, new ushort[] { 20, 21, 22, 23, 24, 25, 26, 27, 28, 29 });
                    CreateAnalogItemVariable(analogArrayFolder, daAnalogArray + "UInt32", "UInt32", BuiltInType.UInt32, ValueRanks.OneDimension, new uint[] { 30, 31, 32, 33, 34, 35, 36, 37, 38, 39 });
                    CreateAnalogItemVariable(analogArrayFolder, daAnalogArray + "UInt64", "UInt64", BuiltInType.UInt64, ValueRanks.OneDimension, new ulong[] { 10, 11, 12, 13, 14, 15, 16, 17, 18, 19 });
                    CreateAnalogItemVariable(analogArrayFolder, daAnalogArray + "UInteger", "UInteger", BuiltInType.UInteger, ValueRanks.OneDimension, new ulong[] { 10, 11, 12, 13, 14, 15, 16, 17, 18, 19 });
                    CreateAnalogItemVariable(analogArrayFolder, daAnalogArray + "UtcTime", "UtcTime", DataTypeIds.UtcTime, ValueRanks.OneDimension, new DateTime[] { DateTime.MinValue.ToUniversalTime(), DateTime.MaxValue.ToUniversalTime(), DateTime.MinValue.ToUniversalTime(), DateTime.MaxValue.ToUniversalTime(), DateTime.MinValue.ToUniversalTime(), DateTime.MaxValue.ToUniversalTime(), DateTime.MinValue.ToUniversalTime(), DateTime.MaxValue.ToUniversalTime(), DateTime.MinValue.ToUniversalTime() }, null);
                    CreateAnalogItemVariable(analogArrayFolder, daAnalogArray + "Variant", "Variant", BuiltInType.Variant, ValueRanks.OneDimension, new Variant[] { 10, 11, 12, 13, 14, 15, 16, 17, 18, 19 });
                    var doc1 = new XmlDocument();
                    CreateAnalogItemVariable(analogArrayFolder, daAnalogArray + "XmlElement", "XmlElement", BuiltInType.XmlElement, ValueRanks.OneDimension, new XmlElement[] { doc1.CreateElement("tag1"), doc1.CreateElement("tag2"), doc1.CreateElement("tag3"), doc1.CreateElement("tag4"), doc1.CreateElement("tag5"), doc1.CreateElement("tag6"), doc1.CreateElement("tag7"), doc1.CreateElement("tag8"), doc1.CreateElement("tag9"), doc1.CreateElement("tag10") });

                    // DataAccess_DiscreteType
                    ResetRandomGenerator(12);
                    var discreteTypeFolder = CreateFolder(daFolder, "DataAccess_DiscreteType", "DiscreteType");
                    var twoStateDiscreteFolder = CreateFolder(discreteTypeFolder, "DataAccess_TwoStateDiscreteType", "TwoStateDiscreteType");
                    const string daTwoStateDiscrete = "DataAccess_TwoStateDiscreteType_";

                    // Add our Nodes to the folder, and specify their customized discrete enumerations
                    CreateTwoStateDiscreteItemVariable(twoStateDiscreteFolder, daTwoStateDiscrete + "001", "001", "red", "blue");
                    CreateTwoStateDiscreteItemVariable(twoStateDiscreteFolder, daTwoStateDiscrete + "002", "002", "open", "close");
                    CreateTwoStateDiscreteItemVariable(twoStateDiscreteFolder, daTwoStateDiscrete + "003", "003", "up", "down");
                    CreateTwoStateDiscreteItemVariable(twoStateDiscreteFolder, daTwoStateDiscrete + "004", "004", "left", "right");
                    CreateTwoStateDiscreteItemVariable(twoStateDiscreteFolder, daTwoStateDiscrete + "005", "005", "circle", "cross");

                    var multiStateDiscreteFolder = CreateFolder(discreteTypeFolder, "DataAccess_MultiStateDiscreteType", "MultiStateDiscreteType");
                    const string daMultiStateDiscrete = "DataAccess_MultiStateDiscreteType_";

                    // Add our Nodes to the folder, and specify their customized discrete enumerations
                    CreateMultiStateDiscreteItemVariable(multiStateDiscreteFolder, daMultiStateDiscrete + "001", "001", "open", "closed", "jammed");
                    CreateMultiStateDiscreteItemVariable(multiStateDiscreteFolder, daMultiStateDiscrete + "002", "002", "red", "green", "blue", "cyan");
                    CreateMultiStateDiscreteItemVariable(multiStateDiscreteFolder, daMultiStateDiscrete + "003", "003", "lolo", "lo", "normal", "hi", "hihi");
                    CreateMultiStateDiscreteItemVariable(multiStateDiscreteFolder, daMultiStateDiscrete + "004", "004", "left", "right", "center");
                    CreateMultiStateDiscreteItemVariable(multiStateDiscreteFolder, daMultiStateDiscrete + "005", "005", "circle", "cross", "triangle");

                    // DataAccess_MultiStateValueDiscreteType
                    ResetRandomGenerator(13);
                    var multiStateValueDiscreteFolder = CreateFolder(discreteTypeFolder, "DataAccess_MultiStateValueDiscreteType", "MultiStateValueDiscreteType");
                    const string daMultiStateValueDiscrete = "DataAccess_MultiStateValueDiscreteType_";

                    // Add our Nodes to the folder, and specify their customized discrete enumerations
                    CreateMultiStateValueDiscreteItemVariable(multiStateValueDiscreteFolder, daMultiStateValueDiscrete + "001", "001", ["open", "closed", "jammed"]);
                    CreateMultiStateValueDiscreteItemVariable(multiStateValueDiscreteFolder, daMultiStateValueDiscrete + "002", "002", ["red", "green", "blue", "cyan"]);
                    CreateMultiStateValueDiscreteItemVariable(multiStateValueDiscreteFolder, daMultiStateValueDiscrete + "003", "003", ["lolo", "lo", "normal", "hi", "hihi"]);
                    CreateMultiStateValueDiscreteItemVariable(multiStateValueDiscreteFolder, daMultiStateValueDiscrete + "004", "004", ["left", "right", "center"]);
                    CreateMultiStateValueDiscreteItemVariable(multiStateValueDiscreteFolder, daMultiStateValueDiscrete + "005", "005", ["circle", "cross", "triangle"]);

                    // Add our Nodes to the folder and specify varying data types
                    CreateMultiStateValueDiscreteItemVariable(multiStateValueDiscreteFolder, daMultiStateValueDiscrete + "Byte", "Byte", DataTypeIds.Byte, ["open", "closed", "jammed"]);
                    CreateMultiStateValueDiscreteItemVariable(multiStateValueDiscreteFolder, daMultiStateValueDiscrete + "Int16", "Int16", DataTypeIds.Int16, ["red", "green", "blue", "cyan"]);
                    CreateMultiStateValueDiscreteItemVariable(multiStateValueDiscreteFolder, daMultiStateValueDiscrete + "Int32", "Int32", DataTypeIds.Int32, ["lolo", "lo", "normal", "hi", "hihi"]);
                    CreateMultiStateValueDiscreteItemVariable(multiStateValueDiscreteFolder, daMultiStateValueDiscrete + "Int64", "Int64", DataTypeIds.Int64, ["left", "right", "center"]);
                    CreateMultiStateValueDiscreteItemVariable(multiStateValueDiscreteFolder, daMultiStateValueDiscrete + "SByte", "SByte", DataTypeIds.SByte, ["open", "closed", "jammed"]);
                    CreateMultiStateValueDiscreteItemVariable(multiStateValueDiscreteFolder, daMultiStateValueDiscrete + "UInt16", "UInt16", DataTypeIds.UInt16, ["red", "green", "blue", "cyan"]);
                    CreateMultiStateValueDiscreteItemVariable(multiStateValueDiscreteFolder, daMultiStateValueDiscrete + "UInt32", "UInt32", DataTypeIds.UInt32, ["lolo", "lo", "normal", "hi", "hihi"]);
                    CreateMultiStateValueDiscreteItemVariable(multiStateValueDiscreteFolder, daMultiStateValueDiscrete + "UInt64", "UInt64", DataTypeIds.UInt64, ["left", "right", "center"]);

                    // References
                    ResetRandomGenerator(14);
                    var referencesFolder = CreateFolder(root, "References", "References");
                    const string referencesPrefix = "References_";

                    var referencesInstructions = CreateVariable(referencesFolder, "References_Instructions", "Instructions", DataTypeIds.String, ValueRanks.Scalar);
                    referencesInstructions.Value = "This folder will contain nodes that have specific Reference configurations.";
                    variables.Add(referencesInstructions);

                    // create variable nodes with specific references
                    var hasForwardReference = CreateMeshVariable(referencesFolder, referencesPrefix + "HasForwardReference", "HasForwardReference");
                    hasForwardReference.AddReference(ReferenceTypes.HasCause, false, variables[0].NodeId);
                    variables.Add(hasForwardReference);

                    var hasInverseReference = CreateMeshVariable(referencesFolder, referencesPrefix + "HasInverseReference", "HasInverseReference");
                    hasInverseReference.AddReference(ReferenceTypes.HasCause, true, variables[0].NodeId);
                    variables.Add(hasInverseReference);

                    BaseDataVariableState has3InverseReference = null;
                    for (var i = 1; i <= 5; i++)
                    {
                        var referenceString = "Has3ForwardReferences";
                        if (i > 1)
                        {
                            referenceString += i.ToString(CultureInfo.InvariantCulture);
                        }
                        var has3ForwardReferences = CreateMeshVariable(referencesFolder, referencesPrefix + referenceString, referenceString);
                        has3ForwardReferences.AddReference(ReferenceTypes.HasCause, false, variables[0].NodeId);
                        has3ForwardReferences.AddReference(ReferenceTypes.HasCause, false, variables[1].NodeId);
                        has3ForwardReferences.AddReference(ReferenceTypes.HasCause, false, variables[2].NodeId);
                        if (i == 1)
                        {
                            has3InverseReference = has3ForwardReferences;
                        }
                        variables.Add(has3ForwardReferences);
                    }

                    var has3InverseReferences = CreateMeshVariable(referencesFolder, referencesPrefix + "Has3InverseReferences", "Has3InverseReferences");
                    has3InverseReferences.AddReference(ReferenceTypes.HasEffect, true, variables[0].NodeId);
                    has3InverseReferences.AddReference(ReferenceTypes.HasEffect, true, variables[1].NodeId);
                    has3InverseReferences.AddReference(ReferenceTypes.HasEffect, true, variables[2].NodeId);
                    variables.Add(has3InverseReferences);

                    var hasForwardAndInverseReferences = CreateMeshVariable(referencesFolder, referencesPrefix + "HasForwardAndInverseReference", "HasForwardAndInverseReference", hasForwardReference, hasInverseReference, has3InverseReference, has3InverseReferences, variables[0]);
                    variables.Add(hasForwardAndInverseReferences);

                    // AccessRights
                    ResetRandomGenerator(15);
                    var folderAccessRights = CreateFolder(root, "AccessRights", "AccessRights");
                    const string accessRights = "AccessRights_";

                    var accessRightsInstructions = CreateVariable(folderAccessRights, accessRights + "Instructions", "Instructions", DataTypeIds.String, ValueRanks.Scalar);
                    accessRightsInstructions.Value = "This folder will be accessible to all who enter, but contents therein will be secured.";
                    variables.Add(accessRightsInstructions);

                    // sub-folder for "AccessAll"
                    var folderAccessRightsAccessAll = CreateFolder(folderAccessRights, "AccessRights_AccessAll", "AccessAll");
                    const string accessRightsAccessAll = "AccessRights_AccessAll_";

                    var arAllRO = CreateVariable(folderAccessRightsAccessAll, accessRightsAccessAll + "RO", "RO", BuiltInType.Int16, ValueRanks.Scalar);
                    arAllRO.AccessLevel = AccessLevels.CurrentRead;
                    arAllRO.UserAccessLevel = AccessLevels.CurrentRead;
                    variables.Add(arAllRO);
                    var arAllWO = CreateVariable(folderAccessRightsAccessAll, accessRightsAccessAll + "WO", "WO", BuiltInType.Int16, ValueRanks.Scalar);
                    arAllWO.AccessLevel = AccessLevels.CurrentWrite;
                    arAllWO.UserAccessLevel = AccessLevels.CurrentWrite;
                    variables.Add(arAllWO);
                    var arAllRW = CreateVariable(folderAccessRightsAccessAll, accessRightsAccessAll + "RW", "RW", BuiltInType.Int16, ValueRanks.Scalar);
                    arAllRW.AccessLevel = AccessLevels.CurrentReadOrWrite;
                    arAllRW.UserAccessLevel = AccessLevels.CurrentReadOrWrite;
                    variables.Add(arAllRW);
                    var arAllRONotUser = CreateVariable(folderAccessRightsAccessAll, accessRightsAccessAll + "RO_NotUser", "RO_NotUser", BuiltInType.Int16, ValueRanks.Scalar);
                    arAllRONotUser.AccessLevel = AccessLevels.CurrentRead;
                    arAllRONotUser.UserAccessLevel = AccessLevels.None;
                    variables.Add(arAllRONotUser);
                    var arAllWONotUser = CreateVariable(folderAccessRightsAccessAll, accessRightsAccessAll + "WO_NotUser", "WO_NotUser", BuiltInType.Int16, ValueRanks.Scalar);
                    arAllWONotUser.AccessLevel = AccessLevels.CurrentWrite;
                    arAllWONotUser.UserAccessLevel = AccessLevels.None;
                    variables.Add(arAllWONotUser);
                    var arAllRWNotUser = CreateVariable(folderAccessRightsAccessAll, accessRightsAccessAll + "RW_NotUser", "RW_NotUser", BuiltInType.Int16, ValueRanks.Scalar);
                    arAllRWNotUser.AccessLevel = AccessLevels.CurrentReadOrWrite;
                    arAllRWNotUser.UserAccessLevel = AccessLevels.CurrentRead;
                    variables.Add(arAllRWNotUser);
                    var arAllROUserRW = CreateVariable(folderAccessRightsAccessAll, accessRightsAccessAll + "RO_User1_RW", "RO_User1_RW", BuiltInType.Int16, ValueRanks.Scalar);
                    arAllROUserRW.AccessLevel = AccessLevels.CurrentRead;
                    arAllROUserRW.UserAccessLevel = AccessLevels.CurrentReadOrWrite;
                    variables.Add(arAllROUserRW);
                    var arAllROGroupRW = CreateVariable(folderAccessRightsAccessAll, accessRightsAccessAll + "RO_Group1_RW", "RO_Group1_RW", BuiltInType.Int16, ValueRanks.Scalar);
                    arAllROGroupRW.AccessLevel = AccessLevels.CurrentRead;
                    arAllROGroupRW.UserAccessLevel = AccessLevels.CurrentReadOrWrite;
                    variables.Add(arAllROGroupRW);

                    // sub-folder for "AccessUser1"
                    var folderAccessRightsAccessUser1 = CreateFolder(folderAccessRights, "AccessRights_AccessUser1", "AccessUser1");
                    const string accessRightsAccessUser1 = "AccessRights_AccessUser1_";

                    var arUserRO = CreateVariable(folderAccessRightsAccessUser1, accessRightsAccessUser1 + "RO", "RO", BuiltInType.Int16, ValueRanks.Scalar);
                    arUserRO.AccessLevel = AccessLevels.CurrentRead;
                    arUserRO.UserAccessLevel = AccessLevels.CurrentRead;
                    variables.Add(arUserRO);
                    var arUserWO = CreateVariable(folderAccessRightsAccessUser1, accessRightsAccessUser1 + "WO", "WO", BuiltInType.Int16, ValueRanks.Scalar);
                    arUserWO.AccessLevel = AccessLevels.CurrentWrite;
                    arUserWO.UserAccessLevel = AccessLevels.CurrentWrite;
                    variables.Add(arUserWO);
                    var arUserRW = CreateVariable(folderAccessRightsAccessUser1, accessRightsAccessUser1 + "RW", "RW", BuiltInType.Int16, ValueRanks.Scalar);
                    arUserRW.AccessLevel = AccessLevels.CurrentReadOrWrite;
                    arUserRW.UserAccessLevel = AccessLevels.CurrentReadOrWrite;
                    variables.Add(arUserRW);

                    // sub-folder for "AccessGroup1"
                    var folderAccessRightsAccessGroup1 = CreateFolder(folderAccessRights, "AccessRights_AccessGroup1", "AccessGroup1");
                    const string accessRightsAccessGroup1 = "AccessRights_AccessGroup1_";

                    var arGroupRO = CreateVariable(folderAccessRightsAccessGroup1, accessRightsAccessGroup1 + "RO", "RO", BuiltInType.Int16, ValueRanks.Scalar);
                    arGroupRO.AccessLevel = AccessLevels.CurrentRead;
                    arGroupRO.UserAccessLevel = AccessLevels.CurrentRead;
                    variables.Add(arGroupRO);
                    var arGroupWO = CreateVariable(folderAccessRightsAccessGroup1, accessRightsAccessGroup1 + "WO", "WO", BuiltInType.Int16, ValueRanks.Scalar);
                    arGroupWO.AccessLevel = AccessLevels.CurrentWrite;
                    arGroupWO.UserAccessLevel = AccessLevels.CurrentWrite;
                    variables.Add(arGroupWO);
                    var arGroupRW = CreateVariable(folderAccessRightsAccessGroup1, accessRightsAccessGroup1 + "RW", "RW", BuiltInType.Int16, ValueRanks.Scalar);
                    arGroupRW.AccessLevel = AccessLevels.CurrentReadOrWrite;
                    arGroupRW.UserAccessLevel = AccessLevels.CurrentReadOrWrite;
                    variables.Add(arGroupRW);

                    // sub folder for "RolePermissions"
                    var folderRolePermissions = CreateFolder(folderAccessRights, "AccessRights_RolePermissions", "RolePermissions");
                    const string rolePermissions = "AccessRights_RolePermissions_";

                    var rpAnonymous = CreateVariable(folderRolePermissions, rolePermissions + "AnonymousAccess", "AnonymousAccess", BuiltInType.Int16, ValueRanks.Scalar);
                    rpAnonymous.Description = "This node can be accessed by users that have Anonymous Role";
                    rpAnonymous.RolePermissions =
                    [
                        // allow access to users with Anonymous role
                        new RolePermissionType()
                        {
                            RoleId = ObjectIds.WellKnownRole_Anonymous,
                            Permissions = (uint)(PermissionType.Browse |PermissionType.Read|PermissionType.ReadRolePermissions | PermissionType.Write)
                        }
                    ];
                    variables.Add(rpAnonymous);

                    var rpAuthenticatedUser = CreateVariable(folderRolePermissions, rolePermissions + "AuthenticatedUser", "AuthenticatedUser", BuiltInType.Int16, ValueRanks.Scalar);
                    rpAuthenticatedUser.Description = "This node can be accessed by users that have AuthenticatedUser Role";
                    rpAuthenticatedUser.RolePermissions =
                    [
                        // allow access to users with AuthenticatedUser role
                        new RolePermissionType()
                        {
                            RoleId = ObjectIds.WellKnownRole_AuthenticatedUser,
                            Permissions = (uint)(PermissionType.Browse |PermissionType.Read|PermissionType.ReadRolePermissions | PermissionType.Write)
                        }
                    ];
                    variables.Add(rpAuthenticatedUser);

                    var rpAdminUser = CreateVariable(folderRolePermissions, rolePermissions + "AdminUser", "AdminUser", BuiltInType.Int16, ValueRanks.Scalar);
                    rpAdminUser.Description = "This node can be accessed by users that have SecurityAdmin Role over an encrypted connection";
                    rpAdminUser.AccessRestrictions = AccessRestrictionType.EncryptionRequired;
                    rpAdminUser.RolePermissions =
                    [
                        // allow access to users with SecurityAdmin role
                        new RolePermissionType()
                        {
                            RoleId = ObjectIds.WellKnownRole_SecurityAdmin,
                            Permissions = (uint)(PermissionType.Browse |PermissionType.Read|PermissionType.ReadRolePermissions | PermissionType.Write)
                        }
                    ];
                    variables.Add(rpAdminUser);

                    // sub-folder for "AccessRestrictions"
                    var folderAccessRestrictions = CreateFolder(folderAccessRights, "AccessRights_AccessRestrictions", "AccessRestrictions");
                    const string accessRestrictions = "AccessRights_AccessRestrictions_";

                    var arNone = CreateVariable(folderAccessRestrictions, accessRestrictions + "None", "None", BuiltInType.Int16, ValueRanks.Scalar);
                    arNone.AccessLevel = AccessLevels.CurrentRead;
                    arNone.UserAccessLevel = AccessLevels.CurrentRead;
                    arNone.AccessRestrictions = AccessRestrictionType.None;
                    variables.Add(arNone);

                    var arSigningRequired = CreateVariable(folderAccessRestrictions, accessRestrictions + "SigningRequired", "SigningRequired", BuiltInType.Int16, ValueRanks.Scalar);
                    arSigningRequired.AccessLevel = AccessLevels.CurrentRead;
                    arSigningRequired.UserAccessLevel = AccessLevels.CurrentRead;
                    arSigningRequired.AccessRestrictions = AccessRestrictionType.SigningRequired;
                    variables.Add(arSigningRequired);

                    var arEncryptionRequired = CreateVariable(folderAccessRestrictions, accessRestrictions + "EncryptionRequired", "EncryptionRequired", BuiltInType.Int16, ValueRanks.Scalar);
                    arEncryptionRequired.AccessLevel = AccessLevels.CurrentRead;
                    arEncryptionRequired.UserAccessLevel = AccessLevels.CurrentRead;
                    arEncryptionRequired.AccessRestrictions = AccessRestrictionType.EncryptionRequired;
                    variables.Add(arEncryptionRequired);

                    var arSessionRequired = CreateVariable(folderAccessRestrictions, accessRestrictions + "SessionRequired", "SessionRequired", BuiltInType.Int16, ValueRanks.Scalar);
                    arSessionRequired.AccessLevel = AccessLevels.CurrentRead;
                    arSessionRequired.UserAccessLevel = AccessLevels.CurrentRead;
                    arSessionRequired.AccessRestrictions = AccessRestrictionType.SessionRequired;
                    variables.Add(arSessionRequired);

                    // NodeIds
                    ResetRandomGenerator(16);
                    var nodeIdsFolder = CreateFolder(root, "NodeIds", "NodeIds");
                    const string nodeIds = "NodeIds_";

                    var nodeIdsInstructions = CreateVariable(folderAccessRights, nodeIds + "Instructions", "Instructions", DataTypeIds.String, ValueRanks.Scalar);
                    nodeIdsInstructions.Value = "All supported Node types are available except whichever is in use for the other nodes.";
                    variables.Add(nodeIdsInstructions);

                    var integerNodeId = CreateVariable(nodeIdsFolder, nodeIds + "Int16Integer", "Int16Integer", DataTypeIds.Int16, ValueRanks.Scalar);
                    integerNodeId.NodeId = new NodeId(9202, NamespaceIndex);
                    variables.Add(integerNodeId);

                    variables.Add(CreateVariable(nodeIdsFolder, nodeIds + "Int16String", "Int16String", DataTypeIds.Int16, ValueRanks.Scalar));

                    var guidNodeId = CreateVariable(nodeIdsFolder, nodeIds + "Int16GUID", "Int16GUID", DataTypeIds.Int16, ValueRanks.Scalar);
                    guidNodeId.NodeId = new NodeId(new Guid("00000000-0000-0000-0000-000000009204"), NamespaceIndex);
                    variables.Add(guidNodeId);

                    var opaqueNodeId = CreateVariable(nodeIdsFolder, nodeIds + "Int16Opaque", "Int16Opaque", DataTypeIds.Int16, ValueRanks.Scalar);
                    opaqueNodeId.NodeId = new NodeId([9, 2, 0, 5], NamespaceIndex);
                    variables.Add(opaqueNodeId);

                    // Methods
                    ResetRandomGenerator(17);
                    var methodsFolder = CreateFolder(root, "Methods", "Methods");
                    const string methods = "Methods_";

                    var methodsInstructions = CreateVariable(methodsFolder, methods + "Instructions", "Instructions", DataTypeIds.String, ValueRanks.Scalar);
                    methodsInstructions.Value = "Contains methods with varying parameter definitions.";
                    variables.Add(methodsInstructions);

                    var voidMethod = CreateMethod(methodsFolder, methods + "Void", "Void");
                    voidMethod.OnCallMethod = new GenericMethodCalledEventHandler(OnVoidCall);

                    // Add Method
                    var addMethod = CreateMethod(methodsFolder, methods + "Add", "Add");
                    // set input arguments
                    addMethod.InputArguments = new PropertyState<Argument[]>(addMethod)
                    {
                        NodeId = new NodeId(addMethod.BrowseName.Name + "InArgs", NamespaceIndex),
                        BrowseName = BrowseNames.InputArguments
                    };
                    addMethod.InputArguments.DisplayName = addMethod.InputArguments.BrowseName.Name;
                    addMethod.InputArguments.TypeDefinitionId = VariableTypeIds.PropertyType;
                    addMethod.InputArguments.ReferenceTypeId = ReferenceTypeIds.HasProperty;
                    addMethod.InputArguments.DataType = DataTypeIds.Argument;
                    addMethod.InputArguments.ValueRank = ValueRanks.OneDimension;

                    addMethod.InputArguments.Value =
                    [
                        new() { Name = "Float value", Description = "Float value",  DataType = DataTypeIds.Float, ValueRank = ValueRanks.Scalar },
                        new() { Name = "UInt32 value", Description = "UInt32 value",  DataType = DataTypeIds.UInt32, ValueRank = ValueRanks.Scalar }
                    ];

                    // set output arguments
                    addMethod.OutputArguments = new PropertyState<Argument[]>(addMethod)
                    {
                        NodeId = new NodeId(addMethod.BrowseName.Name + "OutArgs", NamespaceIndex),
                        BrowseName = BrowseNames.OutputArguments
                    };
                    addMethod.OutputArguments.DisplayName = addMethod.OutputArguments.BrowseName.Name;
                    addMethod.OutputArguments.TypeDefinitionId = VariableTypeIds.PropertyType;
                    addMethod.OutputArguments.ReferenceTypeId = ReferenceTypeIds.HasProperty;
                    addMethod.OutputArguments.DataType = DataTypeIds.Argument;
                    addMethod.OutputArguments.ValueRank = ValueRanks.OneDimension;

                    addMethod.OutputArguments.Value =
                    [
                        new() { Name = "Add Result", Description = "Add Result",  DataType = DataTypeIds.Float, ValueRank = ValueRanks.Scalar }
                    ];

                    addMethod.OnCallMethod = new GenericMethodCalledEventHandler(OnAddCall);

                    // Multiply Method
                    var multiplyMethod = CreateMethod(methodsFolder, methods + "Multiply", "Multiply");
                    // set input arguments
                    multiplyMethod.InputArguments = new PropertyState<Argument[]>(multiplyMethod)
                    {
                        NodeId = new NodeId(multiplyMethod.BrowseName.Name + "InArgs", NamespaceIndex),
                        BrowseName = BrowseNames.InputArguments
                    };
                    multiplyMethod.InputArguments.DisplayName = multiplyMethod.InputArguments.BrowseName.Name;
                    multiplyMethod.InputArguments.TypeDefinitionId = VariableTypeIds.PropertyType;
                    multiplyMethod.InputArguments.ReferenceTypeId = ReferenceTypeIds.HasProperty;
                    multiplyMethod.InputArguments.DataType = DataTypeIds.Argument;
                    multiplyMethod.InputArguments.ValueRank = ValueRanks.OneDimension;

                    multiplyMethod.InputArguments.Value =
                    [
                        new() { Name = "Int16 value", Description = "Int16 value",  DataType = DataTypeIds.Int16, ValueRank = ValueRanks.Scalar },
                        new() { Name = "UInt16 value", Description = "UInt16 value",  DataType = DataTypeIds.UInt16, ValueRank = ValueRanks.Scalar }
                    ];

                    // set output arguments
                    multiplyMethod.OutputArguments = new PropertyState<Argument[]>(multiplyMethod)
                    {
                        NodeId = new NodeId(multiplyMethod.BrowseName.Name + "OutArgs", NamespaceIndex),
                        BrowseName = BrowseNames.OutputArguments
                    };
                    multiplyMethod.OutputArguments.DisplayName = multiplyMethod.OutputArguments.BrowseName.Name;
                    multiplyMethod.OutputArguments.TypeDefinitionId = VariableTypeIds.PropertyType;
                    multiplyMethod.OutputArguments.ReferenceTypeId = ReferenceTypeIds.HasProperty;
                    multiplyMethod.OutputArguments.DataType = DataTypeIds.Argument;
                    multiplyMethod.OutputArguments.ValueRank = ValueRanks.OneDimension;

                    multiplyMethod.OutputArguments.Value =
                    [
                        new() { Name = "Multiply Result", Description = "Multiply Result",  DataType = DataTypeIds.Int32, ValueRank = ValueRanks.Scalar }
                    ];

                    multiplyMethod.OnCallMethod = new GenericMethodCalledEventHandler(OnMultiplyCall);

                    // Divide Method
                    var divideMethod = CreateMethod(methodsFolder, methods + "Divide", "Divide");
                    // set input arguments
                    divideMethod.InputArguments = new PropertyState<Argument[]>(divideMethod)
                    {
                        NodeId = new NodeId(divideMethod.BrowseName.Name + "InArgs", NamespaceIndex),
                        BrowseName = BrowseNames.InputArguments
                    };
                    divideMethod.InputArguments.DisplayName = divideMethod.InputArguments.BrowseName.Name;
                    divideMethod.InputArguments.TypeDefinitionId = VariableTypeIds.PropertyType;
                    divideMethod.InputArguments.ReferenceTypeId = ReferenceTypeIds.HasProperty;
                    divideMethod.InputArguments.DataType = DataTypeIds.Argument;
                    divideMethod.InputArguments.ValueRank = ValueRanks.OneDimension;

                    divideMethod.InputArguments.Value =
                    [
                        new() { Name = "Int32 value", Description = "Int32 value",  DataType = DataTypeIds.Int32, ValueRank = ValueRanks.Scalar },
                        new() { Name = "UInt16 value", Description = "UInt16 value",  DataType = DataTypeIds.UInt16, ValueRank = ValueRanks.Scalar }
                    ];

                    // set output arguments
                    divideMethod.OutputArguments = new PropertyState<Argument[]>(divideMethod)
                    {
                        NodeId = new NodeId(divideMethod.BrowseName.Name + "OutArgs", NamespaceIndex),
                        BrowseName = BrowseNames.OutputArguments
                    };
                    divideMethod.OutputArguments.DisplayName = divideMethod.OutputArguments.BrowseName.Name;
                    divideMethod.OutputArguments.TypeDefinitionId = VariableTypeIds.PropertyType;
                    divideMethod.OutputArguments.ReferenceTypeId = ReferenceTypeIds.HasProperty;
                    divideMethod.OutputArguments.DataType = DataTypeIds.Argument;
                    divideMethod.OutputArguments.ValueRank = ValueRanks.OneDimension;

                    divideMethod.OutputArguments.Value =
                    [
                        new() { Name = "Divide Result", Description = "Divide Result",  DataType = DataTypeIds.Float, ValueRank = ValueRanks.Scalar }
                    ];

                    divideMethod.OnCallMethod = new GenericMethodCalledEventHandler(OnDivideCall);

                    // Substract Method
                    var substractMethod = CreateMethod(methodsFolder, methods + "Substract", "Substract");
                    // set input arguments
                    substractMethod.InputArguments = new PropertyState<Argument[]>(substractMethod)
                    {
                        NodeId = new NodeId(substractMethod.BrowseName.Name + "InArgs", NamespaceIndex),
                        BrowseName = BrowseNames.InputArguments
                    };
                    substractMethod.InputArguments.DisplayName = substractMethod.InputArguments.BrowseName.Name;
                    substractMethod.InputArguments.TypeDefinitionId = VariableTypeIds.PropertyType;
                    substractMethod.InputArguments.ReferenceTypeId = ReferenceTypeIds.HasProperty;
                    substractMethod.InputArguments.DataType = DataTypeIds.Argument;
                    substractMethod.InputArguments.ValueRank = ValueRanks.OneDimension;

                    substractMethod.InputArguments.Value =
                    [
                        new() { Name = "Int16 value", Description = "Int16 value",  DataType = DataTypeIds.Int16, ValueRank = ValueRanks.Scalar },
                        new() { Name = "Byte value", Description = "Byte value",  DataType = DataTypeIds.Byte, ValueRank = ValueRanks.Scalar }
                    ];

                    // set output arguments
                    substractMethod.OutputArguments = new PropertyState<Argument[]>(substractMethod)
                    {
                        NodeId = new NodeId(substractMethod.BrowseName.Name + "OutArgs", NamespaceIndex),
                        BrowseName = BrowseNames.OutputArguments
                    };
                    substractMethod.OutputArguments.DisplayName = substractMethod.OutputArguments.BrowseName.Name;
                    substractMethod.OutputArguments.TypeDefinitionId = VariableTypeIds.PropertyType;
                    substractMethod.OutputArguments.ReferenceTypeId = ReferenceTypeIds.HasProperty;
                    substractMethod.OutputArguments.DataType = DataTypeIds.Argument;
                    substractMethod.OutputArguments.ValueRank = ValueRanks.OneDimension;

                    substractMethod.OutputArguments.Value =
                    [
                        new() { Name = "Substract Result", Description = "Substract Result",  DataType = DataTypeIds.Int16, ValueRank = ValueRanks.Scalar }
                    ];

                    substractMethod.OnCallMethod = new GenericMethodCalledEventHandler(OnSubstractCall);

                    // Hello Method
                    var helloMethod = CreateMethod(methodsFolder, methods + "Hello", "Hello");
                    // set input arguments
                    helloMethod.InputArguments = new PropertyState<Argument[]>(helloMethod)
                    {
                        NodeId = new NodeId(helloMethod.BrowseName.Name + "InArgs", NamespaceIndex),
                        BrowseName = BrowseNames.InputArguments
                    };
                    helloMethod.InputArguments.DisplayName = helloMethod.InputArguments.BrowseName.Name;
                    helloMethod.InputArguments.TypeDefinitionId = VariableTypeIds.PropertyType;
                    helloMethod.InputArguments.ReferenceTypeId = ReferenceTypeIds.HasProperty;
                    helloMethod.InputArguments.DataType = DataTypeIds.Argument;
                    helloMethod.InputArguments.ValueRank = ValueRanks.OneDimension;

                    helloMethod.InputArguments.Value =
                    [
                        new() { Name = "String value", Description = "String value",  DataType = DataTypeIds.String, ValueRank = ValueRanks.Scalar }
                    ];

                    // set output arguments
                    helloMethod.OutputArguments = new PropertyState<Argument[]>(helloMethod)
                    {
                        NodeId = new NodeId(helloMethod.BrowseName.Name + "OutArgs", NamespaceIndex),
                        BrowseName = BrowseNames.OutputArguments
                    };
                    helloMethod.OutputArguments.DisplayName = helloMethod.OutputArguments.BrowseName.Name;
                    helloMethod.OutputArguments.TypeDefinitionId = VariableTypeIds.PropertyType;
                    helloMethod.OutputArguments.ReferenceTypeId = ReferenceTypeIds.HasProperty;
                    helloMethod.OutputArguments.DataType = DataTypeIds.Argument;
                    helloMethod.OutputArguments.ValueRank = ValueRanks.OneDimension;

                    helloMethod.OutputArguments.Value =
                    [
                        new() { Name = "Hello Result", Description = "Hello Result",  DataType = DataTypeIds.String, ValueRank = ValueRanks.Scalar }
                    ];

                    helloMethod.OnCallMethod = new GenericMethodCalledEventHandler(OnHelloCall);

                    // Input Method
                    var inputMethod = CreateMethod(methodsFolder, methods + "Input", "Input");
                    // set input arguments
                    inputMethod.InputArguments = new PropertyState<Argument[]>(inputMethod)
                    {
                        NodeId = new NodeId(inputMethod.BrowseName.Name + "InArgs", NamespaceIndex),
                        BrowseName = BrowseNames.InputArguments
                    };
                    inputMethod.InputArguments.DisplayName = inputMethod.InputArguments.BrowseName.Name;
                    inputMethod.InputArguments.TypeDefinitionId = VariableTypeIds.PropertyType;
                    inputMethod.InputArguments.ReferenceTypeId = ReferenceTypeIds.HasProperty;
                    inputMethod.InputArguments.DataType = DataTypeIds.Argument;
                    inputMethod.InputArguments.ValueRank = ValueRanks.OneDimension;

                    inputMethod.InputArguments.Value =
                    [
                        new() { Name = "String value", Description = "String value",  DataType = DataTypeIds.String, ValueRank = ValueRanks.Scalar }
                    ];

                    inputMethod.OnCallMethod = new GenericMethodCalledEventHandler(OnInputCall);

                    // Output Method
                    var outputMethod = CreateMethod(methodsFolder, methods + "Output", "Output");

                    // set output arguments
                    outputMethod.OutputArguments = new PropertyState<Argument[]>(helloMethod)
                    {
                        NodeId = new NodeId(helloMethod.BrowseName.Name + "OutArgs", NamespaceIndex),
                        BrowseName = BrowseNames.OutputArguments
                    };
                    outputMethod.OutputArguments.DisplayName = helloMethod.OutputArguments.BrowseName.Name;
                    outputMethod.OutputArguments.TypeDefinitionId = VariableTypeIds.PropertyType;
                    outputMethod.OutputArguments.ReferenceTypeId = ReferenceTypeIds.HasProperty;
                    outputMethod.OutputArguments.DataType = DataTypeIds.Argument;
                    outputMethod.OutputArguments.ValueRank = ValueRanks.OneDimension;

                    outputMethod.OutputArguments.Value =
                    [
                        new() { Name = "Output Result", Description = "Output Result",  DataType = DataTypeIds.String, ValueRank = ValueRanks.Scalar }
                    ];

                    outputMethod.OnCallMethod = new GenericMethodCalledEventHandler(OnOutputCall);

                    // Views
                    ResetRandomGenerator(18);
                    var viewsFolder = CreateFolder(root, "Views", "Views");
                    const string views = "Views_";
                    var viewStateOperations = CreateView(viewsFolder, externalReferences, views + "Operations", "Operations");
                    viewStateOperations.AddReference(ReferenceTypes.Organizes, false, massFolder.NodeId);
                    massFolder.AddReference(ReferenceTypes.Organizes, true, viewStateOperations.NodeId);

                    var viewStateEngineering = CreateView(viewsFolder, externalReferences, views + "Engineering", "Engineering");
                    viewStateEngineering.AddReference(ReferenceTypes.Organizes, false, simulationFolder.NodeId);
                    simulationFolder.AddReference(ReferenceTypes.Organizes, true, viewStateEngineering.NodeId);

                    // Locales
                    ResetRandomGenerator(19);
                    var localesFolder = CreateFolder(root, "Locales", "Locales");
                    const string locales = "Locales_";

                    var qnEnglishVariable = CreateVariable(localesFolder, locales + "QNEnglish", "QNEnglish", DataTypeIds.QualifiedName, ValueRanks.Scalar);
                    qnEnglishVariable.Description = new LocalizedText("en", "English");
                    qnEnglishVariable.Value = new QualifiedName("Hello World", NamespaceIndex);
                    variables.Add(qnEnglishVariable);
                    var ltEnglishVariable = CreateVariable(localesFolder, locales + "LTEnglish", "LTEnglish", DataTypeIds.LocalizedText, ValueRanks.Scalar);
                    ltEnglishVariable.Description = new LocalizedText("en", "English");
                    ltEnglishVariable.Value = new LocalizedText("en", "Hello World");
                    variables.Add(ltEnglishVariable);

                    var qnFrancaisVariable = CreateVariable(localesFolder, locales + "QNFrancais", "QNFrancais", DataTypeIds.QualifiedName, ValueRanks.Scalar);
                    qnFrancaisVariable.Description = new LocalizedText("en", "Francais");
                    qnFrancaisVariable.Value = new QualifiedName("Salut tout le monde", NamespaceIndex);
                    variables.Add(qnFrancaisVariable);
                    var ltFrancaisVariable = CreateVariable(localesFolder, locales + "LTFrancais", "LTFrancais", DataTypeIds.LocalizedText, ValueRanks.Scalar);
                    ltFrancaisVariable.Description = new LocalizedText("en", "Francais");
                    ltFrancaisVariable.Value = new LocalizedText("fr", "Salut tout le monde");
                    variables.Add(ltFrancaisVariable);

                    var qnDeutschVariable = CreateVariable(localesFolder, locales + "QNDeutsch", "QNDeutsch", DataTypeIds.QualifiedName, ValueRanks.Scalar);
                    qnDeutschVariable.Description = new LocalizedText("en", "Deutsch");
                    qnDeutschVariable.Value = new QualifiedName("Hallo Welt", NamespaceIndex);
                    variables.Add(qnDeutschVariable);
                    var ltDeutschVariable = CreateVariable(localesFolder, locales + "LTDeutsch", "LTDeutsch", DataTypeIds.LocalizedText, ValueRanks.Scalar);
                    ltDeutschVariable.Description = new LocalizedText("en", "Deutsch");
                    ltDeutschVariable.Value = new LocalizedText("de", "Hallo Welt");
                    variables.Add(ltDeutschVariable);

                    var qnEspanolVariable = CreateVariable(localesFolder, locales + "QNEspanol", "QNEspanol", DataTypeIds.QualifiedName, ValueRanks.Scalar);
                    qnEspanolVariable.Description = new LocalizedText("en", "Espanol");
                    qnEspanolVariable.Value = new QualifiedName("Hola mundo", NamespaceIndex);
                    variables.Add(qnEspanolVariable);
                    var ltEspanolVariable = CreateVariable(localesFolder, locales + "LTEspanol", "LTEspanol", DataTypeIds.LocalizedText, ValueRanks.Scalar);
                    ltEspanolVariable.Description = new LocalizedText("en", "Espanol");
                    ltEspanolVariable.Value = new LocalizedText("es", "Hola mundo");
                    variables.Add(ltEspanolVariable);

                    var qnJapaneseVariable = CreateVariable(localesFolder, locales + "QN日本の", "QN日本の", DataTypeIds.QualifiedName, ValueRanks.Scalar);
                    qnJapaneseVariable.Description = new LocalizedText("en", "Japanese");
                    qnJapaneseVariable.Value = new QualifiedName("ハローワールド", NamespaceIndex);
                    variables.Add(qnJapaneseVariable);
                    var ltJapaneseVariable = CreateVariable(localesFolder, locales + "LT日本の", "LT日本の", DataTypeIds.LocalizedText, ValueRanks.Scalar);
                    ltJapaneseVariable.Description = new LocalizedText("en", "Japanese");
                    ltJapaneseVariable.Value = new LocalizedText("jp", "ハローワールド");
                    variables.Add(ltJapaneseVariable);

                    var qnChineseVariable = CreateVariable(localesFolder, locales + "QN中國的", "QN中國的", DataTypeIds.QualifiedName, ValueRanks.Scalar);
                    qnChineseVariable.Description = new LocalizedText("en", "Chinese");
                    qnChineseVariable.Value = new QualifiedName("世界您好", NamespaceIndex);
                    variables.Add(qnChineseVariable);
                    var ltChineseVariable = CreateVariable(localesFolder, locales + "LT中國的", "LT中國的", DataTypeIds.LocalizedText, ValueRanks.Scalar);
                    ltChineseVariable.Description = new LocalizedText("en", "Chinese");
                    ltChineseVariable.Value = new LocalizedText("ch", "世界您好");
                    variables.Add(ltChineseVariable);

                    var qnRussianVariable = CreateVariable(localesFolder, locales + "QNрусский", "QNрусский", DataTypeIds.QualifiedName, ValueRanks.Scalar);
                    qnRussianVariable.Description = new LocalizedText("en", "Russian");
                    qnRussianVariable.Value = new QualifiedName("LTрусский", NamespaceIndex);
                    variables.Add(qnRussianVariable);
                    var ltRussianVariable = CreateVariable(localesFolder, locales + "LTрусский", "LTрусский", DataTypeIds.LocalizedText, ValueRanks.Scalar);
                    ltRussianVariable.Description = new LocalizedText("en", "Russian");
                    ltRussianVariable.Value = new LocalizedText("ru", "LTрусский");
                    variables.Add(ltRussianVariable);

                    var qnArabicVariable = CreateVariable(localesFolder, locales + "QNالعربية", "QNالعربية", DataTypeIds.QualifiedName, ValueRanks.Scalar);
                    qnArabicVariable.Description = new LocalizedText("en", "Arabic");
                    qnArabicVariable.Value = new QualifiedName("مرحبا بالعال", NamespaceIndex);
                    variables.Add(qnArabicVariable);
                    var ltArabicVariable = CreateVariable(localesFolder, locales + "LTالعربية", "LTالعربية", DataTypeIds.LocalizedText, ValueRanks.Scalar);
                    ltArabicVariable.Description = new LocalizedText("en", "Arabic");
                    ltArabicVariable.Value = new LocalizedText("ae", "مرحبا بالعال");
                    variables.Add(ltArabicVariable);

                    var qnKlingonVariable = CreateVariable(localesFolder, locales + "QNtlhIngan", "QNtlhIngan", DataTypeIds.QualifiedName, ValueRanks.Scalar);
                    qnKlingonVariable.Description = new LocalizedText("en", "Klingon");
                    qnKlingonVariable.Value = new QualifiedName("qo' vIvan", NamespaceIndex);
                    variables.Add(qnKlingonVariable);
                    var ltKlingonVariable = CreateVariable(localesFolder, locales + "LTtlhIngan", "LTtlhIngan", DataTypeIds.LocalizedText, ValueRanks.Scalar);
                    ltKlingonVariable.Description = new LocalizedText("en", "Klingon");
                    ltKlingonVariable.Value = new LocalizedText("ko", "qo' vIvan");
                    variables.Add(ltKlingonVariable);

                    // Attributes
                    ResetRandomGenerator(20);
                    var folderAttributes = CreateFolder(root, "Attributes", "Attributes");

                    // AccessAll
                    var folderAttributesAccessAll = CreateFolder(folderAttributes, "Attributes_AccessAll", "AccessAll");
                    const string attributesAccessAll = "Attributes_AccessAll_";

                    var accessLevelAccessAll = CreateVariable(folderAttributesAccessAll, attributesAccessAll + "AccessLevel", "AccessLevel", DataTypeIds.Double, ValueRanks.Scalar);
                    accessLevelAccessAll.WriteMask = AttributeWriteMask.AccessLevel;
                    accessLevelAccessAll.UserWriteMask = AttributeWriteMask.AccessLevel;
                    variables.Add(accessLevelAccessAll);

                    var arrayDimensionsAccessLevel = CreateVariable(folderAttributesAccessAll, attributesAccessAll + "ArrayDimensions", "ArrayDimensions", DataTypeIds.Double, ValueRanks.Scalar);
                    arrayDimensionsAccessLevel.WriteMask = AttributeWriteMask.ArrayDimensions;
                    arrayDimensionsAccessLevel.UserWriteMask = AttributeWriteMask.ArrayDimensions;
                    variables.Add(arrayDimensionsAccessLevel);

                    var browseNameAccessLevel = CreateVariable(folderAttributesAccessAll, attributesAccessAll + "BrowseName", "BrowseName", DataTypeIds.Double, ValueRanks.Scalar);
                    browseNameAccessLevel.WriteMask = AttributeWriteMask.BrowseName;
                    browseNameAccessLevel.UserWriteMask = AttributeWriteMask.BrowseName;
                    variables.Add(browseNameAccessLevel);

                    var containsNoLoopsAccessLevel = CreateVariable(folderAttributesAccessAll, attributesAccessAll + "ContainsNoLoops", "ContainsNoLoops", DataTypeIds.Double, ValueRanks.Scalar);
                    containsNoLoopsAccessLevel.WriteMask = AttributeWriteMask.ContainsNoLoops;
                    containsNoLoopsAccessLevel.UserWriteMask = AttributeWriteMask.ContainsNoLoops;
                    variables.Add(containsNoLoopsAccessLevel);

                    var dataTypeAccessLevel = CreateVariable(folderAttributesAccessAll, attributesAccessAll + "DataType", "DataType", DataTypeIds.Double, ValueRanks.Scalar);
                    dataTypeAccessLevel.WriteMask = AttributeWriteMask.DataType;
                    dataTypeAccessLevel.UserWriteMask = AttributeWriteMask.DataType;
                    variables.Add(dataTypeAccessLevel);

                    var descriptionAccessLevel = CreateVariable(folderAttributesAccessAll, attributesAccessAll + "Description", "Description", DataTypeIds.Double, ValueRanks.Scalar);
                    descriptionAccessLevel.WriteMask = AttributeWriteMask.Description;
                    descriptionAccessLevel.UserWriteMask = AttributeWriteMask.Description;
                    variables.Add(descriptionAccessLevel);

                    var eventNotifierAccessLevel = CreateVariable(folderAttributesAccessAll, attributesAccessAll + "EventNotifier", "EventNotifier", DataTypeIds.Double, ValueRanks.Scalar);
                    eventNotifierAccessLevel.WriteMask = AttributeWriteMask.EventNotifier;
                    eventNotifierAccessLevel.UserWriteMask = AttributeWriteMask.EventNotifier;
                    variables.Add(eventNotifierAccessLevel);

                    var executableAccessLevel = CreateVariable(folderAttributesAccessAll, attributesAccessAll + "Executable", "Executable", DataTypeIds.Double, ValueRanks.Scalar);
                    executableAccessLevel.WriteMask = AttributeWriteMask.Executable;
                    executableAccessLevel.UserWriteMask = AttributeWriteMask.Executable;
                    variables.Add(executableAccessLevel);

                    var historizingAccessLevel = CreateVariable(folderAttributesAccessAll, attributesAccessAll + "Historizing", "Historizing", DataTypeIds.Double, ValueRanks.Scalar);
                    historizingAccessLevel.WriteMask = AttributeWriteMask.Historizing;
                    historizingAccessLevel.UserWriteMask = AttributeWriteMask.Historizing;
                    variables.Add(historizingAccessLevel);

                    var inverseNameAccessLevel = CreateVariable(folderAttributesAccessAll, attributesAccessAll + "InverseName", "InverseName", DataTypeIds.Double, ValueRanks.Scalar);
                    inverseNameAccessLevel.WriteMask = AttributeWriteMask.InverseName;
                    inverseNameAccessLevel.UserWriteMask = AttributeWriteMask.InverseName;
                    variables.Add(inverseNameAccessLevel);

                    var isAbstractAccessLevel = CreateVariable(folderAttributesAccessAll, attributesAccessAll + "IsAbstract", "IsAbstract", DataTypeIds.Double, ValueRanks.Scalar);
                    isAbstractAccessLevel.WriteMask = AttributeWriteMask.IsAbstract;
                    isAbstractAccessLevel.UserWriteMask = AttributeWriteMask.IsAbstract;
                    variables.Add(isAbstractAccessLevel);

                    var minimumSamplingIntervalAccessLevel = CreateVariable(folderAttributesAccessAll, attributesAccessAll + "MinimumSamplingInterval", "MinimumSamplingInterval", DataTypeIds.Double, ValueRanks.Scalar);
                    minimumSamplingIntervalAccessLevel.WriteMask = AttributeWriteMask.MinimumSamplingInterval;
                    minimumSamplingIntervalAccessLevel.UserWriteMask = AttributeWriteMask.MinimumSamplingInterval;
                    variables.Add(minimumSamplingIntervalAccessLevel);

                    var nodeClassIntervalAccessLevel = CreateVariable(folderAttributesAccessAll, attributesAccessAll + "NodeClass", "NodeClass", DataTypeIds.Double, ValueRanks.Scalar);
                    nodeClassIntervalAccessLevel.WriteMask = AttributeWriteMask.NodeClass;
                    nodeClassIntervalAccessLevel.UserWriteMask = AttributeWriteMask.NodeClass;
                    variables.Add(nodeClassIntervalAccessLevel);

                    var nodeIdAccessLevel = CreateVariable(folderAttributesAccessAll, attributesAccessAll + "NodeId", "NodeId", DataTypeIds.Double, ValueRanks.Scalar);
                    nodeIdAccessLevel.WriteMask = AttributeWriteMask.NodeId;
                    nodeIdAccessLevel.UserWriteMask = AttributeWriteMask.NodeId;
                    variables.Add(nodeIdAccessLevel);

                    var symmetricAccessLevel = CreateVariable(folderAttributesAccessAll, attributesAccessAll + "Symmetric", "Symmetric", DataTypeIds.Double, ValueRanks.Scalar);
                    symmetricAccessLevel.WriteMask = AttributeWriteMask.Symmetric;
                    symmetricAccessLevel.UserWriteMask = AttributeWriteMask.Symmetric;
                    variables.Add(symmetricAccessLevel);

                    var userAccessLevelAccessLevel = CreateVariable(folderAttributesAccessAll, attributesAccessAll + "UserAccessLevel", "UserAccessLevel", DataTypeIds.Double, ValueRanks.Scalar);
                    userAccessLevelAccessLevel.WriteMask = AttributeWriteMask.UserAccessLevel;
                    userAccessLevelAccessLevel.UserWriteMask = AttributeWriteMask.UserAccessLevel;
                    variables.Add(userAccessLevelAccessLevel);

                    var userExecutableAccessLevel = CreateVariable(folderAttributesAccessAll, attributesAccessAll + "UserExecutable", "UserExecutable", DataTypeIds.Double, ValueRanks.Scalar);
                    userExecutableAccessLevel.WriteMask = AttributeWriteMask.UserExecutable;
                    userExecutableAccessLevel.UserWriteMask = AttributeWriteMask.UserExecutable;
                    variables.Add(userExecutableAccessLevel);

                    var valueRankAccessLevel = CreateVariable(folderAttributesAccessAll, attributesAccessAll + "ValueRank", "ValueRank", DataTypeIds.Double, ValueRanks.Scalar);
                    valueRankAccessLevel.WriteMask = AttributeWriteMask.ValueRank;
                    valueRankAccessLevel.UserWriteMask = AttributeWriteMask.ValueRank;
                    variables.Add(valueRankAccessLevel);

                    var writeMaskAccessLevel = CreateVariable(folderAttributesAccessAll, attributesAccessAll + "WriteMask", "WriteMask", DataTypeIds.Double, ValueRanks.Scalar);
                    writeMaskAccessLevel.WriteMask = AttributeWriteMask.WriteMask;
                    writeMaskAccessLevel.UserWriteMask = AttributeWriteMask.WriteMask;
                    variables.Add(writeMaskAccessLevel);

                    var valueForVariableTypeAccessLevel = CreateVariable(folderAttributesAccessAll, attributesAccessAll + "ValueForVariableType", "ValueForVariableType", DataTypeIds.Double, ValueRanks.Scalar);
                    valueForVariableTypeAccessLevel.WriteMask = AttributeWriteMask.ValueForVariableType;
                    valueForVariableTypeAccessLevel.UserWriteMask = AttributeWriteMask.ValueForVariableType;
                    variables.Add(valueForVariableTypeAccessLevel);

                    var allAccessLevel = CreateVariable(folderAttributesAccessAll, attributesAccessAll + "All", "All", DataTypeIds.Double, ValueRanks.Scalar);
                    allAccessLevel.WriteMask = AttributeWriteMask.AccessLevel | AttributeWriteMask.ArrayDimensions | AttributeWriteMask.BrowseName | AttributeWriteMask.ContainsNoLoops | AttributeWriteMask.DataType |
                            AttributeWriteMask.Description | AttributeWriteMask.DisplayName | AttributeWriteMask.EventNotifier | AttributeWriteMask.Executable | AttributeWriteMask.Historizing | AttributeWriteMask.InverseName | AttributeWriteMask.IsAbstract |
                            AttributeWriteMask.MinimumSamplingInterval | AttributeWriteMask.NodeClass | AttributeWriteMask.NodeId | AttributeWriteMask.Symmetric | AttributeWriteMask.UserAccessLevel | AttributeWriteMask.UserExecutable |
                            AttributeWriteMask.UserWriteMask | AttributeWriteMask.ValueForVariableType | AttributeWriteMask.ValueRank | AttributeWriteMask.WriteMask;
                    allAccessLevel.UserWriteMask = AttributeWriteMask.AccessLevel | AttributeWriteMask.ArrayDimensions | AttributeWriteMask.BrowseName | AttributeWriteMask.ContainsNoLoops | AttributeWriteMask.DataType |
                            AttributeWriteMask.Description | AttributeWriteMask.DisplayName | AttributeWriteMask.EventNotifier | AttributeWriteMask.Executable | AttributeWriteMask.Historizing | AttributeWriteMask.InverseName | AttributeWriteMask.IsAbstract |
                            AttributeWriteMask.MinimumSamplingInterval | AttributeWriteMask.NodeClass | AttributeWriteMask.NodeId | AttributeWriteMask.Symmetric | AttributeWriteMask.UserAccessLevel | AttributeWriteMask.UserExecutable |
                            AttributeWriteMask.UserWriteMask | AttributeWriteMask.ValueForVariableType | AttributeWriteMask.ValueRank | AttributeWriteMask.WriteMask;
                    variables.Add(allAccessLevel);

                    // AccessUser1
                    var folderAttributesAccessUser1 = CreateFolder(folderAttributes, "Attributes_AccessUser1", "AccessUser1");
                    const string attributesAccessUser1 = "Attributes_AccessUser1_";

                    var accessLevelAccessUser1 = CreateVariable(folderAttributesAccessUser1, attributesAccessUser1 + "AccessLevel", "AccessLevel", DataTypeIds.Double, ValueRanks.Scalar);
                    accessLevelAccessAll.WriteMask = AttributeWriteMask.AccessLevel;
                    accessLevelAccessAll.UserWriteMask = AttributeWriteMask.AccessLevel;
                    variables.Add(accessLevelAccessAll);

                    var arrayDimensionsAccessUser1 = CreateVariable(folderAttributesAccessUser1, attributesAccessUser1 + "ArrayDimensions", "ArrayDimensions", DataTypeIds.Double, ValueRanks.Scalar);
                    arrayDimensionsAccessUser1.WriteMask = AttributeWriteMask.ArrayDimensions;
                    arrayDimensionsAccessUser1.UserWriteMask = AttributeWriteMask.ArrayDimensions;
                    variables.Add(arrayDimensionsAccessUser1);

                    var browseNameAccessUser1 = CreateVariable(folderAttributesAccessUser1, attributesAccessUser1 + "BrowseName", "BrowseName", DataTypeIds.Double, ValueRanks.Scalar);
                    browseNameAccessUser1.WriteMask = AttributeWriteMask.BrowseName;
                    browseNameAccessUser1.UserWriteMask = AttributeWriteMask.BrowseName;
                    variables.Add(browseNameAccessUser1);

                    var containsNoLoopsAccessUser1 = CreateVariable(folderAttributesAccessUser1, attributesAccessUser1 + "ContainsNoLoops", "ContainsNoLoops", DataTypeIds.Double, ValueRanks.Scalar);
                    containsNoLoopsAccessUser1.WriteMask = AttributeWriteMask.ContainsNoLoops;
                    containsNoLoopsAccessUser1.UserWriteMask = AttributeWriteMask.ContainsNoLoops;
                    variables.Add(containsNoLoopsAccessUser1);

                    var dataTypeAccessUser1 = CreateVariable(folderAttributesAccessUser1, attributesAccessUser1 + "DataType", "DataType", DataTypeIds.Double, ValueRanks.Scalar);
                    dataTypeAccessUser1.WriteMask = AttributeWriteMask.DataType;
                    dataTypeAccessUser1.UserWriteMask = AttributeWriteMask.DataType;
                    variables.Add(dataTypeAccessUser1);

                    var descriptionAccessUser1 = CreateVariable(folderAttributesAccessUser1, attributesAccessUser1 + "Description", "Description", DataTypeIds.Double, ValueRanks.Scalar);
                    descriptionAccessUser1.WriteMask = AttributeWriteMask.Description;
                    descriptionAccessUser1.UserWriteMask = AttributeWriteMask.Description;
                    variables.Add(descriptionAccessUser1);

                    var eventNotifierAccessUser1 = CreateVariable(folderAttributesAccessUser1, attributesAccessUser1 + "EventNotifier", "EventNotifier", DataTypeIds.Double, ValueRanks.Scalar);
                    eventNotifierAccessUser1.WriteMask = AttributeWriteMask.EventNotifier;
                    eventNotifierAccessUser1.UserWriteMask = AttributeWriteMask.EventNotifier;
                    variables.Add(eventNotifierAccessUser1);

                    var executableAccessUser1 = CreateVariable(folderAttributesAccessUser1, attributesAccessUser1 + "Executable", "Executable", DataTypeIds.Double, ValueRanks.Scalar);
                    executableAccessUser1.WriteMask = AttributeWriteMask.Executable;
                    executableAccessUser1.UserWriteMask = AttributeWriteMask.Executable;
                    variables.Add(executableAccessUser1);

                    var historizingAccessUser1 = CreateVariable(folderAttributesAccessUser1, attributesAccessUser1 + "Historizing", "Historizing", DataTypeIds.Double, ValueRanks.Scalar);
                    historizingAccessUser1.WriteMask = AttributeWriteMask.Historizing;
                    historizingAccessUser1.UserWriteMask = AttributeWriteMask.Historizing;
                    variables.Add(historizingAccessUser1);

                    var inverseNameAccessUser1 = CreateVariable(folderAttributesAccessUser1, attributesAccessUser1 + "InverseName", "InverseName", DataTypeIds.Double, ValueRanks.Scalar);
                    inverseNameAccessUser1.WriteMask = AttributeWriteMask.InverseName;
                    inverseNameAccessUser1.UserWriteMask = AttributeWriteMask.InverseName;
                    variables.Add(inverseNameAccessUser1);

                    var isAbstractAccessUser1 = CreateVariable(folderAttributesAccessUser1, attributesAccessUser1 + "IsAbstract", "IsAbstract", DataTypeIds.Double, ValueRanks.Scalar);
                    isAbstractAccessUser1.WriteMask = AttributeWriteMask.IsAbstract;
                    isAbstractAccessUser1.UserWriteMask = AttributeWriteMask.IsAbstract;
                    variables.Add(isAbstractAccessUser1);

                    var minimumSamplingIntervalAccessUser1 = CreateVariable(folderAttributesAccessUser1, attributesAccessUser1 + "MinimumSamplingInterval", "MinimumSamplingInterval", DataTypeIds.Double, ValueRanks.Scalar);
                    minimumSamplingIntervalAccessUser1.WriteMask = AttributeWriteMask.MinimumSamplingInterval;
                    minimumSamplingIntervalAccessUser1.UserWriteMask = AttributeWriteMask.MinimumSamplingInterval;
                    variables.Add(minimumSamplingIntervalAccessUser1);

                    var nodeClassIntervalAccessUser1 = CreateVariable(folderAttributesAccessUser1, attributesAccessUser1 + "NodeClass", "NodeClass", DataTypeIds.Double, ValueRanks.Scalar);
                    nodeClassIntervalAccessUser1.WriteMask = AttributeWriteMask.NodeClass;
                    nodeClassIntervalAccessUser1.UserWriteMask = AttributeWriteMask.NodeClass;
                    variables.Add(nodeClassIntervalAccessUser1);

                    var nodeIdAccessUser1 = CreateVariable(folderAttributesAccessUser1, attributesAccessUser1 + "NodeId", "NodeId", DataTypeIds.Double, ValueRanks.Scalar);
                    nodeIdAccessUser1.WriteMask = AttributeWriteMask.NodeId;
                    nodeIdAccessUser1.UserWriteMask = AttributeWriteMask.NodeId;
                    variables.Add(nodeIdAccessUser1);

                    var symmetricAccessUser1 = CreateVariable(folderAttributesAccessUser1, attributesAccessUser1 + "Symmetric", "Symmetric", DataTypeIds.Double, ValueRanks.Scalar);
                    symmetricAccessUser1.WriteMask = AttributeWriteMask.Symmetric;
                    symmetricAccessUser1.UserWriteMask = AttributeWriteMask.Symmetric;
                    variables.Add(symmetricAccessUser1);

                    var userAccessUser1AccessUser1 = CreateVariable(folderAttributesAccessUser1, attributesAccessUser1 + "UserAccessUser1", "UserAccessUser1", DataTypeIds.Double, ValueRanks.Scalar);
                    userAccessUser1AccessUser1.WriteMask = AttributeWriteMask.UserAccessLevel;
                    userAccessUser1AccessUser1.UserWriteMask = AttributeWriteMask.UserAccessLevel;
                    variables.Add(userAccessUser1AccessUser1);

                    var userExecutableAccessUser1 = CreateVariable(folderAttributesAccessUser1, attributesAccessUser1 + "UserExecutable", "UserExecutable", DataTypeIds.Double, ValueRanks.Scalar);
                    userExecutableAccessUser1.WriteMask = AttributeWriteMask.UserExecutable;
                    userExecutableAccessUser1.UserWriteMask = AttributeWriteMask.UserExecutable;
                    variables.Add(userExecutableAccessUser1);

                    var valueRankAccessUser1 = CreateVariable(folderAttributesAccessUser1, attributesAccessUser1 + "ValueRank", "ValueRank", DataTypeIds.Double, ValueRanks.Scalar);
                    valueRankAccessUser1.WriteMask = AttributeWriteMask.ValueRank;
                    valueRankAccessUser1.UserWriteMask = AttributeWriteMask.ValueRank;
                    variables.Add(valueRankAccessUser1);

                    var writeMaskAccessUser1 = CreateVariable(folderAttributesAccessUser1, attributesAccessUser1 + "WriteMask", "WriteMask", DataTypeIds.Double, ValueRanks.Scalar);
                    writeMaskAccessUser1.WriteMask = AttributeWriteMask.WriteMask;
                    writeMaskAccessUser1.UserWriteMask = AttributeWriteMask.WriteMask;
                    variables.Add(writeMaskAccessUser1);

                    var valueForVariableTypeAccessUser1 = CreateVariable(folderAttributesAccessUser1, attributesAccessUser1 + "ValueForVariableType", "ValueForVariableType", DataTypeIds.Double, ValueRanks.Scalar);
                    valueForVariableTypeAccessUser1.WriteMask = AttributeWriteMask.ValueForVariableType;
                    valueForVariableTypeAccessUser1.UserWriteMask = AttributeWriteMask.ValueForVariableType;
                    variables.Add(valueForVariableTypeAccessUser1);

                    var allAccessUser1 = CreateVariable(folderAttributesAccessUser1, attributesAccessUser1 + "All", "All", DataTypeIds.Double, ValueRanks.Scalar);
                    allAccessUser1.WriteMask = AttributeWriteMask.AccessLevel | AttributeWriteMask.ArrayDimensions | AttributeWriteMask.BrowseName | AttributeWriteMask.ContainsNoLoops | AttributeWriteMask.DataType |
                            AttributeWriteMask.Description | AttributeWriteMask.DisplayName | AttributeWriteMask.EventNotifier | AttributeWriteMask.Executable | AttributeWriteMask.Historizing | AttributeWriteMask.InverseName | AttributeWriteMask.IsAbstract |
                            AttributeWriteMask.MinimumSamplingInterval | AttributeWriteMask.NodeClass | AttributeWriteMask.NodeId | AttributeWriteMask.Symmetric | AttributeWriteMask.UserAccessLevel | AttributeWriteMask.UserExecutable |
                            AttributeWriteMask.UserWriteMask | AttributeWriteMask.ValueForVariableType | AttributeWriteMask.ValueRank | AttributeWriteMask.WriteMask;
                    allAccessUser1.UserWriteMask = AttributeWriteMask.AccessLevel | AttributeWriteMask.ArrayDimensions | AttributeWriteMask.BrowseName | AttributeWriteMask.ContainsNoLoops | AttributeWriteMask.DataType |
                            AttributeWriteMask.Description | AttributeWriteMask.DisplayName | AttributeWriteMask.EventNotifier | AttributeWriteMask.Executable | AttributeWriteMask.Historizing | AttributeWriteMask.InverseName | AttributeWriteMask.IsAbstract |
                            AttributeWriteMask.MinimumSamplingInterval | AttributeWriteMask.NodeClass | AttributeWriteMask.NodeId | AttributeWriteMask.Symmetric | AttributeWriteMask.UserAccessLevel | AttributeWriteMask.UserExecutable |
                            AttributeWriteMask.UserWriteMask | AttributeWriteMask.ValueForVariableType | AttributeWriteMask.ValueRank | AttributeWriteMask.WriteMask;
                    variables.Add(allAccessUser1);

                    // MyCompany
                    ResetRandomGenerator(21);
                    var myCompanyFolder = CreateFolder(root, "MyCompany", "MyCompany");
                    const string myCompany = "MyCompany_";

                    var myCompanyInstructions = CreateVariable(myCompanyFolder, myCompany + "Instructions", "Instructions", DataTypeIds.String, ValueRanks.Scalar);
                    myCompanyInstructions.Value = "A place for the vendor to describe their address-space.";
                    variables.Add(myCompanyInstructions);
                }
                catch (Exception e)
                {
                    Utils.LogError(e, "Error creating the ReferenceNodeManager address space.");
                }

                AddPredefinedNode(SystemContext, root);

                // reset random generator and generate boundary values
                ResetRandomGenerator(100, 1);
                _simulationTimer = new Timer(DoSimulation, null, 1000, 1000);
            }
        }

        private ServiceResult OnWriteInterval(ISystemContext context, NodeState node, ref object value)
        {
            try
            {
                _simulationInterval = (ushort)value;

                if (_simulationEnabled)
                {
                    _simulationTimer.Change(100, _simulationInterval);
                }

                return ServiceResult.Good;
            }
            catch (Exception e)
            {
                Utils.LogError(e, "Error writing Interval variable.");
                return ServiceResult.Create(e, StatusCodes.Bad, "Error writing Interval variable.");
            }
        }

        private ServiceResult OnWriteEnabled(ISystemContext context, NodeState node, ref object value)
        {
            try
            {
                _simulationEnabled = (bool)value;

                if (_simulationEnabled)
                {
                    _simulationTimer.Change(100, _simulationInterval);
                }
                else
                {
                    _simulationTimer.Change(100, 0);
                }

                return ServiceResult.Good;
            }
            catch (Exception e)
            {
                Utils.LogError(e, "Error writing Enabled variable.");
                return ServiceResult.Create(e, StatusCodes.Bad, "Error writing Enabled variable.");
            }
        }

        /// <summary>
        /// Creates a new folder.
        /// </summary>
        /// <param name="parent"></param>
        /// <param name="path"></param>
        /// <param name="name"></param>
        private FolderState CreateFolder(NodeState parent, string path, string name)
        {
            var folder = new FolderState(parent)
            {
                SymbolicName = name,
                ReferenceTypeId = ReferenceTypes.Organizes,
                TypeDefinitionId = ObjectTypeIds.FolderType,
                NodeId = new NodeId(path, NamespaceIndex),
                BrowseName = new QualifiedName(path, NamespaceIndex),
                DisplayName = new LocalizedText("en", name),
                WriteMask = AttributeWriteMask.None,
                UserWriteMask = AttributeWriteMask.None,
                EventNotifier = EventNotifiers.None
            };

            parent?.AddChild(folder);

            return folder;
        }

        /// <summary>
        /// Creates a new variable.
        /// </summary>
        /// <param name="parent"></param>
        /// <param name="path"></param>
        /// <param name="name"></param>
        /// <param name="peers"></param>
        private BaseDataVariableState CreateMeshVariable(NodeState parent, string path, string name, params NodeState[] peers)
        {
            var variable = CreateVariable(parent, path, name, BuiltInType.Double, ValueRanks.Scalar);

            if (peers != null)
            {
                foreach (var peer in peers)
                {
                    peer.AddReference(ReferenceTypes.HasCause, false, variable.NodeId);
                    variable.AddReference(ReferenceTypes.HasCause, true, peer.NodeId);
                    peer.AddReference(ReferenceTypes.HasEffect, true, variable.NodeId);
                    variable.AddReference(ReferenceTypes.HasEffect, false, peer.NodeId);
                }
            }

            return variable;
        }

        /// <summary>
        /// Creates a new variable.
        /// </summary>
        /// <param name="parent"></param>
        /// <param name="path"></param>
        /// <param name="name"></param>
        /// <param name="dataType"></param>
        /// <param name="valueRank"></param>
        private DataItemState CreateDataItemVariable(NodeState parent, string path, string name, BuiltInType dataType, int valueRank)
        {
            var variable = new DataItemState(parent);
            variable.ValuePrecision = new PropertyState<double>(variable);
            variable.Definition = new PropertyState<string>(variable);

            variable.Create(
                SystemContext,
                null,
                variable.BrowseName,
                null,
                true);

            variable.SymbolicName = name;
            variable.ReferenceTypeId = ReferenceTypes.Organizes;
            variable.NodeId = new NodeId(path, NamespaceIndex);
            variable.BrowseName = new QualifiedName(path, NamespaceIndex);
            variable.DisplayName = new LocalizedText("en", name);
            variable.WriteMask = AttributeWriteMask.None;
            variable.UserWriteMask = AttributeWriteMask.None;
            variable.DataType = (uint)dataType;
            variable.ValueRank = valueRank;
            variable.AccessLevel = AccessLevels.CurrentReadOrWrite;
            variable.UserAccessLevel = AccessLevels.CurrentReadOrWrite;
            variable.Historizing = false;
            variable.Value = TypeInfo.GetDefaultValue((uint)dataType, valueRank, Server.TypeTree);
            variable.StatusCode = StatusCodes.Good;
            variable.Timestamp = DateTime.UtcNow;

            if (valueRank == ValueRanks.OneDimension)
            {
                variable.ArrayDimensions = new ReadOnlyList<uint>([0]);
            }
            else if (valueRank == ValueRanks.TwoDimensions)
            {
                variable.ArrayDimensions = new ReadOnlyList<uint>([0, 0]);
            }

            variable.ValuePrecision.Value = 2;
            variable.ValuePrecision.AccessLevel = AccessLevels.CurrentReadOrWrite;
            variable.ValuePrecision.UserAccessLevel = AccessLevels.CurrentReadOrWrite;
            variable.Definition.Value = string.Empty;
            variable.Definition.AccessLevel = AccessLevels.CurrentReadOrWrite;
            variable.Definition.UserAccessLevel = AccessLevels.CurrentReadOrWrite;

            parent?.AddChild(variable);

            return variable;
        }

        /// <summary>
        /// Creates a new variable.
        /// </summary>
        /// <param name="parent"></param>
        /// <param name="path"></param>
        /// <param name="name"></param>
        /// <param name="dataType"></param>
        /// <param name="valueRank"></param>
        private AnalogItemState CreateAnalogItemVariable(NodeState parent, string path, string name, BuiltInType dataType, int valueRank)
        {
            return CreateAnalogItemVariable(parent, path, name, dataType, valueRank, null);
        }

        private AnalogItemState CreateAnalogItemVariable(NodeState parent, string path, string name, BuiltInType dataType, int valueRank, object initialValues)
        {
            return CreateAnalogItemVariable(parent, path, name, dataType, valueRank, initialValues, null);
        }

        private AnalogItemState CreateAnalogItemVariable(NodeState parent, string path, string name, BuiltInType dataType, int valueRank, object initialValues, Opc.Ua.Range customRange)
        {
            return CreateAnalogItemVariable(parent, path, name, (uint)dataType, valueRank, initialValues, customRange);
        }

        private AnalogItemState CreateAnalogItemVariable(NodeState parent, string path, string name, NodeId dataType, int valueRank, object initialValues, Opc.Ua.Range customRange)
        {
            var variable = new AnalogItemState(parent)
            {
                BrowseName = new QualifiedName(path, NamespaceIndex)
            };
            variable.EngineeringUnits = new PropertyState<EUInformation>(variable);
            variable.InstrumentRange = new PropertyState<Opc.Ua.Range>(variable);

            variable.Create(
                SystemContext,
                new NodeId(path, NamespaceIndex),
                variable.BrowseName,
                null,
                true);

            variable.NodeId = new NodeId(path, NamespaceIndex);
            variable.SymbolicName = name;
            variable.DisplayName = new LocalizedText("en", name);
            variable.WriteMask = AttributeWriteMask.None;
            variable.UserWriteMask = AttributeWriteMask.None;
            variable.ReferenceTypeId = ReferenceTypes.Organizes;
            variable.DataType = dataType;
            variable.ValueRank = valueRank;
            variable.AccessLevel = AccessLevels.CurrentReadOrWrite;
            variable.UserAccessLevel = AccessLevels.CurrentReadOrWrite;
            variable.Historizing = false;

            if (valueRank == ValueRanks.OneDimension)
            {
                variable.ArrayDimensions = new ReadOnlyList<uint>([0]);
            }
            else if (valueRank == ValueRanks.TwoDimensions)
            {
                variable.ArrayDimensions = new ReadOnlyList<uint>([0, 0]);
            }

            var builtInType = TypeInfo.GetBuiltInType(dataType, Server.TypeTree);

            // Simulate a mV Voltmeter
            var newRange = GetAnalogRange(builtInType);
            // Using anything but 120,-10 fails a few tests
            newRange.High = Math.Min(newRange.High, 120);
            newRange.Low = Math.Max(newRange.Low, -10);
            variable.InstrumentRange.Value = newRange;

            variable.EURange.Value = customRange ?? new Opc.Ua.Range(100, 0);

            variable.Value = initialValues ?? TypeInfo.GetDefaultValue(dataType, valueRank, Server.TypeTree);

            variable.StatusCode = StatusCodes.Good;
            variable.Timestamp = DateTime.UtcNow;
            // The latest UNECE version (Rev 11, published in 2015) is available here:
            // http://www.opcfoundation.org/UA/EngineeringUnits/UNECE/rec20_latest_08052015.zip
            variable.EngineeringUnits.Value = new EUInformation("mV", "millivolt", "http://www.opcfoundation.org/UA/units/un/cefact")
            {
                // The mapping of the UNECE codes to OPC UA(EUInformation.unitId) is available here:
                // http://www.opcfoundation.org/UA/EngineeringUnits/UNECE/UNECE_to_OPCUA.csv
                UnitId = 12890 // "2Z"
            };
            variable.OnWriteValue = OnWriteAnalog;
            variable.EURange.OnWriteValue = OnWriteAnalogRange;
            variable.EURange.AccessLevel = AccessLevels.CurrentReadOrWrite;
            variable.EURange.UserAccessLevel = AccessLevels.CurrentReadOrWrite;
            variable.EngineeringUnits.AccessLevel = AccessLevels.CurrentReadOrWrite;
            variable.EngineeringUnits.UserAccessLevel = AccessLevels.CurrentReadOrWrite;
            variable.InstrumentRange.OnWriteValue = OnWriteAnalogRange;
            variable.InstrumentRange.AccessLevel = AccessLevels.CurrentReadOrWrite;
            variable.InstrumentRange.UserAccessLevel = AccessLevels.CurrentReadOrWrite;

            parent?.AddChild(variable);

            return variable;
        }

        /// <summary>
        /// Creates a new variable.
        /// </summary>
        /// <param name="parent"></param>
        /// <param name="path"></param>
        /// <param name="name"></param>
        /// <param name="trueState"></param>
        /// <param name="falseState"></param>
        private TwoStateDiscreteState CreateTwoStateDiscreteItemVariable(NodeState parent, string path, string name, string trueState, string falseState)
        {
            var variable = new TwoStateDiscreteState(parent)
            {
                NodeId = new NodeId(path, NamespaceIndex),
                BrowseName = new QualifiedName(path, NamespaceIndex),
                DisplayName = new LocalizedText("en", name),
                WriteMask = AttributeWriteMask.None,
                UserWriteMask = AttributeWriteMask.None
            };

            variable.Create(
                SystemContext,
                null,
                variable.BrowseName,
                null,
                true);

            variable.SymbolicName = name;
            variable.ReferenceTypeId = ReferenceTypes.Organizes;
            variable.DataType = DataTypeIds.Boolean;
            variable.ValueRank = ValueRanks.Scalar;
            variable.AccessLevel = AccessLevels.CurrentReadOrWrite;
            variable.UserAccessLevel = AccessLevels.CurrentReadOrWrite;
            variable.Historizing = false;
            variable.Value = (bool)GetNewValue(variable);
            variable.StatusCode = StatusCodes.Good;
            variable.Timestamp = DateTime.UtcNow;

            variable.TrueState.Value = trueState;
            variable.TrueState.AccessLevel = AccessLevels.CurrentReadOrWrite;
            variable.TrueState.UserAccessLevel = AccessLevels.CurrentReadOrWrite;

            variable.FalseState.Value = falseState;
            variable.FalseState.AccessLevel = AccessLevels.CurrentReadOrWrite;
            variable.FalseState.UserAccessLevel = AccessLevels.CurrentReadOrWrite;

            parent?.AddChild(variable);

            return variable;
        }

        /// <summary>
        /// Creates a new variable.
        /// </summary>
        /// <param name="parent"></param>
        /// <param name="path"></param>
        /// <param name="name"></param>
        /// <param name="values"></param>
        private MultiStateDiscreteState CreateMultiStateDiscreteItemVariable(NodeState parent, string path, string name, params string[] values)
        {
            var variable = new MultiStateDiscreteState(parent)
            {
                NodeId = new NodeId(path, NamespaceIndex),
                BrowseName = new QualifiedName(path, NamespaceIndex),
                DisplayName = new LocalizedText("en", name),
                WriteMask = AttributeWriteMask.None,
                UserWriteMask = AttributeWriteMask.None
            };

            variable.Create(
                SystemContext,
                null,
                variable.BrowseName,
                null,
                true);

            variable.SymbolicName = name;
            variable.ReferenceTypeId = ReferenceTypes.Organizes;
            variable.DataType = DataTypeIds.UInt32;
            variable.ValueRank = ValueRanks.Scalar;
            variable.AccessLevel = AccessLevels.CurrentReadOrWrite;
            variable.UserAccessLevel = AccessLevels.CurrentReadOrWrite;
            variable.Historizing = false;
            variable.Value = (uint)0;
            variable.StatusCode = StatusCodes.Good;
            variable.Timestamp = DateTime.UtcNow;
            variable.OnWriteValue = OnWriteDiscrete;

            var strings = new LocalizedText[values.Length];

            for (var ii = 0; ii < strings.Length; ii++)
            {
                strings[ii] = values[ii];
            }

            variable.EnumStrings.Value = strings;
            variable.EnumStrings.AccessLevel = AccessLevels.CurrentReadOrWrite;
            variable.EnumStrings.UserAccessLevel = AccessLevels.CurrentReadOrWrite;

            parent?.AddChild(variable);

            return variable;
        }

        /// <summary>
        /// Creates a new UInt32 variable.
        /// </summary>
        /// <param name="parent"></param>
        /// <param name="path"></param>
        /// <param name="name"></param>
        /// <param name="enumNames"></param>
        private MultiStateValueDiscreteState CreateMultiStateValueDiscreteItemVariable(NodeState parent, string path, string name, params string[] enumNames)
        {
            return CreateMultiStateValueDiscreteItemVariable(parent, path, name, null, enumNames);
        }

        /// <summary>
        /// Creates a new variable.
        /// </summary>
        /// <param name="parent"></param>
        /// <param name="path"></param>
        /// <param name="name"></param>
        /// <param name="nodeId"></param>
        /// <param name="enumNames"></param>
        private MultiStateValueDiscreteState CreateMultiStateValueDiscreteItemVariable(NodeState parent, string path, string name, NodeId nodeId, params string[] enumNames)
        {
            var variable = new MultiStateValueDiscreteState(parent)
            {
                NodeId = new NodeId(path, NamespaceIndex),
                BrowseName = new QualifiedName(path, NamespaceIndex),
                DisplayName = new LocalizedText("en", name),
                WriteMask = AttributeWriteMask.None,
                UserWriteMask = AttributeWriteMask.None
            };

            variable.Create(
                SystemContext,
                null,
                variable.BrowseName,
                null,
                true);

            variable.SymbolicName = name;
            variable.ReferenceTypeId = ReferenceTypes.Organizes;
            variable.DataType = nodeId ?? DataTypeIds.UInt32;
            variable.ValueRank = ValueRanks.Scalar;
            variable.AccessLevel = AccessLevels.CurrentReadOrWrite;
            variable.UserAccessLevel = AccessLevels.CurrentReadOrWrite;
            variable.Historizing = false;
            variable.Value = (uint)0;
            variable.StatusCode = StatusCodes.Good;
            variable.Timestamp = DateTime.UtcNow;
            variable.OnWriteValue = OnWriteValueDiscrete;

            // there are two enumerations for this type:
            // EnumStrings = the string representations for enumerated values
            // ValueAsText = the actual enumerated value

            // set the enumerated strings
            var strings = new LocalizedText[enumNames.Length];
            for (var ii = 0; ii < strings.Length; ii++)
            {
                strings[ii] = enumNames[ii];
            }

            // set the enumerated values
            var values = new EnumValueType[enumNames.Length];
            for (var ii = 0; ii < values.Length; ii++)
            {
                values[ii] = new EnumValueType
                {
                    Value = ii,
                    Description = strings[ii],
                    DisplayName = strings[ii]
                };
            }
            variable.EnumValues.Value = values;
            variable.EnumValues.AccessLevel = AccessLevels.CurrentReadOrWrite;
            variable.EnumValues.UserAccessLevel = AccessLevels.CurrentReadOrWrite;
            variable.ValueAsText.Value = variable.EnumValues.Value[0].DisplayName;

            parent?.AddChild(variable);

            return variable;
        }

        private ServiceResult OnWriteDiscrete(
            ISystemContext context,
            NodeState node,
            NumericRange indexRange,
            QualifiedName dataEncoding,
            ref object value,
            ref StatusCode statusCode,
            ref DateTime timestamp)
        {
            var variable = node as MultiStateDiscreteState;

            // verify data type.
            var typeInfo = TypeInfo.IsInstanceOfDataType(
                value,
                variable.DataType,
                variable.ValueRank,
                context.NamespaceUris,
                context.TypeTable);

            if (typeInfo == null || typeInfo == TypeInfo.Unknown)
            {
                return StatusCodes.BadTypeMismatch;
            }

            if (indexRange != NumericRange.Empty)
            {
                return StatusCodes.BadIndexRangeInvalid;
            }

            var number = Convert.ToDouble(value, CultureInfo.InvariantCulture);

            if (number >= variable.EnumStrings.Value.Length || number < 0)
            {
                return StatusCodes.BadOutOfRange;
            }

            return ServiceResult.Good;
        }

        private ServiceResult OnWriteValueDiscrete(
            ISystemContext context,
            NodeState node,
            NumericRange indexRange,
            QualifiedName dataEncoding,
            ref object value,
            ref StatusCode statusCode,
            ref DateTime timestamp)
        {
            var typeInfo = TypeInfo.Construct(value);

            if (node is not MultiStateValueDiscreteState variable ||
                typeInfo == null ||
                typeInfo == TypeInfo.Unknown ||
                !TypeInfo.IsNumericType(typeInfo.BuiltInType))
            {
                return StatusCodes.BadTypeMismatch;
            }

            if (indexRange != NumericRange.Empty)
            {
                return StatusCodes.BadIndexRangeInvalid;
            }

            var number = Convert.ToInt32(value, CultureInfo.InvariantCulture);
            if (number >= variable.EnumValues.Value.Length || number < 0)
            {
                return StatusCodes.BadOutOfRange;
            }

            if (!node.SetChildValue(context, BrowseNames.ValueAsText, variable.EnumValues.Value[number].DisplayName, true))
            {
                return StatusCodes.BadOutOfRange;
            }

            node.ClearChangeMasks(context, true);

            return ServiceResult.Good;
        }

        private ServiceResult OnWriteAnalog(
            ISystemContext context,
            NodeState node,
            NumericRange indexRange,
            QualifiedName dataEncoding,
            ref object value,
            ref StatusCode statusCode,
            ref DateTime timestamp)
        {
            var variable = node as AnalogItemState;

            // verify data type.
            var typeInfo = TypeInfo.IsInstanceOfDataType(
                value,
                variable.DataType,
                variable.ValueRank,
                context.NamespaceUris,
                context.TypeTable);

            if (typeInfo == null || typeInfo == TypeInfo.Unknown)
            {
                return StatusCodes.BadTypeMismatch;
            }

            // check index range.
            if (variable.ValueRank >= 0)
            {
                if (indexRange != NumericRange.Empty)
                {
                    var target = variable.Value;
                    ServiceResult result = indexRange.UpdateRange(ref target, value);

                    if (ServiceResult.IsBad(result))
                    {
                        return result;
                    }

                    value = target;
                }
            }

            // check instrument range.
            else
            {
                if (indexRange != NumericRange.Empty)
                {
                    return StatusCodes.BadIndexRangeInvalid;
                }

                var number = Convert.ToDouble(value, CultureInfo.InvariantCulture);

                if (variable.InstrumentRange != null && (number < variable.InstrumentRange.Value.Low || number > variable.InstrumentRange.Value.High))
                {
                    return StatusCodes.BadOutOfRange;
                }
            }

            return ServiceResult.Good;
        }

        private ServiceResult OnWriteAnalogRange(
            ISystemContext context,
            NodeState node,
            NumericRange indexRange,
            QualifiedName dataEncoding,
            ref object value,
            ref StatusCode statusCode,
            ref DateTime timestamp)
        {
            var typeInfo = TypeInfo.Construct(value);

            if (node is not PropertyState<Opc.Ua.Range> variable ||
                value is not ExtensionObject extensionObject ||
                typeInfo == null ||
                typeInfo == TypeInfo.Unknown)
            {
                return StatusCodes.BadTypeMismatch;
            }

            if (extensionObject.Body is not Opc.Ua.Range newRange ||
                variable.Parent is not AnalogItemState parent)
            {
                return StatusCodes.BadTypeMismatch;
            }

            if (indexRange != NumericRange.Empty)
            {
                return StatusCodes.BadIndexRangeInvalid;
            }

            var parentTypeInfo = TypeInfo.Construct(parent.Value);
            var parentRange = GetAnalogRange(parentTypeInfo.BuiltInType);
            if (parentRange.High < newRange.High ||
                parentRange.Low > newRange.Low)
            {
                return StatusCodes.BadOutOfRange;
            }

            value = newRange;

            return ServiceResult.Good;
        }

        /// <summary>
        /// Creates a new variable.
        /// </summary>
        /// <param name="parent"></param>
        /// <param name="path"></param>
        /// <param name="name"></param>
        /// <param name="dataType"></param>
        /// <param name="valueRank"></param>
        private BaseDataVariableState CreateVariable(NodeState parent, string path, string name, BuiltInType dataType, int valueRank)
        {
            return CreateVariable(parent, path, name, (uint)dataType, valueRank);
        }

        /// <summary>
        /// Creates a new variable.
        /// </summary>
        /// <param name="parent"></param>
        /// <param name="path"></param>
        /// <param name="name"></param>
        /// <param name="dataType"></param>
        /// <param name="valueRank"></param>
        private BaseDataVariableState CreateVariable(NodeState parent, string path, string name, NodeId dataType, int valueRank)
        {
            var variable = new BaseDataVariableState(parent)
            {
                SymbolicName = name,
                ReferenceTypeId = ReferenceTypes.Organizes,
                TypeDefinitionId = VariableTypeIds.BaseDataVariableType,
                NodeId = new NodeId(path, NamespaceIndex),
                BrowseName = new QualifiedName(path, NamespaceIndex),
                DisplayName = new LocalizedText("en", name),
                WriteMask = AttributeWriteMask.DisplayName | AttributeWriteMask.Description,
                UserWriteMask = AttributeWriteMask.DisplayName | AttributeWriteMask.Description,
                DataType = dataType,
                ValueRank = valueRank,
                AccessLevel = AccessLevels.CurrentReadOrWrite,
                UserAccessLevel = AccessLevels.CurrentReadOrWrite,
                Historizing = false
            };
            variable.Value = GetNewValue(variable);
            variable.StatusCode = StatusCodes.Good;
            variable.Timestamp = DateTime.UtcNow;

            if (valueRank == ValueRanks.OneDimension)
            {
                variable.ArrayDimensions = new ReadOnlyList<uint>([0]);
            }
            else if (valueRank == ValueRanks.TwoDimensions)
            {
                variable.ArrayDimensions = new ReadOnlyList<uint>([0, 0]);
            }

            parent?.AddChild(variable);

            return variable;
        }

        private BaseDataVariableState[] CreateVariables(NodeState parent, string path, string name, BuiltInType dataType, int valueRank, ushort numVariables)
        {
            return CreateVariables(parent, path, name, (uint)dataType, valueRank, numVariables);
        }

        private BaseDataVariableState[] CreateVariables(NodeState parent, string path, string name, NodeId dataType, int valueRank, ushort numVariables)
        {
            // first, create a new Parent folder for this data-type
            var newParentFolder = CreateFolder(parent, path, name);

            var itemsCreated = new List<BaseDataVariableState>();
            // now to create the remaining NUMBERED items
            for (uint i = 0; i < numVariables; i++)
            {
                var newName = string.Format(CultureInfo.InvariantCulture, "{0}_{1}", name, i.ToString("00", CultureInfo.InvariantCulture));
                var newPath = string.Format(CultureInfo.InvariantCulture, "{0}_{1}", path, newName);
                itemsCreated.Add(CreateVariable(newParentFolder, newPath, newName, dataType, valueRank));
            }
            return [.. itemsCreated];
        }

        /// <summary>
        /// Creates a new variable.
        /// </summary>
        /// <param name="parent"></param>
        /// <param name="path"></param>
        /// <param name="name"></param>
        /// <param name="dataType"></param>
        /// <param name="valueRank"></param>
        private BaseDataVariableState CreateDynamicVariable(NodeState parent, string path, string name, BuiltInType dataType, int valueRank)
        {
            return CreateDynamicVariable(parent, path, name, (uint)dataType, valueRank);
        }

        /// <summary>
        /// Creates a new variable.
        /// </summary>
        /// <param name="parent"></param>
        /// <param name="path"></param>
        /// <param name="name"></param>
        /// <param name="dataType"></param>
        /// <param name="valueRank"></param>
        private BaseDataVariableState CreateDynamicVariable(NodeState parent, string path, string name, NodeId dataType, int valueRank)
        {
            var variable = CreateVariable(parent, path, name, dataType, valueRank);
            _dynamicNodes.Add(variable);
            return variable;
        }

        private BaseDataVariableState[] CreateDynamicVariables(NodeState parent, string path, string name, BuiltInType dataType, int valueRank, uint numVariables)
        {
            return CreateDynamicVariables(parent, path, name, (uint)dataType, valueRank, numVariables);
        }

        private BaseDataVariableState[] CreateDynamicVariables(NodeState parent, string path, string name, NodeId dataType, int valueRank, uint numVariables)
        {
            // first, create a new Parent folder for this data-type
            var newParentFolder = CreateFolder(parent, path, name);

            var itemsCreated = new List<BaseDataVariableState>();
            // now to create the remaining NUMBERED items
            for (uint i = 0; i < numVariables; i++)
            {
                var newName = string.Format(CultureInfo.InvariantCulture, "{0}_{1}", name, i.ToString("00", CultureInfo.InvariantCulture));
                var newPath = string.Format(CultureInfo.InvariantCulture, "{0}_{1}", path, newName);
                itemsCreated.Add(CreateDynamicVariable(newParentFolder, newPath, newName, dataType, valueRank));
            }//for i
            return [.. itemsCreated];
        }

        /// <summary>
        /// Creates a new view.
        /// </summary>
        /// <param name="parent"></param>
        /// <param name="externalReferences"></param>
        /// <param name="path"></param>
        /// <param name="name"></param>
        private ViewState CreateView(NodeState parent, IDictionary<NodeId, IList<IReference>> externalReferences, string path, string name)
        {
            var type = new ViewState
            {
                SymbolicName = name,
                NodeId = new NodeId(path, NamespaceIndex),
                BrowseName = new QualifiedName(name, NamespaceIndex)
            };
            type.DisplayName = type.BrowseName.Name;
            type.WriteMask = AttributeWriteMask.None;
            type.UserWriteMask = AttributeWriteMask.None;
            type.ContainsNoLoops = true;

            if (!externalReferences.TryGetValue(ObjectIds.ViewsFolder, out var references))
            {
                externalReferences[ObjectIds.ViewsFolder] = references = [];
            }

            type.AddReference(ReferenceTypeIds.Organizes, true, ObjectIds.ViewsFolder);
            references.Add(new NodeStateReference(ReferenceTypeIds.Organizes, false, type.NodeId));

            if (parent != null)
            {
                parent.AddReference(ReferenceTypes.Organizes, false, type.NodeId);
                type.AddReference(ReferenceTypes.Organizes, true, parent.NodeId);
            }

            AddPredefinedNode(SystemContext, type);
            return type;
        }

        /// <summary>
        /// Creates a new method.
        /// </summary>
        /// <param name="parent"></param>
        /// <param name="path"></param>
        /// <param name="name"></param>
        private MethodState CreateMethod(NodeState parent, string path, string name)
        {
            var method = new MethodState(parent)
            {
                SymbolicName = name,
                ReferenceTypeId = ReferenceTypeIds.HasComponent,
                NodeId = new NodeId(path, NamespaceIndex),
                BrowseName = new QualifiedName(path, NamespaceIndex),
                DisplayName = new LocalizedText("en", name),
                WriteMask = AttributeWriteMask.None,
                UserWriteMask = AttributeWriteMask.None,
                Executable = true,
                UserExecutable = true
            };

            parent?.AddChild(method);

            return method;
        }

        private ServiceResult OnVoidCall(
            ISystemContext context,
            MethodState method,
            IList<object> inputArguments,
            IList<object> outputArguments)
        {
            return ServiceResult.Good;
        }

        private ServiceResult OnAddCall(
            ISystemContext context,
            MethodState method,
            IList<object> inputArguments,
            IList<object> outputArguments)
        {
            // all arguments must be provided.
            if (inputArguments.Count < 2)
            {
                return StatusCodes.BadArgumentsMissing;
            }

            try
            {
                var floatValue = (float)inputArguments[0];
                var uintValue = (uint)inputArguments[1];

                // set output parameter
                outputArguments[0] = floatValue + uintValue;
                return ServiceResult.Good;
            }
            catch
            {
                return new ServiceResult(StatusCodes.BadInvalidArgument);
            }
        }

        private ServiceResult OnMultiplyCall(
            ISystemContext context,
            MethodState method,
            IList<object> inputArguments,
            IList<object> outputArguments)
        {
            // all arguments must be provided.
            if (inputArguments.Count < 2)
            {
                return StatusCodes.BadArgumentsMissing;
            }

            try
            {
                var op1 = (short)inputArguments[0];
                var op2 = (ushort)inputArguments[1];

                // set output parameter
                outputArguments[0] = op1 * op2;
                return ServiceResult.Good;
            }
            catch
            {
                return new ServiceResult(StatusCodes.BadInvalidArgument);
            }
        }

        private ServiceResult OnDivideCall(
            ISystemContext context,
            MethodState method,
            IList<object> inputArguments,
            IList<object> outputArguments)
        {
            // all arguments must be provided.
            if (inputArguments.Count < 2)
            {
                return StatusCodes.BadArgumentsMissing;
            }

            try
            {
                var op1 = (int)inputArguments[0];
                var op2 = (ushort)inputArguments[1];

                // set output parameter
                outputArguments[0] = op1 / (float)op2;
                return ServiceResult.Good;
            }
            catch
            {
                return new ServiceResult(StatusCodes.BadInvalidArgument);
            }
        }

        private ServiceResult OnSubstractCall(
            ISystemContext context,
            MethodState method,
            IList<object> inputArguments,
            IList<object> outputArguments)
        {
            // all arguments must be provided.
            if (inputArguments.Count < 2)
            {
                return StatusCodes.BadArgumentsMissing;
            }

            try
            {
                var op1 = (short)inputArguments[0];
                var op2 = (byte)inputArguments[1];

                // set output parameter
                outputArguments[0] = (short)(op1 - op2);
                return ServiceResult.Good;
            }
            catch
            {
                return new ServiceResult(StatusCodes.BadInvalidArgument);
            }
        }

        private ServiceResult OnHelloCall(
            ISystemContext context,
            MethodState method,
            IList<object> inputArguments,
            IList<object> outputArguments)
        {
            // all arguments must be provided.
            if (inputArguments.Count < 1)
            {
                return StatusCodes.BadArgumentsMissing;
            }

            try
            {
                var op1 = (string)inputArguments[0];

                // set output parameter
                outputArguments[0] = "hello " + op1;
                return ServiceResult.Good;
            }
            catch
            {
                return new ServiceResult(StatusCodes.BadInvalidArgument);
            }
        }

        private ServiceResult OnInputCall(
            ISystemContext context,
            MethodState method,
            IList<object> inputArguments,
            IList<object> outputArguments)
        {
            // all arguments must be provided.
            if (inputArguments.Count < 1)
            {
                return StatusCodes.BadArgumentsMissing;
            }

            return ServiceResult.Good;
        }

        private ServiceResult OnOutputCall(
            ISystemContext context,
            MethodState method,
            IList<object> inputArguments,
            IList<object> outputArguments)
        {
            // all arguments must be provided.
            try
            {
                // set output parameter
                outputArguments[0] = "Output";
                return ServiceResult.Good;
            }
            catch
            {
                return new ServiceResult(StatusCodes.BadInvalidArgument);
            }
        }

        private void ResetRandomGenerator(int seed, int boundaryValueFrequency = 0)
        {
            _randomSource = new Opc.Ua.Test.RandomSource(seed);
            _generator = new Opc.Ua.Test.TestDataGenerator(_randomSource)
            {
                BoundaryValueFrequency = boundaryValueFrequency
            };
        }

        private object GetNewValue(BaseVariableState variable)
        {
            Debug.Assert(_generator != null, "Need a random generator!");

            object value = null;
            for (var retryCount = 0; value == null && retryCount < 10; retryCount++)
            {
                value = _generator.GetRandom(variable.DataType, variable.ValueRank,
                    new uint[] { 10 }, Server.TypeTree);
                if (value is Variant variant && variant.Value == null)
                {
                    value = null;
                }
            }
            return value;
        }

        private void DoSimulation(object state)
        {
            try
            {
                lock (Lock)
                {
                    var timeStamp = DateTime.UtcNow;
                    foreach (var variable in _dynamicNodes)
                    {
                        variable.Value = GetNewValue(variable);
                        variable.Timestamp = timeStamp;
                        variable.ClearChangeMasks(SystemContext, false);
                    }
                }
            }
            catch (Exception e)
            {
                Utils.LogError(e, "Unexpected error doing simulation.");
            }
        }

        /// <summary>
        /// Frees any resources allocated for the address space.
        /// </summary>
        public override void DeleteAddressSpace()
        {
            lock (Lock)
            {
                // TBD
            }
        }

        /// <summary>
        /// Returns a unique handle for the node.
        /// </summary>
        /// <param name="context"></param>
        /// <param name="nodeId"></param>
        /// <param name="cache"></param>
        protected override NodeHandle GetManagerHandle(ServerSystemContext context,
            NodeId nodeId, IDictionary<NodeId, NodeState> cache)
        {
            lock (Lock)
            {
                // quickly exclude nodes that are not in the namespace.
                if (!IsNodeIdInNamespace(nodeId))
                {
                    return null;
                }

                if (!PredefinedNodes.TryGetValue(nodeId, out var node))
                {
                    return null;
                }

                return new NodeHandle
                {
                    NodeId = nodeId,
                    Node = node,
                    Validated = true
                };
            }
        }

        /// <summary>
        /// Verifies that the specified node exists.
        /// </summary>
        /// <param name="context"></param>
        /// <param name="handle"></param>
        /// <param name="cache"></param>
        protected override NodeState ValidateNode(
           ServerSystemContext context,
           NodeHandle handle,
           IDictionary<NodeId, NodeState> cache)
        {
            // not valid if no root.
            if (handle == null)
            {
                return null;
            }

            // check if previously validated.
            if (handle.Validated)
            {
                return handle.Node;
            }

            // TBD

            return null;
        }

        private Opc.Ua.Test.RandomSource _randomSource;
        private Opc.Ua.Test.TestDataGenerator _generator;
        private Timer _simulationTimer;
        private ushort _simulationInterval = 1000;
        private bool _simulationEnabled = true;
        private readonly List<BaseDataVariableState> _dynamicNodes;
    }

    public static class VariableExtensions
    {
        public static BaseDataVariableState MinimumSamplingInterval(this BaseDataVariableState variable, int minimumSamplingInterval)
        {
            variable.MinimumSamplingInterval = minimumSamplingInterval;
            return variable;
        }
    }
}
