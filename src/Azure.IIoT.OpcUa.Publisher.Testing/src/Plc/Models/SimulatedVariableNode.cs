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
            get => (T)_variable.Value;
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
            variable.Value = value;
            variable.Timestamp = _timeService.Now;
            variable.ClearChangeMasks(_context, false);
        }
    }
}
