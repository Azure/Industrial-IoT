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

namespace TestData {
    using Opc.Ua;
    using System;
    using System.Collections.Generic;

    /// <summary>
    /// Wraps a file which contains a list of historical values.
    /// </summary>
    internal sealed class HistoryFile : IHistoryDataSource {
        /// <summary>
        /// Creates a new file.
        /// </summary>
        internal HistoryFile(object dataLock, List<HistoryEntry> entries) {
            _lock = dataLock;
            _entries = entries;
        }

        /// <summary>
        /// Returns the next value in the archive.
        /// </summary>
        /// <param name="startTime">The starting time for the search.</param>
        /// <param name="isForward">Whether to search forward in time.</param>
        /// <param name="isReadModified">Whether to return modified data.</param>
        /// <param name="position">A index that must be passed to the NextRaw call. </param>
        /// <returns>The DataValue.</returns>
        public DataValue FirstRaw(DateTime startTime, bool isForward, bool isReadModified, out int position) {
            position = -1;

            lock (_lock) {
                if (isForward) {
                    for (var ii = 0; ii < _entries.Count; ii++) {
                        if (_entries[ii].Value.ServerTimestamp >= startTime) {
                            position = ii;
                            break;
                        }
                    }
                }
                else {
                    for (var ii = _entries.Count - 1; ii >= 0; ii--) {
                        if (_entries[ii].Value.ServerTimestamp <= startTime) {
                            position = ii;
                            break;
                        }
                    }
                }

                if (position < 0 || position >= _entries.Count) {
                    return null;
                }

                var entry = _entries[position];

                return new DataValue {
                    Value = entry.Value.Value,
                    ServerTimestamp = entry.Value.ServerTimestamp,
                    SourceTimestamp = entry.Value.SourceTimestamp,
                    StatusCode = entry.Value.StatusCode
                };
            }
        }

        /// <summary>
        /// Returns the next value in the archive.
        /// </summary>
        /// <param name="lastTime">The timestamp of the last value returned.</param>
        /// <param name="isForward">Whether to search forward in time.</param>
        /// <param name="isReadModified">Whether to return modified data.</param>
        /// <param name="position">A index previously returned by the reader.</param>
        /// <returns>The DataValue.</returns>
        public DataValue NextRaw(DateTime lastTime, bool isForward, bool isReadModified, ref int position) {
            position++;

            lock (_lock) {
                if (position < 0 || position >= _entries.Count) {
                    return null;
                }

                var entry = _entries[position];

                return new DataValue {
                    Value = entry.Value.Value,
                    ServerTimestamp = entry.Value.ServerTimestamp,
                    SourceTimestamp = entry.Value.SourceTimestamp,
                    StatusCode = entry.Value.StatusCode
                };
            }
        }

        private readonly object _lock = new();
        private readonly List<HistoryEntry> _entries;
    }
}
