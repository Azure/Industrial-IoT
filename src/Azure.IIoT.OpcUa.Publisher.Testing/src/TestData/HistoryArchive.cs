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

    /// <summary>
    /// A class that provides access to archived data.
    /// </summary>
    internal sealed class HistoryArchive : IDisposable
    {
        /// <summary>
        /// Frees any unmanaged resources.
        /// </summary>
        public void Dispose()
        {
            if (_updateTimer != null)
            {
                _updateTimer.Dispose();
                _updateTimer = null;
            }
        }

        /// <summary>
        /// Creates a new record in the archive.
        /// </summary>
        /// <param name="nodeId"></param>
        /// <param name="dataType"></param>
        public void CreateRecord(NodeId nodeId, BuiltInType dataType)
        {
            lock (_lock)
            {
                var record = new HistoryRecord
                {
                    RawData = [],
                    Historizing = true,
                    DataType = dataType
                };

                var now = DateTime.UtcNow;

                for (var ii = 1000; ii >= 0; ii--)
                {
                    var entry = new HistoryEntry
                    {
                        Value = new DataValue
                        {
                            ServerTimestamp = now.AddSeconds(-(ii * 10))
                        }
                    };
                    entry.Value.SourceTimestamp = entry.Value.ServerTimestamp.AddMilliseconds(1234);
                    entry.IsModified = false;

                    switch (dataType)
                    {
                        case BuiltInType.Int32:
                            {
                                entry.Value.Value = ii;
                                break;
                            }
                    }

                    record.RawData.Add(entry);
                }

                _records ??= [];

                _records[nodeId] = record;

                _updateTimer ??= new Timer(OnUpdate, null, 10000, 10000);
            }
        }

        /// <summary>
        /// Periodically adds new values into the archive.
        /// </summary>
        /// <param name="state"></param>
        private void OnUpdate(object state)
        {
            try
            {
                var now = DateTime.UtcNow;

                lock (_lock)
                {
                    foreach (var record in _records.Values)
                    {
                        if (!record.Historizing || record.RawData.Count >= 2000)
                        {
                            continue;
                        }

                        var entry = new HistoryEntry
                        {
                            Value = new DataValue
                            {
                                ServerTimestamp = now
                            }
                        };
                        entry.Value.SourceTimestamp = entry.Value.ServerTimestamp.AddMilliseconds(-4567);
                        entry.IsModified = false;

                        switch (record.DataType)
                        {
                            case BuiltInType.Int32:
                                {
                                    var lastValue = (int)record.RawData[^1].Value.Value;
                                    entry.Value.Value = lastValue + 1;
                                    break;
                                }
                        }

                        record.RawData.Add(entry);
                    }
                }
            }
            catch (Exception e)
            {
                Utils.Trace(e, "Unexpected error updating history.");
            }
        }

        private readonly Lock _lock = new();
        private Timer _updateTimer;
        private Dictionary<NodeId, HistoryRecord> _records;
    }

    /// <summary>
    /// A single entry in the archive.
    /// </summary>
    internal sealed class HistoryEntry
    {
        public DataValue Value;
        public bool IsModified;
    }

    /// <summary>
    /// A record in the archive.
    /// </summary>
    internal sealed class HistoryRecord
    {
        public List<HistoryEntry> RawData;
        public bool Historizing;
        public BuiltInType DataType;
    }
}
