// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Plc
{
    using Opc.Ua;
    using Opc.Ua.Test;
    using System;

    public sealed class SimulatedVariableNode<T> : IDisposable
    {
        private readonly ISystemContext _context;
        private readonly BaseDataVariableState _variable;
        private ITimer _timer;
        private readonly TimeService _timeService;

        public T Value
        {
            get => GetValue(_variable.Value);
            set => SetValue(_variable, value);
        }

        public SimulatedVariableNode(ISystemContext context, BaseDataVariableState variable, TimeService timeService)
        {
            _context = context;
            _variable = variable;
            _timeService = timeService;
        }

        public void Dispose()
        {
            Stop();
            _timer.Dispose();
        }

        /// <summary>
        /// Start periodic update.
        /// The update Func gets the current value as input and should return the updated value.
        /// </summary>
        /// <param name="update"></param>
        /// <param name="periodMs"></param>
        public void Start(Func<T, T> update, int periodMs)
        {
            _timer = _timeService.NewTimer((s, o) => Value = update(Value), (uint)periodMs);
        }

        public void Stop()
        {
            if (_timer == null)
            {
                return;
            }

            _timer.Enabled = false;
        }

        private void SetValue(BaseDataVariableState variable, T value)
        {
            variable.Value = ToVariant(variable, value);
            variable.Timestamp = _timeService.Now;
            variable.ClearChangeMasks(_context, false);
        }

        private static T GetValue(Variant value)
        {
            if (typeof(T) == typeof(bool) && value.TryGetValue(out bool boolValue))
            {
                return (T)(object)boolValue;
            }

            if (typeof(T) == typeof(int) && value.TryGetValue(out int intValue))
            {
                return (T)(object)intValue;
            }

            if (typeof(T) == typeof(uint) && value.TryGetValue(out uint uintValue))
            {
                return (T)(object)uintValue;
            }

            if (typeof(T) == typeof(double) && value.TryGetValue(out double doubleValue))
            {
                return (T)(object)doubleValue;
            }

            if (typeof(T) == typeof(string) && value.TryGetValue(out string stringValue))
            {
                return (T)(object)stringValue;
            }

            if (typeof(T) == typeof(byte[]))
            {
                if (value.TryGetValue(out ByteString byteStringValue))
                {
                    return (T)(object)byteStringValue.ToArray();
                }

                if (value.TryGetValue(out ArrayOf<byte> arrayValue))
                {
                    return (T)(object)(arrayValue.ToArray() ?? []);
                }
            }

            return default!;
        }

        private static Variant ToVariant(BaseDataVariableState variable, T value)
        {
            return value switch
            {
                bool boolValue => new Variant(boolValue),
                int intValue => new Variant(intValue),
                uint uintValue => new Variant(uintValue),
                double doubleValue => new Variant(doubleValue),
                string stringValue => new Variant(stringValue),
                byte[] bytes when variable.ValueRank == ValueRanks.Scalar =>
                    new Variant(ByteString.From(bytes)),
                byte[] bytes => new Variant((ArrayOf<byte>)bytes),
                null => Variant.Null,
                _ => throw new NotSupportedException($"Cannot convert {typeof(T)} to an OPC UA Variant.")
            };
        }
    }
}
