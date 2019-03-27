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

using System;
using System.Collections.Generic;
using System.Threading;
using System.Xml;
using Opc.Ua;

namespace TestData {
    public interface ITestDataSystemCallback {
        void OnDataChange(
            BaseVariableState variable,
            object value,
            StatusCode statusCode,
            DateTime timestamp);
    }

    public class TestDataSystem : IDisposable {
        public TestDataSystem(ITestDataSystemCallback callback, NamespaceTable namespaceUris, StringTable serverUris) {
            _callback = callback;
            _minimumSamplingInterval = int.MaxValue;
            _monitoredNodes = new Dictionary<uint, BaseVariableState>();
            _generator = new Opc.Ua.Test.DataGenerator(null) {
                NamespaceUris = namespaceUris,
                ServerUris = serverUris
            };
            _historyArchive = new HistoryArchive();
        }

        public void Dispose() {
            if (_historyArchive != null) {
                _historyArchive.Dispose();
                _historyArchive = null;
            }
        }

        /// <summary>
        /// The number of nodes being monitored.
        /// </summary>
        public int MonitoredNodeCount {
            get {
                lock (_lock) {
                    if (_monitoredNodes == null) {
                        return 0;
                    }

                    return _monitoredNodes.Count;
                }
            }
        }

        /// <summary>
        /// Gets or sets the current system status.
        /// </summary>
        public StatusCode SystemStatus {
            get {
                lock (_lock) {
                    return _systemStatus;
                }
            }

            set {
                lock (_lock) {
                    _systemStatus = value;
                }
            }
        }

        /// <summary>
        /// Creates an archive for the variable.
        /// </summary>
        public void EnableHistoryArchiving(BaseVariableState variable) {
            if (variable == null) {
                return;
            }

            if (variable.ValueRank == ValueRanks.Scalar) {
                _historyArchive.CreateRecord(variable.NodeId, TypeInfo.GetBuiltInType(variable.DataType));
            }
        }

        /// <summary>
        /// Returns the history file for the variable.
        /// </summary>
        public IHistoryDataSource GetHistoryFile(BaseVariableState variable) {
            if (variable == null) {
                return null;
            }

            return _historyArchive.GetHistoryFile(variable.NodeId);
        }

        /// <summary>
        /// Returns a new value for the variable.
        /// </summary>
        public object ReadValue(BaseVariableState variable) {
            lock (_lock) {
                switch (variable.NumericId) {
                    case Variables.ScalarValueObjectType_BooleanValue:
                    case Variables.UserScalarValueObjectType_BooleanValue: {
                            return _generator.GetRandom<bool>(false);
                        }

                    case Variables.ScalarValueObjectType_SByteValue:
                    case Variables.UserScalarValueObjectType_SByteValue: {
                            return _generator.GetRandom<sbyte>(false);
                        }

                    case Variables.AnalogScalarValueObjectType_SByteValue: {
                            return (sbyte)(((int)(_generator.GetRandom<uint>(false) % 201)) - 100);
                        }

                    case Variables.ScalarValueObjectType_ByteValue:
                    case Variables.UserScalarValueObjectType_ByteValue: {
                            return _generator.GetRandom<byte>(false);
                        }

                    case Variables.AnalogScalarValueObjectType_ByteValue: {
                            return (byte)((_generator.GetRandom<uint>(false) % 201) + 50);
                        }

                    case Variables.ScalarValueObjectType_Int16Value:
                    case Variables.UserScalarValueObjectType_Int16Value: {
                            return _generator.GetRandom<short>(false);
                        }

                    case Variables.AnalogScalarValueObjectType_Int16Value: {
                            return (short)(((int)(_generator.GetRandom<uint>(false) % 201)) - 100);
                        }

                    case Variables.ScalarValueObjectType_UInt16Value:
                    case Variables.UserScalarValueObjectType_UInt16Value: {
                            return _generator.GetRandom<ushort>(false);
                        }

                    case Variables.AnalogScalarValueObjectType_UInt16Value: {
                            return (ushort)((_generator.GetRandom<uint>(false) % 201) + 50);
                        }

                    case Variables.ScalarValueObjectType_Int32Value:
                    case Variables.UserScalarValueObjectType_Int32Value: {
                            return _generator.GetRandom<int>(false);
                        }

                    case Variables.AnalogScalarValueObjectType_Int32Value:
                    case Variables.AnalogScalarValueObjectType_IntegerValue: {
                            return ((int)(_generator.GetRandom<uint>(false) % 201)) - 100;
                        }

                    case Variables.ScalarValueObjectType_UInt32Value:
                    case Variables.UserScalarValueObjectType_UInt32Value: {
                            return _generator.GetRandom<uint>(false);
                        }

                    case Variables.AnalogScalarValueObjectType_UInt32Value:
                    case Variables.AnalogScalarValueObjectType_UIntegerValue: {
                            return (_generator.GetRandom<uint>(false) % 201) + 50;
                        }

                    case Variables.ScalarValueObjectType_Int64Value:
                    case Variables.UserScalarValueObjectType_Int64Value: {
                            return _generator.GetRandom<long>(false);
                        }

                    case Variables.AnalogScalarValueObjectType_Int64Value: {
                            return (long)(((int)(_generator.GetRandom<uint>(false) % 201)) - 100);
                        }

                    case Variables.ScalarValueObjectType_UInt64Value:
                    case Variables.UserScalarValueObjectType_UInt64Value: {
                            return _generator.GetRandom<ulong>(false);
                        }

                    case Variables.AnalogScalarValueObjectType_UInt64Value: {
                            return (ulong)((_generator.GetRandom<uint>(false) % 201) + 50);
                        }

                    case Variables.ScalarValueObjectType_FloatValue:
                    case Variables.UserScalarValueObjectType_FloatValue: {
                            return _generator.GetRandom<float>(false);
                        }

                    case Variables.AnalogScalarValueObjectType_FloatValue: {
                            return (float)(((int)(_generator.GetRandom<uint>(false) % 201)) - 100);
                        }

                    case Variables.ScalarValueObjectType_DoubleValue:
                    case Variables.UserScalarValueObjectType_DoubleValue: {
                            return _generator.GetRandom<double>(false);
                        }

                    case Variables.AnalogScalarValueObjectType_DoubleValue:
                    case Variables.AnalogScalarValueObjectType_NumberValue: {
                            return (double)(((int)(_generator.GetRandom<uint>(false) % 201)) - 100);
                        }

                    case Variables.ScalarValueObjectType_StringValue:
                    case Variables.UserScalarValueObjectType_StringValue: {
                            return _generator.GetRandom<string>(false);
                        }

                    case Variables.ScalarValueObjectType_DateTimeValue:
                    case Variables.UserScalarValueObjectType_DateTimeValue: {
                            return _generator.GetRandom<DateTime>(false);
                        }

                    case Variables.ScalarValueObjectType_GuidValue:
                    case Variables.UserScalarValueObjectType_GuidValue: {
                            return _generator.GetRandom<Guid>(false);
                        }

                    case Variables.ScalarValueObjectType_ByteStringValue:
                    case Variables.UserScalarValueObjectType_ByteStringValue: {
                            return _generator.GetRandom<byte[]>(false);
                        }

                    case Variables.ScalarValueObjectType_XmlElementValue:
                    case Variables.UserScalarValueObjectType_XmlElementValue: {
                            return _generator.GetRandom<XmlElement>(false);
                        }

                    case Variables.ScalarValueObjectType_NodeIdValue:
                    case Variables.UserScalarValueObjectType_NodeIdValue: {
                            return _generator.GetRandom<NodeId>(false);
                        }

                    case Variables.ScalarValueObjectType_ExpandedNodeIdValue:
                    case Variables.UserScalarValueObjectType_ExpandedNodeIdValue: {
                            return _generator.GetRandom<ExpandedNodeId>(false);
                        }

                    case Variables.ScalarValueObjectType_QualifiedNameValue:
                    case Variables.UserScalarValueObjectType_QualifiedNameValue: {
                            return _generator.GetRandom<QualifiedName>(false);
                        }

                    case Variables.ScalarValueObjectType_LocalizedTextValue:
                    case Variables.UserScalarValueObjectType_LocalizedTextValue: {
                            return _generator.GetRandom<LocalizedText>(false);
                        }

                    case Variables.ScalarValueObjectType_StatusCodeValue:
                    case Variables.UserScalarValueObjectType_StatusCodeValue: {
                            return _generator.GetRandom<StatusCode>(false);
                        }

                    case Variables.ScalarValueObjectType_VariantValue:
                    case Variables.UserScalarValueObjectType_VariantValue: {
                            return _generator.GetRandomVariant(false).Value;
                        }

                    case Variables.ScalarValueObjectType_StructureValue: {
                            return GetRandomStructure();
                        }

                    case Variables.ScalarValueObjectType_EnumerationValue: {
                            return _generator.GetRandom<int>(false);
                        }

                    case Variables.ScalarValueObjectType_NumberValue: {
                            return _generator.GetRandom(BuiltInType.Number);
                        }

                    case Variables.ScalarValueObjectType_IntegerValue: {
                            return _generator.GetRandom(BuiltInType.Integer);
                        }

                    case Variables.ScalarValueObjectType_UIntegerValue: {
                            return _generator.GetRandom(BuiltInType.UInteger);
                        }

                    case Variables.ArrayValueObjectType_BooleanValue:
                    case Variables.UserArrayValueObjectType_BooleanValue: {
                            return _generator.GetRandomArray<bool>(false, 100, false);
                        }

                    case Variables.ArrayValueObjectType_SByteValue:
                    case Variables.UserArrayValueObjectType_SByteValue: {
                            return _generator.GetRandomArray<sbyte>(false, 100, false);
                        }

                    case Variables.AnalogArrayValueObjectType_SByteValue: {
                            var values = _generator.GetRandomArray<sbyte>(false, 100, false);

                            for (var ii = 0; ii < values.Length; ii++) {
                                values[ii] = (sbyte)(((int)(_generator.GetRandom<uint>(false) % 201)) - 100);
                            }

                            return values;
                        }

                    case Variables.ArrayValueObjectType_ByteValue:
                    case Variables.UserArrayValueObjectType_ByteValue: {
                            return _generator.GetRandomArray<byte>(false, 100, false);
                        }

                    case Variables.AnalogArrayValueObjectType_ByteValue: {
                            var values = _generator.GetRandomArray<byte>(false, 100, false);

                            for (var ii = 0; ii < values.Length; ii++) {
                                values[ii] = (byte)((_generator.GetRandom<uint>(false) % 201) + 50);
                            }

                            return values;
                        }

                    case Variables.ArrayValueObjectType_Int16Value:
                    case Variables.UserArrayValueObjectType_Int16Value: {
                            return _generator.GetRandomArray<short>(false, 100, false);
                        }

                    case Variables.AnalogArrayValueObjectType_Int16Value: {
                            var values = _generator.GetRandomArray<short>(false, 100, false);

                            for (var ii = 0; ii < values.Length; ii++) {
                                values[ii] = (short)(((int)(_generator.GetRandom<uint>(false) % 201)) - 100);
                            }

                            return values;
                        }

                    case Variables.ArrayValueObjectType_UInt16Value:
                    case Variables.UserArrayValueObjectType_UInt16Value: {
                            return _generator.GetRandomArray<ushort>(false, 100, false);
                        }

                    case Variables.AnalogArrayValueObjectType_UInt16Value: {
                            var values = _generator.GetRandomArray<ushort>(false, 100, false);

                            for (var ii = 0; ii < values.Length; ii++) {
                                values[ii] = (ushort)((_generator.GetRandom<uint>(false) % 201) + 50);
                            }

                            return values;
                        }

                    case Variables.ArrayValueObjectType_Int32Value:
                    case Variables.UserArrayValueObjectType_Int32Value: {
                            return _generator.GetRandomArray<int>(false, 100, false);
                        }

                    case Variables.AnalogArrayValueObjectType_Int32Value:
                    case Variables.AnalogArrayValueObjectType_IntegerValue: {
                            var values = _generator.GetRandomArray<int>(false, 100, false);

                            for (var ii = 0; ii < values.Length; ii++) {
                                values[ii] = ((int)(_generator.GetRandom<uint>(false) % 201)) - 100;
                            }

                            return values;
                        }

                    case Variables.ArrayValueObjectType_UInt32Value:
                    case Variables.UserArrayValueObjectType_UInt32Value: {
                            return _generator.GetRandomArray<uint>(false, 100, false);
                        }

                    case Variables.AnalogArrayValueObjectType_UInt32Value:
                    case Variables.AnalogArrayValueObjectType_UIntegerValue: {
                            var values = _generator.GetRandomArray<uint>(false, 100, false);

                            for (var ii = 0; ii < values.Length; ii++) {
                                values[ii] = (_generator.GetRandom<uint>(false) % 201) + 50;
                            }

                            return values;
                        }

                    case Variables.ArrayValueObjectType_Int64Value:
                    case Variables.UserArrayValueObjectType_Int64Value: {
                            return _generator.GetRandomArray<long>(false, 100, false);
                        }

                    case Variables.AnalogArrayValueObjectType_Int64Value: {
                            var values = _generator.GetRandomArray<long>(false, 100, false);

                            for (var ii = 0; ii < values.Length; ii++) {
                                values[ii] = ((int)(_generator.GetRandom<uint>(false) % 201)) - 100;
                            }

                            return values;
                        }

                    case Variables.ArrayValueObjectType_UInt64Value:
                    case Variables.UserArrayValueObjectType_UInt64Value: {
                            return _generator.GetRandomArray<ulong>(false, 100, false);
                        }

                    case Variables.AnalogArrayValueObjectType_UInt64Value: {
                            var values = _generator.GetRandomArray<ulong>(false, 100, false);

                            for (var ii = 0; ii < values.Length; ii++) {
                                values[ii] = (_generator.GetRandom<uint>(false) % 201) + 50;
                            }

                            return values;
                        }

                    case Variables.ArrayValueObjectType_FloatValue:
                    case Variables.UserArrayValueObjectType_FloatValue: {
                            return _generator.GetRandomArray<float>(false, 100, false);
                        }

                    case Variables.AnalogArrayValueObjectType_FloatValue: {
                            var values = _generator.GetRandomArray<float>(false, 100, false);

                            for (var ii = 0; ii < values.Length; ii++) {
                                values[ii] = ((int)(_generator.GetRandom<uint>(false) % 201)) - 100;
                            }

                            return values;
                        }

                    case Variables.ArrayValueObjectType_DoubleValue:
                    case Variables.UserArrayValueObjectType_DoubleValue: {
                            return _generator.GetRandomArray<double>(false, 100, false);
                        }

                    case Variables.AnalogArrayValueObjectType_DoubleValue:
                    case Variables.AnalogArrayValueObjectType_NumberValue: {
                            var values = _generator.GetRandomArray<double>(false, 100, false);

                            for (var ii = 0; ii < values.Length; ii++) {
                                values[ii] = ((int)(_generator.GetRandom<uint>(false) % 201)) - 100;
                            }

                            return values;
                        }

                    case Variables.ArrayValueObjectType_StringValue:
                    case Variables.UserArrayValueObjectType_StringValue: {
                            return _generator.GetRandomArray<string>(false, 100, false);
                        }

                    case Variables.ArrayValueObjectType_DateTimeValue:
                    case Variables.UserArrayValueObjectType_DateTimeValue: {
                            return _generator.GetRandomArray<DateTime>(false, 100, false);
                        }

                    case Variables.ArrayValueObjectType_GuidValue:
                    case Variables.UserArrayValueObjectType_GuidValue: {
                            return _generator.GetRandomArray<Guid>(false, 100, false);
                        }

                    case Variables.ArrayValueObjectType_ByteStringValue:
                    case Variables.UserArrayValueObjectType_ByteStringValue: {
                            return _generator.GetRandomArray<byte[]>(false, 100, false);
                        }

                    case Variables.ArrayValueObjectType_XmlElementValue:
                    case Variables.UserArrayValueObjectType_XmlElementValue: {
                            return _generator.GetRandomArray<XmlElement>(false, 100, false);
                        }

                    case Variables.ArrayValueObjectType_NodeIdValue:
                    case Variables.UserArrayValueObjectType_NodeIdValue: {
                            return _generator.GetRandomArray<NodeId>(false, 100, false);
                        }

                    case Variables.ArrayValueObjectType_ExpandedNodeIdValue:
                    case Variables.UserArrayValueObjectType_ExpandedNodeIdValue: {
                            return _generator.GetRandomArray<ExpandedNodeId>(false, 100, false);
                        }

                    case Variables.ArrayValueObjectType_QualifiedNameValue:
                    case Variables.UserArrayValueObjectType_QualifiedNameValue: {
                            return _generator.GetRandomArray<QualifiedName>(false, 100, false);
                        }

                    case Variables.ArrayValueObjectType_LocalizedTextValue:
                    case Variables.UserArrayValueObjectType_LocalizedTextValue: {
                            return _generator.GetRandomArray<LocalizedText>(false, 100, false);
                        }

                    case Variables.ArrayValueObjectType_StatusCodeValue:
                    case Variables.UserArrayValueObjectType_StatusCodeValue: {
                            return _generator.GetRandomArray<StatusCode>(false, 100, false);
                        }

                    case Variables.ArrayValueObjectType_VariantValue:
                    case Variables.UserArrayValueObjectType_VariantValue: {
                            return _generator.GetRandomArray<object>(false, 100, false);
                        }

                    case Variables.ArrayValueObjectType_StructureValue: {
                            var values = _generator.GetRandomArray<ExtensionObject>(false, 10, false);

                            for (var ii = 0; values != null && ii < values.Length; ii++) {
                                values[ii] = GetRandomStructure();
                            }

                            return values;
                        }

                    case Variables.ArrayValueObjectType_EnumerationValue: {
                            return _generator.GetRandomArray<int>(false, 100, false);
                        }

                    case Variables.ArrayValueObjectType_NumberValue: {
                            return _generator.GetRandomArray(BuiltInType.Number, false, 100, false);
                        }

                    case Variables.ArrayValueObjectType_IntegerValue: {
                            return _generator.GetRandomArray(BuiltInType.Integer, false, 100, false);
                        }

                    case Variables.ArrayValueObjectType_UIntegerValue: {
                            return _generator.GetRandomArray(BuiltInType.UInteger, false, 100, false);
                        }
                }

                return null;
            }
        }

        /// <summary>
        /// Returns a random structure.
        /// </summary>
        private ExtensionObject GetRandomStructure() {
            if (_generator.GetRandomBoolean()) {
                var value = new ScalarValueDataType {
                    BooleanValue = _generator.GetRandom<bool>(false),
                    SByteValue = _generator.GetRandom<sbyte>(false),
                    ByteValue = _generator.GetRandom<byte>(false),
                    Int16Value = _generator.GetRandom<short>(false),
                    UInt16Value = _generator.GetRandom<ushort>(false),
                    Int32Value = _generator.GetRandom<int>(false),
                    UInt32Value = _generator.GetRandom<uint>(false),
                    Int64Value = _generator.GetRandom<long>(false),
                    UInt64Value = _generator.GetRandom<ulong>(false),
                    FloatValue = _generator.GetRandom<float>(false),
                    DoubleValue = _generator.GetRandom<double>(false),
                    StringValue = _generator.GetRandom<string>(false),
                    DateTimeValue = _generator.GetRandom<DateTime>(false),
                    GuidValue = _generator.GetRandom<Uuid>(false),
                    ByteStringValue = _generator.GetRandom<byte[]>(false),
                    XmlElementValue = _generator.GetRandom<XmlElement>(false),
                    NodeIdValue = _generator.GetRandom<NodeId>(false),
                    ExpandedNodeIdValue = _generator.GetRandom<ExpandedNodeId>(false),
                    QualifiedNameValue = _generator.GetRandom<QualifiedName>(false),
                    LocalizedTextValue = _generator.GetRandom<LocalizedText>(false),
                    StatusCodeValue = _generator.GetRandom<StatusCode>(false),
                    VariantValue = _generator.GetRandomVariant(false)
                };

                return new ExtensionObject(value);
            }
            else {
                var value = new ArrayValueDataType {
                    BooleanValue = _generator.GetRandomArray<bool>(false, 10, false),
                    SByteValue = _generator.GetRandomArray<sbyte>(false, 10, false),
                    ByteValue = _generator.GetRandomArray<byte>(false, 10, false),
                    Int16Value = _generator.GetRandomArray<short>(false, 10, false),
                    UInt16Value = _generator.GetRandomArray<ushort>(false, 10, false),
                    Int32Value = _generator.GetRandomArray<int>(false, 10, false),
                    UInt32Value = _generator.GetRandomArray<uint>(false, 10, false),
                    Int64Value = _generator.GetRandomArray<long>(false, 10, false),
                    UInt64Value = _generator.GetRandomArray<ulong>(false, 10, false),
                    FloatValue = _generator.GetRandomArray<float>(false, 10, false),
                    DoubleValue = _generator.GetRandomArray<double>(false, 10, false),
                    StringValue = _generator.GetRandomArray<string>(false, 10, false),
                    DateTimeValue = _generator.GetRandomArray<DateTime>(false, 10, false),
                    GuidValue = _generator.GetRandomArray<Uuid>(false, 10, false),
                    ByteStringValue = _generator.GetRandomArray<byte[]>(false, 10, false),
                    XmlElementValue = _generator.GetRandomArray<XmlElement>(false, 10, false),
                    NodeIdValue = _generator.GetRandomArray<NodeId>(false, 10, false),
                    ExpandedNodeIdValue = _generator.GetRandomArray<ExpandedNodeId>(false, 10, false),
                    QualifiedNameValue = _generator.GetRandomArray<QualifiedName>(false, 10, false),
                    LocalizedTextValue = _generator.GetRandomArray<LocalizedText>(false, 10, false),
                    StatusCodeValue = _generator.GetRandomArray<StatusCode>(false, 10, false)
                };

                var values = _generator.GetRandomArray<object>(false, 10, false);

                for (var ii = 0; values != null && ii < values.Length; ii++) {
                    value.VariantValue.Add(new Variant(values[ii]));
                }

                return new ExtensionObject(value);
            }
        }

        public void StartMonitoringValue(uint monitoredItemId, double samplingInterval, BaseVariableState variable) {
            lock (_lock) {
                if (_monitoredNodes == null) {
                    _monitoredNodes = new Dictionary<uint, BaseVariableState>();
                }

                _monitoredNodes[monitoredItemId] = variable;

                SetSamplingInterval(samplingInterval);
            }
        }

        public void SetSamplingInterval(double samplingInterval) {
            lock (_lock) {
                if (samplingInterval < 0) {
                    // _samplingEvent.Set();
                    _minimumSamplingInterval = int.MaxValue;

                    if (_timer != null) {
                        _timer.Dispose();
                        _timer = null;
                    }

                    return;
                }

                if (_minimumSamplingInterval > samplingInterval) {
                    _minimumSamplingInterval = (int)samplingInterval;

                    if (_minimumSamplingInterval < 100) {
                        _minimumSamplingInterval = 100;
                    }

                    if (_timer != null) {
                        _timer.Dispose();
                        _timer = null;
                    }

                    _timer = new Timer(DoSample, null, _minimumSamplingInterval, _minimumSamplingInterval);
                }
            }
        }

        void DoSample(object state) {
            Utils.Trace("DoSample HiRes={0:ss.ffff} Now={1:ss.ffff}", HiResClock.UtcNow, DateTime.UtcNow);

            var samples = new Queue<Sample>();

            lock (_lock) {
                if (_monitoredNodes == null) {
                    return;
                }

                foreach (var variable in _monitoredNodes.Values) {
                    var sample = new Sample {
                        Variable = variable
                    };
                    sample.Value = ReadValue(sample.Variable);
                    sample.StatusCode = StatusCodes.Good;
                    sample.Timestamp = DateTime.UtcNow;

                    samples.Enqueue(sample);
                }
            }

            while (samples.Count > 0) {
                var sample = samples.Dequeue();

                _callback.OnDataChange(
                    sample.Variable,
                    sample.Value,
                    sample.StatusCode,
                    sample.Timestamp);
            }
        }

        public void StopMonitoringValue(uint monitoredItemId) {
            lock (_lock) {
                if (_monitoredNodes == null) {
                    return;
                }

                _monitoredNodes.Remove(monitoredItemId);

                if (_monitoredNodes.Count == 0) {
                    SetSamplingInterval(-1);
                }
            }
        }

        private class Sample {
            public BaseVariableState Variable;
            public object Value;
            public StatusCode StatusCode;
            public DateTime Timestamp;
        }


        private readonly object _lock = new object();
        private ITestDataSystemCallback _callback;
        private Opc.Ua.Test.DataGenerator _generator;
        private int _minimumSamplingInterval;
        private Dictionary<uint, BaseVariableState> _monitoredNodes;
        private Timer _timer;
        private StatusCode _systemStatus;
        private HistoryArchive _historyArchive;
    }
}
