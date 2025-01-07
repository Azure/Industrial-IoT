/* ========================================================================
 * Copyright (c) 2005-2017 The OPC Foundation, Inc. All rights reserved.
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

namespace PerfTest
{
    using Opc.Ua;
    using Opc.Ua.Server;
    using System;
    using System.Collections.Generic;
    using System.Threading;

    public class UnderlyingSystem
    {
        public void Initialize()
        {
            _registers = [];
            var register1 = new MemoryRegister();
            register1.Initialize(1, "R1", 50000);
            _registers.Add(register1);
        }

        public IList<MemoryRegister> Registers => _registers;

        public MemoryRegister GetRegister(int id)
        {
            if (id > 0 && id <= _registers.Count)
            {
                return _registers[id - 1];
            }

            return null;
        }

        private List<MemoryRegister> _registers;
    }

    public class MemoryRegister
    {
        public int Id { get; private set; }

        public string Name { get; private set; }

        public int Size => _values.Length;

        public void Initialize(int id, string name, int size)
        {
            Id = id;
            Name = name;
            _values = new int[size];
            _monitoredItems = new IDataChangeMonitoredItem2[size][];
        }

        public int Read(int index)
        {
            if (index >= 0 && index < _values.Length)
            {
                return _values[index];
            }

            return 0;
        }

        public void Subscribe(int index, IDataChangeMonitoredItem2 monitoredItem)
        {
            lock (_lock)
            {
                _timer ??= new Timer(OnUpdate, null, 45, 45);

                if (index >= 0 && index < _values.Length)
                {
                    var monitoredItems = _monitoredItems[index];

                    if (monitoredItems == null)
                    {
                        _monitoredItems[index] = monitoredItems = new IDataChangeMonitoredItem2[1];
                    }
                    else
                    {
                        _monitoredItems[index] = new IDataChangeMonitoredItem2[monitoredItems.Length + 1];
                        Array.Copy(monitoredItems, _monitoredItems[index], monitoredItems.Length);
                        monitoredItems = _monitoredItems[index];
                    }

                    monitoredItems[^1] = monitoredItem;
                }
            }
        }

        public void Unsubscribe(int index, IDataChangeMonitoredItem2 monitoredItem)
        {
            lock (_lock)
            {
                if (index >= 0 && index < _values.Length)
                {
                    var monitoredItems = _monitoredItems[index];

                    if (monitoredItems != null)
                    {
                        for (var ii = 0; ii < monitoredItems.Length; ii++)
                        {
                            if (ReferenceEquals(monitoredItems[ii], monitoredItem))
                            {
                                _monitoredItems[index] = new IDataChangeMonitoredItem2[monitoredItems.Length - 1];

                                if (ii > 0)
                                {
                                    Array.Copy(monitoredItems, _monitoredItems[index], ii);
                                }

                                if (ii < monitoredItems.Length - 1)
                                {
                                    Array.Copy(monitoredItems, ii + 1, _monitoredItems[index], 0, monitoredItems.Length - ii - 1);
                                }

                                break;
                            }
                        }
                    }
                }
            }
        }

        private void OnUpdate(object state)
        {
            try
            {
                lock (_lock)
                {
                    var start = HiResClock.UtcNow;
                    var delta = _values.Length / 2;

                    var value = new DataValue
                    {
                        ServerTimestamp = DateTime.UtcNow,
                        SourceTimestamp = DateTime.UtcNow
                    };

                    for (var ii = _start; ii < delta + _start && ii < _values.Length; ii++)
                    {
                        _values[ii] += ii + 1;

                        var monitoredItems = _monitoredItems[ii];

                        if (monitoredItems != null)
                        {
                            value.WrappedValue = new Variant(_values[ii]);

                            for (var jj = 0; jj < monitoredItems.Length; jj++)
                            {
                                monitoredItems[jj].QueueValue(value, null, true);
                            }
                        }
                    }

                    _start += delta;

                    if (_start >= _values.Length)
                    {
                        _start = 0;
                    }

                    if ((HiResClock.UtcNow - start).TotalMilliseconds > 50)
                    {
                        Utils.Trace("Update took {0}ms.", (HiResClock.UtcNow - start).TotalMilliseconds);
                    }
                }
            }
            catch (Exception e)
            {
                Utils.Trace(e, "Unexpected error updating items.");
            }
        }

        private readonly Lock _lock = new();
        private int[] _values;
        private int _start;
        private Timer _timer;
        private IDataChangeMonitoredItem2[][] _monitoredItems;
    }
}
