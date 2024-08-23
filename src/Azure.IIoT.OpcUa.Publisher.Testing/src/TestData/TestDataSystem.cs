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

namespace TestData
{
    using Opc.Ua;
    using System;
    using System.Collections.Generic;
    using System.Threading;
    using System.Xml;

    public interface ITestDataSystemCallback
    {
        void OnDataChange(
            BaseVariableState variable,
            object value,
            StatusCode statusCode,
            DateTime timestamp);
    }

    public class TestDataSystem : IDisposable
    {
        public TestDataSystem(ITestDataSystemCallback callback,
            NamespaceTable namespaceUris, StringTable serverUris)
        {
            _callback = callback;
            _minimumSamplingInterval = int.MaxValue;
            _monitoredNodes = new Dictionary<uint, BaseVariableState>();
            _generator = new Opc.Ua.Test.TestDataGenerator()
            {
                NamespaceUris = namespaceUris,
                ServerUris = serverUris
            };
            _historyArchive = new HistoryArchive();
        }

        public void Dispose()
        {
            if (_historyArchive != null)
            {
                _historyArchive.Dispose();
                _historyArchive = null;
            }
            if (_timer != null)
            {
                _timer.Dispose();
                _timer = null;
            }
        }

        /// <summary>
        /// The number of nodes being monitored.
        /// </summary>
        public int MonitoredNodeCount
        {
            get
            {
                lock (_lock)
                {
                    if (_monitoredNodes == null)
                    {
                        return 0;
                    }

                    return _monitoredNodes.Count;
                }
            }
        }

        /// <summary>
        /// Creates an archive for the variable.
        /// </summary>
        /// <param name="variable"></param>
        public void EnableHistoryArchiving(BaseVariableState variable)
        {
            if (variable == null)
            {
                return;
            }

            if (variable.ValueRank == ValueRanks.Scalar)
            {
                _historyArchive.CreateRecord(variable.NodeId, TypeInfo.GetBuiltInType(variable.DataType));
            }
        }

        /// <summary>
        /// Returns a new value for the variable.
        /// </summary>
        /// <param name="variable"></param>
        public object ReadValue(BaseVariableState variable)
        {
            lock (_lock)
            {
                switch (variable.NumericId)
                {
                    case Variables.ScalarValueObjectType_BooleanValue:
                    case Variables.UserScalarValueObjectType_BooleanValue:
                        return _generator.GetRandom<bool>();
                    case Variables.ScalarValueObjectType_SByteValue:
                    case Variables.UserScalarValueObjectType_SByteValue:
                        return _generator.GetRandom<sbyte>();
                    case Variables.AnalogScalarValueObjectType_SByteValue:
                        return (sbyte)(((int)(_generator.GetRandom<uint>() % 201)) - 100);
                    case Variables.ScalarValueObjectType_ByteValue:
                    case Variables.UserScalarValueObjectType_ByteValue:
                        return _generator.GetRandom<byte>();
                    case Variables.AnalogScalarValueObjectType_ByteValue:
                        return (byte)((_generator.GetRandom<uint>() % 201) + 50);
                    case Variables.ScalarValueObjectType_Int16Value:
                    case Variables.UserScalarValueObjectType_Int16Value:
                        return _generator.GetRandom<short>();
                    case Variables.AnalogScalarValueObjectType_Int16Value:
                        return (short)(((int)(_generator.GetRandom<uint>() % 201)) - 100);
                    case Variables.ScalarValueObjectType_UInt16Value:
                    case Variables.UserScalarValueObjectType_UInt16Value:
                        return _generator.GetRandom<ushort>();
                    case Variables.AnalogScalarValueObjectType_UInt16Value:
                        return (ushort)((_generator.GetRandom<uint>() % 201) + 50);
                    case Variables.ScalarValueObjectType_Int32Value:
                    case Variables.UserScalarValueObjectType_Int32Value:
                        return _generator.GetRandom<int>();
                    case Variables.AnalogScalarValueObjectType_Int32Value:
                    case Variables.AnalogScalarValueObjectType_IntegerValue:
                        return ((int)(_generator.GetRandom<uint>() % 201)) - 100;
                    case Variables.ScalarValueObjectType_UInt32Value:
                    case Variables.UserScalarValueObjectType_UInt32Value:
                        return _generator.GetRandom<uint>();
                    case Variables.AnalogScalarValueObjectType_UInt32Value:
                    case Variables.AnalogScalarValueObjectType_UIntegerValue:
                        return (_generator.GetRandom<uint>() % 201) + 50;
                    case Variables.ScalarValueObjectType_Int64Value:
                    case Variables.UserScalarValueObjectType_Int64Value:
                        return _generator.GetRandom<long>();
                    case Variables.AnalogScalarValueObjectType_Int64Value:
                        return (long)(((int)(_generator.GetRandom<uint>() % 201)) - 100);
                    case Variables.ScalarValueObjectType_UInt64Value:
                    case Variables.UserScalarValueObjectType_UInt64Value:
                        return _generator.GetRandom<ulong>();
                    case Variables.AnalogScalarValueObjectType_UInt64Value:
                        return (ulong)((_generator.GetRandom<uint>() % 201) + 50);
                    case Variables.ScalarValueObjectType_FloatValue:
                    case Variables.UserScalarValueObjectType_FloatValue:
                        return _generator.GetRandom<float>();
                    case Variables.AnalogScalarValueObjectType_FloatValue:
                        return (float)(((int)(_generator.GetRandom<uint>() % 201)) - 100);
                    case Variables.ScalarValueObjectType_DoubleValue:
                    case Variables.UserScalarValueObjectType_DoubleValue:
                        return _generator.GetRandom<double>();
                    case Variables.AnalogScalarValueObjectType_DoubleValue:
                    case Variables.AnalogScalarValueObjectType_NumberValue:
                        return (double)(((int)(_generator.GetRandom<uint>() % 201)) - 100);
                    case Variables.ScalarValueObjectType_StringValue:
                    case Variables.UserScalarValueObjectType_StringValue:
                        return _generator.GetRandom<string>();
                    case Variables.ScalarValueObjectType_DateTimeValue:
                    case Variables.UserScalarValueObjectType_DateTimeValue:
                        return _generator.GetRandom<DateTime>();
                    case Variables.ScalarValueObjectType_GuidValue:
                    case Variables.UserScalarValueObjectType_GuidValue:
                        return _generator.GetRandom<Guid>();
                    case Variables.ScalarValueObjectType_ByteStringValue:
                    case Variables.UserScalarValueObjectType_ByteStringValue:
                        return _generator.GetRandom<byte[]>();
                    case Variables.ScalarValueObjectType_XmlElementValue:
                    case Variables.UserScalarValueObjectType_XmlElementValue:
                        return _generator.GetRandom<XmlElement>();
                    case Variables.ScalarValueObjectType_NodeIdValue:
                    case Variables.UserScalarValueObjectType_NodeIdValue:
                        return _generator.GetRandom<NodeId>();
                    case Variables.ScalarValueObjectType_ExpandedNodeIdValue:
                    case Variables.UserScalarValueObjectType_ExpandedNodeIdValue:
                        return _generator.GetRandom<ExpandedNodeId>();
                    case Variables.ScalarValueObjectType_QualifiedNameValue:
                    case Variables.UserScalarValueObjectType_QualifiedNameValue:
                        return _generator.GetRandom<QualifiedName>();
                    case Variables.ScalarValueObjectType_LocalizedTextValue:
                    case Variables.UserScalarValueObjectType_LocalizedTextValue:
                        return _generator.GetRandom<LocalizedText>();
                    case Variables.ScalarValueObjectType_StatusCodeValue:
                    case Variables.UserScalarValueObjectType_StatusCodeValue:
                        return _generator.GetRandom<StatusCode>();
                    case Variables.ScalarValueObjectType_VariantValue:
                    case Variables.UserScalarValueObjectType_VariantValue:
                        return _generator.GetRandomVariant().Value;
                    case Variables.ScalarValueObjectType_StructureValue:
                        return GetRandomStructure();
                    case Variables.ScalarValueObjectType_EnumerationValue:
                        return _generator.GetRandom<int>();
                    case Variables.ScalarValueObjectType_NumberValue:
                        return _generator.GetRandom(BuiltInType.Number);
                    case Variables.ScalarValueObjectType_IntegerValue:
                        return _generator.GetRandom(BuiltInType.Integer);
                    case Variables.ScalarValueObjectType_UIntegerValue:
                        return _generator.GetRandom(BuiltInType.UInteger);
                    case Variables.ArrayValueObjectType_BooleanValue:
                    case Variables.UserArrayValueObjectType_BooleanValue:
                        return _generator.GetRandomArray<bool>();
                    case Variables.ArrayValueObjectType_SByteValue:
                    case Variables.UserArrayValueObjectType_SByteValue:
                        return _generator.GetRandomArray<sbyte>();
                    case Variables.ArrayValueObjectType_ByteValue:
                    case Variables.UserArrayValueObjectType_ByteValue:
                        return _generator.GetRandomArray<byte>();
                    case Variables.ArrayValueObjectType_Int16Value:
                    case Variables.UserArrayValueObjectType_Int16Value:
                        return _generator.GetRandomArray<short>();
                    case Variables.ArrayValueObjectType_UInt16Value:
                    case Variables.UserArrayValueObjectType_UInt16Value:
                        return _generator.GetRandomArray<ushort>();
                    case Variables.ArrayValueObjectType_Int32Value:
                    case Variables.UserArrayValueObjectType_Int32Value:
                        return _generator.GetRandomArray<int>();
                    case Variables.ArrayValueObjectType_UInt32Value:
                    case Variables.UserArrayValueObjectType_UInt32Value:
                        return _generator.GetRandomArray<uint>();
                    case Variables.ArrayValueObjectType_Int64Value:
                    case Variables.UserArrayValueObjectType_Int64Value:
                        return _generator.GetRandomArray<long>();
                    case Variables.ArrayValueObjectType_UInt64Value:
                    case Variables.UserArrayValueObjectType_UInt64Value:
                        return _generator.GetRandomArray<ulong>();
                    case Variables.ArrayValueObjectType_FloatValue:
                    case Variables.UserArrayValueObjectType_FloatValue:
                        return _generator.GetRandomArray<float>();
                    case Variables.ArrayValueObjectType_DoubleValue:
                    case Variables.UserArrayValueObjectType_DoubleValue:
                        return _generator.GetRandomArray<double>();
                    case Variables.ArrayValueObjectType_StringValue:
                    case Variables.UserArrayValueObjectType_StringValue:
                        return _generator.GetRandomArray<string>();
                    case Variables.ArrayValueObjectType_DateTimeValue:
                    case Variables.UserArrayValueObjectType_DateTimeValue:
                        return _generator.GetRandomArray<DateTime>();
                    case Variables.ArrayValueObjectType_GuidValue:
                    case Variables.UserArrayValueObjectType_GuidValue:
                        return _generator.GetRandomArray<Guid>();
                    case Variables.ArrayValueObjectType_ByteStringValue:
                    case Variables.UserArrayValueObjectType_ByteStringValue:
                        return _generator.GetRandomArray<byte[]>();
                    case Variables.ArrayValueObjectType_XmlElementValue:
                    case Variables.UserArrayValueObjectType_XmlElementValue:
                        return _generator.GetRandomArray<XmlElement>();
                    case Variables.ArrayValueObjectType_NodeIdValue:
                    case Variables.UserArrayValueObjectType_NodeIdValue:
                        return _generator.GetRandomArray<NodeId>();
                    case Variables.ArrayValueObjectType_ExpandedNodeIdValue:
                    case Variables.UserArrayValueObjectType_ExpandedNodeIdValue:
                        return _generator.GetRandomArray<ExpandedNodeId>();
                    case Variables.ArrayValueObjectType_QualifiedNameValue:
                    case Variables.UserArrayValueObjectType_QualifiedNameValue:
                        return _generator.GetRandomArray<QualifiedName>();
                    case Variables.ArrayValueObjectType_LocalizedTextValue:
                    case Variables.UserArrayValueObjectType_LocalizedTextValue:
                        return _generator.GetRandomArray<LocalizedText>();
                    case Variables.ArrayValueObjectType_StatusCodeValue:
                    case Variables.UserArrayValueObjectType_StatusCodeValue:
                        return _generator.GetRandomArray<StatusCode>();
                    case Variables.ArrayValueObjectType_VariantValue:
                    case Variables.UserArrayValueObjectType_VariantValue:
                        return _generator.GetRandomArray<object>();
                    case Variables.ArrayValueObjectType_EnumerationValue:
                        return _generator.GetRandomArray<int>();
                    case Variables.ArrayValueObjectType_NumberValue:
                        return _generator.GetRandomArray(BuiltInType.Number, 100, false);
                    case Variables.ArrayValueObjectType_IntegerValue:
                        return _generator.GetRandomArray(BuiltInType.Integer, 100, false);
                    case Variables.ArrayValueObjectType_UIntegerValue:
                        return _generator.GetRandomArray(BuiltInType.UInteger, 100, false);
                    case Variables.AnalogArrayValueObjectType_SByteValue:
                        {
                            var values = _generator.GetRandomArray<sbyte>();
                            for (var i = 0; i < values.Length; i++)
                            {
                                values[i] = (sbyte)(((int)(_generator.GetRandom<uint>() % 201)) - 100);
                            }
                            return values;
                        }
                    case Variables.AnalogArrayValueObjectType_ByteValue:
                        {
                            var values = _generator.GetRandomArray<byte>();
                            for (var i = 0; i < values.Length; i++)
                            {
                                values[i] = (byte)((_generator.GetRandom<uint>() % 201) + 50);
                            }
                            return values;
                        }
                    case Variables.AnalogArrayValueObjectType_Int16Value:
                        {
                            var values = _generator.GetRandomArray<short>();
                            for (var i = 0; i < values.Length; i++)
                            {
                                values[i] = (short)(((int)(_generator.GetRandom<uint>() % 201)) - 100);
                            }
                            return values;
                        }
                    case Variables.AnalogArrayValueObjectType_UInt16Value:
                        {
                            var values = _generator.GetRandomArray<ushort>();
                            for (var i = 0; i < values.Length; i++)
                            {
                                values[i] = (ushort)((_generator.GetRandom<uint>() % 201) + 50);
                            }
                            return values;
                        }
                    case Variables.AnalogArrayValueObjectType_Int32Value:
                    case Variables.AnalogArrayValueObjectType_IntegerValue:
                        {
                            var values = _generator.GetRandomArray<int>();
                            for (var i = 0; i < values.Length; i++)
                            {
                                values[i] = ((int)(_generator.GetRandom<uint>() % 201)) - 100;
                            }
                            return values;
                        }
                    case Variables.AnalogArrayValueObjectType_UInt32Value:
                    case Variables.AnalogArrayValueObjectType_UIntegerValue:
                        {
                            var values = _generator.GetRandomArray<uint>();
                            for (var i = 0; i < values.Length; i++)
                            {
                                values[i] = (_generator.GetRandom<uint>() % 201) + 50;
                            }
                            return values;
                        }
                    case Variables.AnalogArrayValueObjectType_Int64Value:
                        {
                            var values = _generator.GetRandomArray<long>();
                            for (var i = 0; i < values.Length; i++)
                            {
                                values[i] = ((int)(_generator.GetRandom<uint>() % 201)) - 100;
                            }
                            return values;
                        }
                    case Variables.AnalogArrayValueObjectType_UInt64Value:
                        {
                            var values = _generator.GetRandomArray<ulong>();
                            for (var i = 0; i < values.Length; i++)
                            {
                                values[i] = (_generator.GetRandom<uint>() % 201) + 50;
                            }
                            return values;
                        }
                    case Variables.AnalogArrayValueObjectType_FloatValue:
                        {
                            var values = _generator.GetRandomArray<float>();
                            for (var i = 0; i < values.Length; i++)
                            {
                                values[i] = ((int)(_generator.GetRandom<uint>() % 201)) - 100;
                            }
                            return values;
                        }
                    case Variables.AnalogArrayValueObjectType_DoubleValue:
                    case Variables.AnalogArrayValueObjectType_NumberValue:
                        {
                            var values = _generator.GetRandomArray<double>();
                            for (var i = 0; i < values.Length; i++)
                            {
                                values[i] = ((int)(_generator.GetRandom<uint>() % 201)) - 100;
                            }
                            return values;
                        }
                    case Variables.ArrayValueObjectType_StructureValue:
                        {
                            var values = _generator.GetRandomArray<ExtensionObject>(10);
                            for (var i = 0; values != null && i < values.Length; i++)
                            {
                                values[i] = GetRandomStructure();
                            }
                            return values;
                        }
                }
                return null;
            }
        }

        /// <summary>
        /// Returns a random structure.
        /// </summary>
        private ExtensionObject GetRandomStructure()
        {
            if (_generator.GetRandomBoolean())
            {
                var scalar = new ScalarValueDataType
                {
                    BooleanValue = _generator.GetRandom<bool>(),
                    SByteValue = _generator.GetRandom<sbyte>(),
                    ByteValue = _generator.GetRandom<byte>(),
                    Int16Value = _generator.GetRandom<short>(),
                    UInt16Value = _generator.GetRandom<ushort>(),
                    Int32Value = _generator.GetRandom<int>(),
                    UInt32Value = _generator.GetRandom<uint>(),
                    Int64Value = _generator.GetRandom<long>(),
                    UInt64Value = _generator.GetRandom<ulong>(),
                    FloatValue = _generator.GetRandom<float>(),
                    DoubleValue = _generator.GetRandom<double>(),
                    StringValue = _generator.GetRandom<string>(),
                    DateTimeValue = _generator.GetRandom<DateTime>(),
                    GuidValue = _generator.GetRandom<Uuid>(),
                    ByteStringValue = _generator.GetRandom<byte[]>(),
                    XmlElementValue = _generator.GetRandom<XmlElement>(),
                    NodeIdValue = _generator.GetRandom<NodeId>(),
                    ExpandedNodeIdValue = _generator.GetRandom<ExpandedNodeId>(),
                    QualifiedNameValue = _generator.GetRandom<QualifiedName>(),
                    LocalizedTextValue = _generator.GetRandom<LocalizedText>(),
                    StatusCodeValue = _generator.GetRandom<StatusCode>(),
                    VariantValue = _generator.GetRandomVariant()
                };
                return new ExtensionObject(scalar);
            }
            var array = new ArrayValueDataType
            {
                BooleanValue = _generator.GetRandomArray<bool>(10),
                SByteValue = _generator.GetRandomArray<sbyte>(10),
                ByteValue = _generator.GetRandomArray<byte>(10),
                Int16Value = _generator.GetRandomArray<short>(10),
                UInt16Value = _generator.GetRandomArray<ushort>(10),
                Int32Value = _generator.GetRandomArray<int>(10),
                UInt32Value = _generator.GetRandomArray<uint>(10),
                Int64Value = _generator.GetRandomArray<long>(10),
                UInt64Value = _generator.GetRandomArray<ulong>(10),
                FloatValue = _generator.GetRandomArray<float>(10),
                DoubleValue = _generator.GetRandomArray<double>(10),
                StringValue = _generator.GetRandomArray<string>(10),
                DateTimeValue = _generator.GetRandomArray<DateTime>(10),
                GuidValue = _generator.GetRandomArray<Uuid>(10),
                ByteStringValue = _generator.GetRandomArray<byte[]>(10),
                XmlElementValue = _generator.GetRandomArray<XmlElement>(10),
                NodeIdValue = _generator.GetRandomArray<NodeId>(10),
                ExpandedNodeIdValue = _generator.GetRandomArray<ExpandedNodeId>(10),
                QualifiedNameValue = _generator.GetRandomArray<QualifiedName>(10),
                LocalizedTextValue = _generator.GetRandomArray<LocalizedText>(10),
                StatusCodeValue = _generator.GetRandomArray<StatusCode>(10)
            };

            var values = _generator.GetRandomArray<object>(10);
            for (var i = 0; values != null && i < values.Length; i++)
            {
                array.VariantValue.Add(new Variant(values[i]));
            }

            return new ExtensionObject(array.TypeId, array);
        }

        public void StartMonitoringValue(uint monitoredItemId,
            double samplingInterval, BaseVariableState variable)
        {
            lock (_lock)
            {
                _monitoredNodes ??= new Dictionary<uint, BaseVariableState>();
                _monitoredNodes[monitoredItemId] = variable;
                SetSamplingInterval(samplingInterval);
            }
        }

        public void SetSamplingInterval(double samplingInterval)
        {
            lock (_lock)
            {
                if (samplingInterval < 0)
                {
                    // _samplingEvent.Set();
                    _minimumSamplingInterval = int.MaxValue;

                    if (_timer != null)
                    {
                        _timer.Dispose();
                        _timer = null;
                    }

                    return;
                }

                if (_minimumSamplingInterval > samplingInterval)
                {
                    _minimumSamplingInterval = (int)samplingInterval;

                    if (_minimumSamplingInterval < 100)
                    {
                        _minimumSamplingInterval = 100;
                    }

                    if (_timer != null)
                    {
                        _timer.Dispose();
                        _timer = null;
                    }

                    _timer = new Timer(DoSample, null,
                        _minimumSamplingInterval, _minimumSamplingInterval);
                }
            }
        }

        private void DoSample(object state)
        {
            Utils.Trace("DoSample HiRes={0:ss.ffff} Now={1:ss.ffff}", HiResClock.UtcNow, DateTime.UtcNow);

            var samples = new Queue<Sample>();

            lock (_lock)
            {
                if (_monitoredNodes == null)
                {
                    return;
                }

                foreach (var variable in _monitoredNodes.Values)
                {
                    var sample = new Sample
                    {
                        Variable = variable
                    };
                    sample.Value = ReadValue(sample.Variable);
                    sample.StatusCode = StatusCodes.Good;
                    sample.Timestamp = DateTime.UtcNow;

                    samples.Enqueue(sample);
                }
            }

            while (samples.Count > 0)
            {
                var sample = samples.Dequeue();

                _callback.OnDataChange(
                    sample.Variable,
                    sample.Value,
                    sample.StatusCode,
                    sample.Timestamp);
            }
        }

        public void StopMonitoringValue(uint monitoredItemId)
        {
            lock (_lock)
            {
                if (_monitoredNodes == null)
                {
                    return;
                }

                _monitoredNodes.Remove(monitoredItemId);

                if (_monitoredNodes.Count == 0)
                {
                    SetSamplingInterval(-1);
                }
            }
        }

        private sealed class Sample
        {
            public BaseVariableState Variable;
            public object Value;
            public StatusCode StatusCode;
            public DateTime Timestamp;
        }

        private readonly object _lock = new();
        private readonly ITestDataSystemCallback _callback;
        private readonly Opc.Ua.Test.TestDataGenerator _generator;
        private int _minimumSamplingInterval;
        private Dictionary<uint, BaseVariableState> _monitoredNodes;
        private Timer _timer;
        private HistoryArchive _historyArchive;
    }
}
