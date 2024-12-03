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

namespace MemoryBuffer
{
    using Opc.Ua;
    using Opc.Ua.Server;
    using System;
    using System.Collections.Generic;
    using System.Diagnostics;
    using System.Threading;

    public partial class MemoryBufferState
    {
        /// <summary>
        /// Initializes the buffer from the configuration.
        /// </summary>
        /// <param name="context"></param>
        /// <param name="configuration"></param>
        public MemoryBufferState(ISystemContext context, MemoryBufferInstance configuration) :
            base(null)
        {
            Initialize(context);
            var dataType = "UInt32";
            var name = dataType;
            var count = 10;

            if (configuration != null)
            {
                count = configuration.TagCount;

                if (!string.IsNullOrEmpty(configuration.DataType))
                {
                    dataType = configuration.DataType;
                }

                if (!string.IsNullOrEmpty(configuration.Name))
                {
                    name = dataType;
                }
            }

            SymbolicName = name;

            var elementType = BuiltInType.UInt32;

            switch (dataType)
            {
                case "Double":
                    {
                        elementType = BuiltInType.Double;
                        break;
                    }
            }

            CreateBuffer(elementType, count);
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing && _scanTimer != null)
            {
                _scanTimer.Dispose();
                _scanTimer = null;
            }
            base.Dispose(disposing);
        }

        /// <summary>
        /// The server that the buffer belongs to.
        /// </summary>
        public IServerInternal Server { get; private set; }

        /// <summary>
        /// The node manager that the buffer belongs to.
        /// </summary>
        public INodeManager NodeManager { get; private set; }

        /// <summary>
        /// The built-in type for the values stored in the buffer.
        /// </summary>
        public BuiltInType ElementType { get; private set; }

        /// <summary>
        /// The size of each element in the buffer.
        /// </summary>
        public uint ElementSize => (uint)_elementSize;

        /// <summary>
        /// The rate at which the buffer is scanned.
        /// </summary>
        public int MaximumScanRate { get; private set; }

        /// <summary>
        /// Initializes the buffer with enough space to hold the specified number of elements.
        /// </summary>
        /// <param name="elementName">The type of element.</param>
        /// <param name="noOfElements">The number of elements.</param>
        public void CreateBuffer(string elementName, int noOfElements)
        {
            if (string.IsNullOrEmpty(elementName))
            {
                elementName = "UInt32";
            }

            var elementType = BuiltInType.UInt32;

            switch (elementName)
            {
                case "Double":
                    {
                        elementType = BuiltInType.Double;
                        break;
                    }
            }

            CreateBuffer(elementType, noOfElements);
        }

        /// <summary>
        /// Initializes the buffer with enough space to hold the specified number of elements.
        /// </summary>
        /// <param name="elementType">The type of element.</param>
        /// <param name="noOfElements">The number of elements.</param>
        public void CreateBuffer(BuiltInType elementType, int noOfElements)
        {
            lock (_dataLock)
            {
                ElementType = elementType;
                _elementSize = 1;

                switch (ElementType)
                {
                    case BuiltInType.UInt32:
                        {
                            _elementSize = 4;
                            break;
                        }

                    case BuiltInType.Double:
                        {
                            _elementSize = 8;
                            break;
                        }
                }

                _lastScanTime = DateTime.UtcNow;
                MaximumScanRate = 1000;

                _buffer = new byte[_elementSize * noOfElements];
                SizeInBytes.Value = (uint)_buffer.Length;
            }
        }

        /// <summary>
        /// Creates an object which can browser the tags in the buffer.
        /// </summary>
        /// <param name="context"></param>
        /// <param name="view"></param>
        /// <param name="referenceType"></param>
        /// <param name="includeSubtypes"></param>
        /// <param name="browseDirection"></param>
        /// <param name="browseName"></param>
        /// <param name="additionalReferences"></param>
        /// <param name="internalOnly"></param>
        public override INodeBrowser CreateBrowser(
            ISystemContext context,
            ViewDescription view,
            NodeId referenceType,
            bool includeSubtypes,
            BrowseDirection browseDirection,
            QualifiedName browseName,
            IEnumerable<IReference> additionalReferences,
            bool internalOnly)
        {
            NodeBrowser browser = new MemoryBufferBrowser(
                context,
                view,
                referenceType,
                includeSubtypes,
                browseDirection,
                browseName,
                additionalReferences,
                internalOnly,
                this);

            PopulateBrowser(context, browser);

            return browser;
        }

        /// <summary>
        /// Handles the read operation for an invidual tag.
        /// </summary>
        /// <param name="context"></param>
        /// <param name="node"></param>
        /// <param name="indexRange"></param>
        /// <param name="dataEncoding"></param>
        /// <param name="value"></param>
        /// <param name="statusCode"></param>
        /// <param name="timestamp"></param>
        public ServiceResult ReadTagValue(
#pragma warning disable IDE0079 // Remove unnecessary suppression
#pragma warning disable IDE0060 // Remove unused parameter
            ISystemContext context,
#pragma warning restore IDE0060 // Remove unused parameter
#pragma warning restore IDE0079 // Remove unnecessary suppression
            NodeState node,
            NumericRange indexRange,
            QualifiedName dataEncoding,
            ref object value,
            ref StatusCode statusCode,
            ref DateTime timestamp)
        {
            if (node is not MemoryTagState tag)
            {
                return StatusCodes.BadNodeIdUnknown;
            }

            if (NumericRange.Empty != indexRange)
            {
                return StatusCodes.BadIndexRangeInvalid;
            }

            if (!QualifiedName.IsNull(dataEncoding))
            {
                return StatusCodes.BadDataEncodingInvalid;
            }

            var offset = (int)tag.Offset;

            lock (_dataLock)
            {
                if (offset < 0 || offset >= _buffer.Length)
                {
                    return StatusCodes.BadNodeIdUnknown;
                }

                if (_buffer == null)
                {
                    return StatusCodes.BadOutOfService;
                }

                value = GetValueAtOffset(offset).Value;
            }

            statusCode = StatusCodes.Good;
            timestamp = _lastScanTime;

            return ServiceResult.Good;
        }

        /// <summary>
        /// Handles a write operation for an individual tag.
        /// </summary>
        /// <param name="context"></param>
        /// <param name="node"></param>
        /// <param name="indexRange"></param>
        /// <param name="dataEncoding"></param>
        /// <param name="value"></param>
        /// <param name="statusCode"></param>
        /// <param name="timestamp"></param>
        public ServiceResult WriteTagValue(
#pragma warning disable IDE0079 // Remove unnecessary suppression
#pragma warning disable IDE0060 // Remove unused parameter
            ISystemContext context,
#pragma warning restore IDE0060 // Remove unused parameter
#pragma warning restore IDE0079 // Remove unnecessary suppression
            NodeState node,
            NumericRange indexRange,
            QualifiedName dataEncoding,
            ref object value,
            ref StatusCode statusCode,
            ref DateTime timestamp)
        {
            if (node is not MemoryTagState tag)
            {
                return StatusCodes.BadNodeIdUnknown;
            }

            if (NumericRange.Empty != indexRange)
            {
                return StatusCodes.BadIndexRangeInvalid;
            }

            if (!QualifiedName.IsNull(dataEncoding))
            {
                return StatusCodes.BadDataEncodingInvalid;
            }

            if (statusCode != StatusCodes.Good)
            {
                return StatusCodes.BadWriteNotSupported;
            }

            if (timestamp != DateTime.MinValue)
            {
                return StatusCodes.BadWriteNotSupported;
            }

            var changed = false;
            var offset = (int)tag.Offset;

            lock (_dataLock)
            {
                if (offset < 0 || offset >= _buffer.Length)
                {
                    return StatusCodes.BadNodeIdUnknown;
                }

                if (_buffer == null)
                {
                    return StatusCodes.BadOutOfService;
                }

                byte[] bytes = null;

                switch (ElementType)
                {
                    case BuiltInType.UInt32:
                        {
                            if (value is not uint valueToWrite)
                            {
                                return StatusCodes.BadTypeMismatch;
                            }

                            bytes = BitConverter.GetBytes(valueToWrite);
                            break;
                        }

                    case BuiltInType.Double:
                        {
                            if (value is not double valueToWrite)
                            {
                                return StatusCodes.BadTypeMismatch;
                            }

                            bytes = BitConverter.GetBytes(valueToWrite);
                            break;
                        }

                    default:
                        {
                            return StatusCodes.BadNodeIdUnknown;
                        }
                }

                for (var ii = 0; ii < bytes.Length; ii++)
                {
                    if (!changed && _buffer[offset + ii] != bytes[ii])
                    {
                        changed = true;
                    }

                    _buffer[offset + ii] = bytes[ii];
                }
            }

            if (changed)
            {
                OnBufferChanged(offset);
            }

            return ServiceResult.Good;
        }

        /// <summary>
        /// Returns the value at the specified offset.
        /// </summary>
        /// <param name="offset"></param>
        public Variant GetValueAtOffset(int offset)
        {
            lock (_dataLock)
            {
                if (offset < 0 || offset >= _buffer.Length)
                {
                    return Variant.Null;
                }

                if (_buffer == null)
                {
                    return Variant.Null;
                }

                switch (ElementType)
                {
                    case BuiltInType.UInt32:
                        {
                            return new Variant(BitConverter.ToUInt32(_buffer, offset));
                        }

                    case BuiltInType.Double:
                        {
                            return new Variant(BitConverter.ToDouble(_buffer, offset));
                        }
                }

                return Variant.Null;
            }
        }

        /// <summary>
        /// Initializes the instance with the context for the node being monitored.
        /// </summary>
        /// <param name="server"></param>
        /// <param name="nodeManager"></param>
        public void InitializeMonitoring(
            IServerInternal server,
            INodeManager nodeManager)
        {
            lock (_dataLock)
            {
                Server = server;
                NodeManager = nodeManager;
                _nonValueMonitoredItems = [];
            }
        }

        /// <summary>
        /// Creates a new data change monitored item.
        /// </summary>
        /// <param name="tag"></param>
        /// <param name="monitoredItemId"></param>
        /// <param name="itemToMonitor"></param>
        /// <param name="diagnosticsMasks"></param>
        /// <param name="timestampsToReturn"></param>
        /// <param name="monitoringMode"></param>
        /// <param name="clientHandle"></param>
        /// <param name="samplingInterval"></param>
        public MemoryBufferMonitoredItem CreateDataChangeItem(
            MemoryTagState tag,
            uint monitoredItemId,
            ReadValueId itemToMonitor,
            DiagnosticsMasks diagnosticsMasks,
            TimestampsToReturn timestampsToReturn,
            MonitoringMode monitoringMode,
            uint clientHandle,
            double samplingInterval)
        {
            lock (_dataLock)
            {
                var monitoredItem = new MemoryBufferMonitoredItem(
                    Server,
                    NodeManager,
                    this,
                    tag.Offset,
                    0,
                    monitoredItemId,
                    itemToMonitor,
                    diagnosticsMasks,
                    timestampsToReturn,
                    monitoringMode,
                    clientHandle,
                    null,
                    null,
                    null,
                    samplingInterval,
                    0,
                    false,
                    0);

                if (itemToMonitor.AttributeId != Attributes.Value)
                {
                    _nonValueMonitoredItems.Add(monitoredItem.Id, monitoredItem);
                    return monitoredItem;
                }

                var elementCount = (int)(SizeInBytes.Value / ElementSize);

                if (_monitoringTable == null)
                {
                    _monitoringTable = new MemoryBufferMonitoredItem[elementCount][];
                    _scanTimer = new Timer(DoScan, null, 100, 100);
                }

                var elementOffet = (int)(tag.Offset / ElementSize);

                var monitoredItems = _monitoringTable[elementOffet];

                if (monitoredItems == null)
                {
                    monitoredItems = new MemoryBufferMonitoredItem[1];
                }
                else
                {
                    monitoredItems = new MemoryBufferMonitoredItem[monitoredItems.Length + 1];
                    _monitoringTable[elementOffet].CopyTo(monitoredItems, 0);
                }

                monitoredItems[^1] = monitoredItem;
                _monitoringTable[elementOffet] = monitoredItems;
                _itemCount++;

                return monitoredItem;
            }
        }

        /// <summary>
        /// Scans the buffer and updates every other element.
        /// </summary>
        /// <param name="state"></param>
        private void DoScan(object state)
        {
            var start1 = DateTime.UtcNow;

            lock (_dataLock)
            {
                for (var ii = 0; ii < _buffer.Length; ii += _elementSize)
                {
                    _buffer[ii]++;

                    // notify any monitored items that the value has changed.
                    OnBufferChanged(ii);
                }

                _lastScanTime = DateTime.UtcNow;
            }

            var end1 = DateTime.UtcNow;

            var delta1 = ((double)(end1.Ticks - start1.Ticks)) / TimeSpan.TicksPerMillisecond;

            if (delta1 > 100)
            {
                Debug.WriteLine("SAMPLING DELAY ({0}ms)", delta1);
            }
        }

        /// <summary>
        /// Deletes the monitored item.
        /// </summary>
        /// <param name="monitoredItem"></param>
        public void DeleteItem(MemoryBufferMonitoredItem monitoredItem)
        {
            lock (_dataLock)
            {
                if (monitoredItem.AttributeId != Attributes.Value)
                {
                    _nonValueMonitoredItems.Remove(monitoredItem.Id);
                    return;
                }

                if (_monitoringTable != null)
                {
                    var elementOffet = (int)(monitoredItem.Offset / ElementSize);

                    var monitoredItems = _monitoringTable[elementOffet];

                    if (monitoredItems != null)
                    {
                        var index = -1;

                        for (var ii = 0; ii < monitoredItems.Length; ii++)
                        {
                            if (ReferenceEquals(monitoredItems[ii], monitoredItem))
                            {
                                index = ii;
                                break;
                            }
                        }

                        if (index >= 0)
                        {
                            _itemCount--;

                            if (monitoredItems.Length == 1)
                            {
                                monitoredItems = null;
                            }
                            else
                            {
                                monitoredItems = new MemoryBufferMonitoredItem[monitoredItems.Length - 1];

                                Array.Copy(_monitoringTable[elementOffet], 0, monitoredItems, 0, index);
                                Array.Copy(_monitoringTable[elementOffet], index + 1, monitoredItems, index, monitoredItems.Length - index);
                            }

                            _monitoringTable[elementOffet] = monitoredItems;
                        }
                    }
                }
            }
        }

        /// <summary>
        /// Handles change events raised by the node.
        /// </summary>
        /// <param name="offset"></param>
        public void OnBufferChanged(int offset)
        {
            lock (_dataLock)
            {
                if (_monitoringTable != null)
                {
                    var elementOffet = (int)(offset / ElementSize);

                    var monitoredItems = _monitoringTable[elementOffet];

                    if (monitoredItems != null)
                    {
                        var value = new DataValue
                        {
                            WrappedValue = GetValueAtOffset(offset),
                            StatusCode = StatusCodes.Good,
                            ServerTimestamp = DateTime.UtcNow,
                            SourceTimestamp = _lastScanTime
                        };

                        for (var ii = 0; ii < monitoredItems.Length; ii++)
                        {
                            monitoredItems[ii].QueueValue(value, null);
                            _updateCount++;
                        }
                    }
                }
            }
        }

        private void ScanTimer_Tick(object sender, EventArgs e)
        {
            DoScan(null);
        }

        private void PublishTimer_Tick(object sender, EventArgs e)
        {
            var start1 = DateTime.UtcNow;

            lock (_dataLock)
            {
                if (_itemCount > 0 && _updateCount < _itemCount)
                {
                    Debug.WriteLine("{0:HH:mm:ss.fff} MEMORYBUFFER Reported  {1}/{2} items ***.", DateTime.Now, _updateCount, _itemCount);
                }

                _updateCount = 0;
            }

            var end1 = DateTime.UtcNow;

            var delta1 = ((double)(end1.Ticks - start1.Ticks)) / TimeSpan.TicksPerMillisecond;

            if (delta1 > 100)
            {
                Debug.WriteLine("****** PUBLISH DELAY ({0}ms) ******", delta1);
            }
        }

        private readonly Lock _dataLock = new();
        private MemoryBufferMonitoredItem[][] _monitoringTable;
        private Dictionary<uint, MemoryBufferMonitoredItem> _nonValueMonitoredItems;
        private int _elementSize;
        private DateTime _lastScanTime;
        private byte[] _buffer;
        private Timer _scanTimer;
        private int _updateCount;
        private int _itemCount;
    }
}
