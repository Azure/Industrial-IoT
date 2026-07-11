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

    public partial class TestDataObjectState
    {
        /// <summary>
        /// Initializes the object as a collection of counters which change value on read.
        /// </summary>
        /// <param name="context"></param>
        /// <param name="node"></param>
        protected override void OnAfterCreate(ISystemContext context, NodeState node, System.Threading.CancellationToken ct = default)
        {
            base.OnAfterCreate(context, node, ct);

            GenerateValues.OnCall = OnGenerateValues;
        }

        /// <summary>
        /// Initialzies the variable as a counter.
        /// </summary>
        /// <param name="context"></param>
        /// <param name="variable"></param>
        /// <param name="numericId"></param>
        protected void InitializeVariable(ISystemContext context, BaseVariableState variable, uint numericId)
        {
            variable.NumericId = numericId;

            // provide an implementation that produces a random value on each read.
            if (SimulationActive.Value)
            {
                variable.OnReadValue = DoDeviceRead;
            }

            // set a valid initial value.

            if (context.SystemHandle is TestDataSystem system)
            {
                GenerateValue(system, variable);
            }

            // allow writes if the simulation is not active.
            if (!SimulationActive.Value)
            {
                variable.AccessLevel = variable.UserAccessLevel = AccessLevels.CurrentReadOrWrite;
            }

            // set the EU range.

            if (variable.FindChild(context, (QualifiedName)Opc.Ua.BrowseNames.EURange) is BaseVariableState euRange)
            {
                if (context.TypeTable.IsTypeOf(variable.DataType, Opc.Ua.DataTypeIds.UInteger))
                {
                    euRange.Value = Variant.FromStructure(new Opc.Ua.Range(250, 50));
                }
                else
                {
                    euRange.Value = Variant.FromStructure(new Opc.Ua.Range(100, -100));
                }
            }

            variable.OnSimpleWriteValue = OnWriteAnalogValue;
        }

        /// <summary>
        /// Validates a written value.
        /// </summary>
        /// <param name="context"></param>
        /// <param name="node"></param>
        /// <param name="value"></param>
        public ServiceResult OnWriteAnalogValue(
            ISystemContext context,
            NodeState node,
            ref Variant value)
        {
            if (node.FindChild(context, (QualifiedName)Opc.Ua.BrowseNames.EURange) is not BaseVariableState euRange)
            {
                return ServiceResult.Good;
            }

            if (!euRange.Value.TryGetStructure<Opc.Ua.Range>(out Opc.Ua.Range range))
            {
                return ServiceResult.Good;
            }

            if (TryValidateNumberArray(value, range, out var arrayResult))
            {
                return arrayResult;
            }

            var number = value.GetDouble();

            if (number > range.High || number < range.Low)
            {
                return StatusCodes.BadOutOfRange;
            }

            return ServiceResult.Good;
        }

        /// <summary>
        /// Generates a new value for the variable.
        /// </summary>
        /// <param name="system"></param>
        /// <param name="variable"></param>
        protected void GenerateValue(TestDataSystem system, BaseVariableState variable)
        {
            variable.Value = new Variant(system.ReadValue(variable));
            variable.Timestamp = DateTime.UtcNow;
            variable.StatusCode = StatusCodes.Good;
        }

        /// <summary>
        /// Handles the generate values method.
        /// </summary>
        /// <param name="context"></param>
        /// <param name="method"></param>
        /// <param name="objectId"></param>
        /// <param name="count"></param>
        protected virtual ServiceResult OnGenerateValues(
            ISystemContext context,
            MethodState method,
            NodeId objectId,
            uint count)
        {
            ClearChangeMasks(context, true);

            if (AreEventsMonitored)
            {
                var e = new GenerateValuesEventState(null);

                var message = new TranslationInfo(
                    "GenerateValuesEventType",
                    "en-US",
                    "New values generated for test source '{0}'.",
                    DisplayName);

                e.Initialize(
                    context,
                    this,
                    EventSeverity.MediumLow,
                    new LocalizedText(message));

                e.Iterations = new PropertyState<uint>.Implementation<VariantBuilder>(e)
                {
                    Value = count
                };

                e.NewValueCount = new PropertyState<uint>.Implementation<VariantBuilder>(e)
                {
                    Value = 10
                };

                ReportEvent(context, e);
            }

#if CONDITION_SAMPLES
            this.CycleComplete.RequestAcknowledgement(context, (ushort)EventSeverity.Low);
#endif

            return ServiceResult.Good;
        }

        /// <summary>
        /// Generates a new value each time the value is read.
        /// </summary>
        /// <param name="context"></param>
        /// <param name="node"></param>
        /// <param name="indexRange"></param>
        /// <param name="dataEncoding"></param>
        /// <param name="value"></param>
        /// <param name="statusCode"></param>
        /// <param name="timestamp"></param>
        private ServiceResult DoDeviceRead(
            ISystemContext context,
            NodeState node,
            NumericRange indexRange,
            QualifiedName dataEncoding,
            ref Variant value,
            ref StatusCode statusCode,
            ref DateTimeUtc timestamp)
        {
            if (node is not BaseVariableState variable)
            {
                return ServiceResult.Good;
            }

            if (!SimulationActive.Value)
            {
                return ServiceResult.Good;
            }

            if (context.SystemHandle is not TestDataSystem system)
            {
                return StatusCodes.BadOutOfService;
            }

            try
            {
                value = new Variant(system.ReadValue(variable));

                statusCode = StatusCodes.Good;
                timestamp = DateTimeUtc.Now;

                var error = BaseVariableState.ApplyIndexRangeAndDataEncoding(
                    context,
                    indexRange,
                    dataEncoding,
                    ref value);

                if (ServiceResult.IsBad(error))
                {
                    statusCode = error.StatusCode;
                }

                return ServiceResult.Good;
            }
            catch (Exception e)
            {
                return new ServiceResult(e);
            }
        }

        private static bool TryValidateNumberArray(
            Variant value,
            Opc.Ua.Range range,
            out ServiceResult result)
        {
            if (value.TryGetValue(out ArrayOf<sbyte> sbyteValues))
            {
                result = ValidateNumberArray(sbyteValues.ToArray(), range);
                return true;
            }
            if (value.TryGetValue(out ArrayOf<byte> byteValues))
            {
                result = ValidateNumberArray(byteValues.ToArray(), range);
                return true;
            }
            if (value.TryGetValue(out ArrayOf<short> int16Values))
            {
                result = ValidateNumberArray(int16Values.ToArray(), range);
                return true;
            }
            if (value.TryGetValue(out ArrayOf<ushort> uint16Values))
            {
                result = ValidateNumberArray(uint16Values.ToArray(), range);
                return true;
            }
            if (value.TryGetValue(out ArrayOf<int> int32Values))
            {
                result = ValidateNumberArray(int32Values.ToArray(), range);
                return true;
            }
            if (value.TryGetValue(out ArrayOf<uint> uint32Values))
            {
                result = ValidateNumberArray(uint32Values.ToArray(), range);
                return true;
            }
            if (value.TryGetValue(out ArrayOf<long> int64Values))
            {
                result = ValidateNumberArray(int64Values.ToArray(), range);
                return true;
            }
            if (value.TryGetValue(out ArrayOf<ulong> uint64Values))
            {
                result = ValidateNumberArray(uint64Values.ToArray(), range);
                return true;
            }
            if (value.TryGetValue(out ArrayOf<float> floatValues))
            {
                result = ValidateNumberArray(floatValues.ToArray(), range);
                return true;
            }
            if (value.TryGetValue(out ArrayOf<double> doubleValues))
            {
                result = ValidateNumberArray(doubleValues.ToArray(), range);
                return true;
            }

            result = ServiceResult.Good;
            return false;
        }

        private static ServiceResult ValidateNumberArray<T>(
            IEnumerable<T> values,
            Opc.Ua.Range range) where T : IConvertible
        {
            foreach (var value in values)
            {
                var number = Convert.ToDouble(value, System.Globalization.CultureInfo.InvariantCulture);

                if (number > range.High || number < range.Low)
                {
                    return StatusCodes.BadOutOfRange;
                }
            }

            return ServiceResult.Good;
        }
    }
}
