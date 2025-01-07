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

namespace HistoricalAccess
{
    using Opc.Ua;
    using Opc.Ua.Server;
    using Opc.Ua.Test;
    using System;
    using System.Collections.Generic;
    using System.Data;

    /// <summary>
    /// Stores the metadata for a node representing an item in the archive.
    /// </summary>
    public class ArchiveItemState : DataItemState
    {
        /// <summary>
        /// Creates a new instance of a item.
        /// </summary>
        /// <param name="context"></param>
        /// <param name="item"></param>
        /// <param name="namespaceIndex"></param>
        /// <param name="timeService"></param>
        public ArchiveItemState(ISystemContext context, ArchiveItem item, ushort namespaceIndex, TimeService timeService)
        :
            base(null)
        {
            ArchiveItem = item;
            _timeService = timeService;
            TypeDefinitionId = VariableTypeIds.DataItemType;
            SymbolicName = ArchiveItem.Name;
            NodeId = ConstructId(ArchiveItem.UniquePath, namespaceIndex);
            BrowseName = new QualifiedName(ArchiveItem.Name, namespaceIndex);
            DisplayName = new LocalizedText(BrowseName.Name);
            Description = null;
            WriteMask = 0;
            UserWriteMask = 0;
            DataType = DataTypeIds.BaseDataType;
            ValueRank = ValueRanks.Scalar;
            AccessLevel = AccessLevels.HistoryReadOrWrite | AccessLevels.CurrentRead;
            UserAccessLevel = AccessLevels.HistoryReadOrWrite | AccessLevels.CurrentRead;
            MinimumSamplingInterval = MinimumSamplingIntervals.Indeterminate;
            Historizing = true;

            _annotations = new PropertyState<Annotation>(this)
            {
                ReferenceTypeId = ReferenceTypeIds.HasProperty,
                TypeDefinitionId = VariableTypeIds.PropertyType,
                SymbolicName = BrowseNames.Annotations,
                BrowseName = BrowseNames.Annotations
            };
            _annotations.DisplayName = new LocalizedText(_annotations.BrowseName.Name);
            _annotations.Description = null;
            _annotations.WriteMask = 0;
            _annotations.UserWriteMask = 0;
            _annotations.DataType = DataTypeIds.Annotation;
            _annotations.ValueRank = ValueRanks.Scalar;
            _annotations.AccessLevel = AccessLevels.HistoryReadOrWrite;
            _annotations.UserAccessLevel = AccessLevels.HistoryReadOrWrite;
            _annotations.MinimumSamplingInterval = MinimumSamplingIntervals.Indeterminate;
            _annotations.Historizing = false;
            AddChild(_annotations);

            _annotations.NodeId = NodeTypes.ConstructIdForComponent(_annotations, namespaceIndex);

            _configuration = new HistoricalDataConfigurationState(this);
            _configuration.MaxTimeInterval = new PropertyState<double>(_configuration);
            _configuration.MinTimeInterval = new PropertyState<double>(_configuration);
            _configuration.StartOfArchive = new PropertyState<DateTime>(_configuration);
            _configuration.StartOfOnlineArchive = new PropertyState<DateTime>(_configuration);

            _configuration.Create(
                context,
                null,
                BrowseNames.HAConfiguration,
                null,
                true);

            _configuration.SymbolicName = BrowseNames.HAConfiguration;
            _configuration.ReferenceTypeId = ReferenceTypeIds.HasHistoricalConfiguration;

            AddChild(_configuration);
        }

        /// <summary>
        /// Loads the configuration.
        /// </summary>
        /// <param name="context"></param>
        public void LoadConfiguration(SystemContext context)
        {
            var reader = new DataFileReader(_timeService);

            if (reader.LoadConfiguration(context, ArchiveItem))
            {
                DataType = (uint)ArchiveItem.DataType;
                ValueRank = ArchiveItem.ValueRank;
                Historizing = ArchiveItem.Archiving;

                _configuration.MinTimeInterval.Value = ArchiveItem.SamplingInterval;
                _configuration.MaxTimeInterval.Value = ArchiveItem.SamplingInterval;
                _configuration.Stepped.Value = ArchiveItem.Stepped;

                var configuration = ArchiveItem.AggregateConfiguration;
                _configuration.AggregateConfiguration.PercentDataGood.Value = configuration.PercentDataGood;
                _configuration.AggregateConfiguration.PercentDataBad.Value = configuration.PercentDataBad;
                _configuration.AggregateConfiguration.UseSlopedExtrapolation.Value = configuration.UseSlopedExtrapolation;
                _configuration.AggregateConfiguration.TreatUncertainAsBad.Value = configuration.TreatUncertainAsBad;
            }
        }

        /// <summary>
        /// Loads the data.
        /// </summary>
        /// <param name="context"></param>
        public void ReloadFromSource(SystemContext context)
        {
            LoadConfiguration(context);

            if (ArchiveItem.LastLoadTime == DateTime.MinValue
                || (ArchiveItem.Persistent && ArchiveItem.LastLoadTime.AddDays(1) < _timeService.UtcNow))
            {
                var reader = new DataFileReader(_timeService);
                reader.LoadHistoryData(context, ArchiveItem);

                // set the start of the archive.
                if (ArchiveItem.DataSet.Tables[0].DefaultView.Count > 0)
                {
                    _configuration.StartOfArchive.Value = (DateTime)ArchiveItem.DataSet.Tables[0].DefaultView[0].Row[0];
                    _configuration.StartOfOnlineArchive.Value = _configuration.StartOfArchive.Value;
                }

                if (ArchiveItem.Archiving)
                {
                    // save the pattern used to produce new data.
                    _pattern = [];

                    foreach (DataRowView row in ArchiveItem.DataSet.Tables[0].DefaultView)
                    {
                        var value = (DataValue)row.Row[2];
                        _pattern.Add(value);
                        _nextSampleTime = value.SourceTimestamp.AddMilliseconds(ArchiveItem.SamplingInterval);
                    }

                    // fill in data until the present time.
                    _patternIndex = 0;
                    NewSamples(context);
                }
            }
        }

        /// <summary>
        /// Creates a new sample.
        /// </summary>
        /// <param name="context"></param>
        public IList<DataValue> NewSamples(SystemContext context)
        {
            System.Diagnostics.Contracts.Contract.Assume(context is not null);
            var newSamples = new List<DataValue>();

            while (_pattern != null && _nextSampleTime < _timeService.UtcNow)
            {
                var value = new DataValue
                {
                    WrappedValue = _pattern[_patternIndex].WrappedValue,
                    ServerTimestamp = _nextSampleTime,
                    SourceTimestamp = _nextSampleTime,
                    StatusCode = _pattern[_patternIndex].StatusCode
                };
                _nextSampleTime = value.SourceTimestamp.AddMilliseconds(ArchiveItem.SamplingInterval);
                newSamples.Add(value);

                var row = ArchiveItem.DataSet.Tables[0].NewRow();

                row[0] = value.SourceTimestamp;
                row[1] = value.ServerTimestamp;
                row[2] = value;
                row[3] = value.WrappedValue.TypeInfo.BuiltInType;
                row[4] = value.WrappedValue.TypeInfo.ValueRank;

                ArchiveItem.DataSet.Tables[0].Rows.Add(row);
                _patternIndex = (_patternIndex + 1) % _pattern.Count;
            }

            ArchiveItem.DataSet.AcceptChanges();
            return newSamples;
        }

        /// <summary>
        /// Updates the history.
        /// </summary>
        /// <param name="context"></param>
        /// <param name="value"></param>
        /// <param name="performUpdateType"></param>
        public uint UpdateHistory(SystemContext context, DataValue value, PerformUpdateType performUpdateType)
        {
            var replaced = false;

            if (performUpdateType == PerformUpdateType.Remove)
            {
                return StatusCodes.BadNotSupported;
            }

            if (StatusCode.IsNotBad(value.StatusCode))
            {
                var typeInfo = value.WrappedValue.TypeInfo ?? TypeInfo.Construct(value.Value);

                if (typeInfo == null || typeInfo.BuiltInType != ArchiveItem.DataType || typeInfo.ValueRank != ValueRanks.Scalar)
                {
                    return StatusCodes.BadTypeMismatch;
                }
            }

            var filter = string.Format(System.Globalization.CultureInfo.InvariantCulture, "SourceTimestamp = #{0}#", value.SourceTimestamp);

            var view = new DataView(
                ArchiveItem.DataSet.Tables[0],
                filter,
                null,
                DataViewRowState.CurrentRows);

            DataRow row = null;

            const int ii = 0;
            for (; ii < view.Count;)
            {
                if (performUpdateType == PerformUpdateType.Insert)
                {
                    return StatusCodes.BadEntryExists;
                }

                // add record indicating it was replaced.
                var modifiedRow = ArchiveItem.DataSet.Tables[1].NewRow();

                modifiedRow[0] = view[ii].Row[0];
                modifiedRow[1] = view[ii].Row[1];
                modifiedRow[2] = view[ii].Row[2];
                modifiedRow[3] = view[ii].Row[3];
                modifiedRow[4] = view[ii].Row[4];
                modifiedRow[5] = HistoryUpdateType.Replace;
                modifiedRow[6] = GetModificationInfo(context, HistoryUpdateType.Replace);

                ArchiveItem.DataSet.Tables[1].Rows.Add(modifiedRow);

                replaced = true;
                row = view[ii].Row;
                break;
            }

            // add record indicating it was inserted.
            if (!replaced)
            {
                if (performUpdateType == PerformUpdateType.Replace)
                {
                    return StatusCodes.BadNoEntryExists;
                }

                var modifiedRow = ArchiveItem.DataSet.Tables[1].NewRow();

                modifiedRow[0] = value.SourceTimestamp;
                modifiedRow[1] = value.ServerTimestamp;
                modifiedRow[2] = value;

                if (value.WrappedValue.TypeInfo != null)
                {
                    modifiedRow[3] = value.WrappedValue.TypeInfo.BuiltInType;
                    modifiedRow[4] = value.WrappedValue.TypeInfo.ValueRank;
                }
                else
                {
                    modifiedRow[3] = BuiltInType.Variant;
                    modifiedRow[4] = ValueRanks.Scalar;
                }

                modifiedRow[5] = HistoryUpdateType.Insert;
                modifiedRow[6] = GetModificationInfo(context, HistoryUpdateType.Insert);

                ArchiveItem.DataSet.Tables[1].Rows.Add(modifiedRow);

                row = ArchiveItem.DataSet.Tables[0].NewRow();
            }

            // add/update new record.
            row[0] = value.SourceTimestamp;
            row[1] = value.ServerTimestamp;
            row[2] = value;

            if (value.WrappedValue.TypeInfo != null)
            {
                row[3] = value.WrappedValue.TypeInfo.BuiltInType;
                row[4] = value.WrappedValue.TypeInfo.ValueRank;
            }
            else
            {
                row[3] = BuiltInType.Variant;
                row[4] = ValueRanks.Scalar;
            }

            if (!replaced)
            {
                ArchiveItem.DataSet.Tables[0].Rows.Add(row);
            }

            // accept all changes.
            ArchiveItem.DataSet.AcceptChanges();

            return StatusCodes.Good;
        }

        /// <summary>
        /// Updates the history.
        /// </summary>
        /// <param name="context"></param>
        /// <param name="annotation"></param>
        /// <param name="value"></param>
        /// <param name="performUpdateType"></param>
        public uint UpdateAnnotations(SystemContext context, Annotation annotation, DataValue value, PerformUpdateType performUpdateType)
        {
            System.Diagnostics.Contracts.Contract.Assume(context is not null);
            var replaced = false;

            var filter = string.Format(System.Globalization.CultureInfo.InvariantCulture, "SourceTimestamp = #{0}#", value.SourceTimestamp);

            var view = new DataView(
                ArchiveItem.DataSet.Tables[2],
                filter,
                null,
                DataViewRowState.CurrentRows);

            DataRow row = null;

            for (var ii = 0; ii < view.Count; ii++)
            {
                var current = (Annotation)view[ii].Row[5];

                replaced = current.UserName == annotation.UserName;

                if (performUpdateType == PerformUpdateType.Insert && replaced)
                {
                    return StatusCodes.BadEntryExists;
                }

                if (replaced)
                {
                    row = view[ii].Row;
                    break;
                }
            }

            // add record indicating it was inserted.
            if (!replaced)
            {
                if (performUpdateType == PerformUpdateType.Replace || performUpdateType == PerformUpdateType.Remove)
                {
                    return StatusCodes.BadNoEntryExists;
                }

                row = ArchiveItem.DataSet.Tables[2].NewRow();
            }

            // add/update new record.
            if (performUpdateType != PerformUpdateType.Remove)
            {
                row[0] = value.SourceTimestamp;
                row[1] = value.ServerTimestamp;
                row[2] = new DataValue(new ExtensionObject(annotation), StatusCodes.Good, value.SourceTimestamp, value.ServerTimestamp);
                row[3] = BuiltInType.ExtensionObject;
                row[4] = ValueRanks.Scalar;
                row[5] = annotation;

                if (!replaced)
                {
                    ArchiveItem.DataSet.Tables[2].Rows.Add(row);
                }
            }

            // delete record.
            else
            {
                row.Delete();
            }

            // accept all changes.
            ArchiveItem.DataSet.AcceptChanges();

            return StatusCodes.Good;
        }

        /// <summary>
        /// Deletes a value from the history.
        /// </summary>
        /// <param name="context"></param>
        /// <param name="sourceTimestamp"></param>
        public uint DeleteHistory(SystemContext context, DateTime sourceTimestamp)
        {
            var deleted = false;

            var filter = string.Format(System.Globalization.CultureInfo.InvariantCulture, "SourceTimestamp = #{0}#", sourceTimestamp);

            var view = new DataView(
                ArchiveItem.DataSet.Tables[0],
                filter,
                null,
                DataViewRowState.CurrentRows);

            for (var ii = 0; ii < view.Count; ii++)
            {
                var modifiedRow = ArchiveItem.DataSet.Tables[1].NewRow();

                modifiedRow[0] = view[ii].Row[0];
                modifiedRow[1] = view[ii].Row[1];
                modifiedRow[2] = view[ii].Row[2];
                modifiedRow[3] = view[ii].Row[3];
                modifiedRow[4] = view[ii].Row[4];
                modifiedRow[5] = HistoryUpdateType.Delete;
                modifiedRow[6] = GetModificationInfo(context, HistoryUpdateType.Delete);

                view[ii].Row.Delete();

                ArchiveItem.DataSet.Tables[1].Rows.Add(modifiedRow);
                deleted = true;
            }

            if (!deleted)
            {
                return StatusCodes.BadNoEntryExists;
            }

            ArchiveItem.DataSet.AcceptChanges();
            return StatusCodes.Good;
        }

        /// <summary>
        /// Deletes a value from the history.
        /// </summary>
        /// <param name="context"></param>
        /// <param name="startTime"></param>
        /// <param name="endTime"></param>
        /// <param name="isModified"></param>
        public uint DeleteHistory(SystemContext context, DateTime startTime, DateTime endTime, bool isModified)
        {
            // ensure time goes up.
            if (endTime < startTime)
            {
                (endTime, startTime) = (startTime, endTime);
            }

            var filter = string.Format(
                System.Globalization.CultureInfo.InvariantCulture,
                "SourceTimestamp >= #{0}# AND SourceTimestamp < #{1}#",
                startTime,
                endTime);

            // select the table.
            var table = ArchiveItem.DataSet.Tables[0];

            if (isModified)
            {
                table = ArchiveItem.DataSet.Tables[1];
            }

            // delete the values.
            var view = new DataView(
                table,
                filter,
                null,
                DataViewRowState.CurrentRows);

            var rowsToDelete = new List<DataRow>();

            for (var ii = 0; ii < view.Count; ii++)
            {
                if (!isModified)
                {
                    var modifiedRow = ArchiveItem.DataSet.Tables[1].NewRow();

                    modifiedRow[0] = view[ii].Row[0];
                    modifiedRow[1] = view[ii].Row[1];
                    modifiedRow[2] = view[ii].Row[2];
                    modifiedRow[3] = view[ii].Row[3];
                    modifiedRow[4] = view[ii].Row[4];
                    modifiedRow[5] = HistoryUpdateType.Delete;
                    modifiedRow[6] = GetModificationInfo(context, HistoryUpdateType.Delete);

                    ArchiveItem.DataSet.Tables[1].Rows.Add(modifiedRow);
                }

                rowsToDelete.Add(view[ii].Row);
            }

            // delete rows.
            foreach (var row in rowsToDelete)
            {
                row.Delete();
            }

            // commit all changes.
            ArchiveItem.DataSet.AcceptChanges();

            return StatusCodes.Good;
        }

        /// <summary>
        /// Creates a modification info record.
        /// </summary>
        /// <param name="context"></param>
        /// <param name="updateType"></param>
        private ModificationInfo GetModificationInfo(SystemContext context, HistoryUpdateType updateType)
        {
            var info = new ModificationInfo
            {
                UpdateType = updateType,
                ModificationTime = _timeService.UtcNow
            };

            if (context.OperationContext?.UserIdentity != null)
            {
                info.UserName = context.OperationContext.UserIdentity.DisplayName;
            }

            return info;
        }

        /// <summary>
        /// Reads the history for the specified time range.
        /// </summary>
        /// <param name="startTime"></param>
        /// <param name="endTime"></param>
        /// <param name="isModified"></param>
        public DataView ReadHistory(DateTime startTime, DateTime endTime, bool isModified)
        {
            return ReadHistory(startTime, endTime, isModified, null);
        }

        /// <summary>
        /// Reads the history for the specified time range.
        /// </summary>
        /// <param name="startTime"></param>
        /// <param name="endTime"></param>
        /// <param name="isModified"></param>
        /// <param name="browseName"></param>
        public DataView ReadHistory(DateTime startTime, DateTime endTime, bool isModified, QualifiedName browseName)
        {
            if (startTime != DateTime.MinValue && endTime != DateTime.MaxValue)
            {
                //
            }

            if (isModified)
            {
                return ArchiveItem.DataSet.Tables[1].DefaultView;
            }

            if (browseName == BrowseNames.Annotations)
            {
                return ArchiveItem.DataSet.Tables[2].DefaultView;
            }

            return ArchiveItem.DataSet.Tables[0].DefaultView;
        }

        /// <summary>
        /// Finds the value at or before the timestamp.
        /// </summary>
        /// <param name="view"></param>
        /// <param name="timestamp"></param>
        /// <param name="ignoreBad"></param>
        /// <param name="dataIgnored"></param>
        public int FindValueAtOrBefore(DataView view, DateTime timestamp, bool ignoreBad, out bool dataIgnored)
        {
            dataIgnored = false;

            if (view.Count <= 0)
            {
                return -1;
            }

            var min = 0;
            var max = view.Count;
            var position = (max - min) / 2;

            while (position >= 0 && position < view.Count)
            {
                var current = (DateTime)view[position].Row[0];

                // check for exact match.
                if (current == timestamp)
                {
                    // skip the first timestamp.
                    while (position > 0 && (DateTime)view[position - 1].Row[0] == timestamp)
                    {
                        position--;
                    }

                    return position;
                }

                // move up.
                if (current < timestamp)
                {
                    min = position + 1;
                }

                // move down.
                if (current > timestamp)
                {
                    max = position - 1;
                }

                // not found.
                if (max < min)
                {
                    // find the value before.
                    while (position >= 0)
                    {
                        timestamp = (DateTime)view[position].Row[0];

                        // skip the first timestamp in group.
                        while (position > 0 && (DateTime)view[position - 1].Row[0] == timestamp)
                        {
                            position--;
                        }

                        // ignore bad data.
                        if (ignoreBad)
                        {
                            var value = (DataValue)view[position].Row[2];

                            if (StatusCode.IsBad(value.StatusCode))
                            {
                                position--;
                                dataIgnored = true;
                                continue;
                            }
                        }

                        break;
                    }

                    // return the position.
                    return position;
                }

                position = min + ((max - min) / 2);
            }

            return -1;
        }

        /// <summary>
        /// Returns the next value after the current position.
        /// </summary>
        /// <param name="view"></param>
        /// <param name="position"></param>
        /// <param name="ignoreBad"></param>
        /// <param name="dataIgnored"></param>
        public int FindValueAfter(DataView view, int position, bool ignoreBad, out bool dataIgnored)
        {
            dataIgnored = false;

            if (position < 0 || position >= view.Count)
            {
                return -1;
            }

            var timestamp = (DateTime)view[position].Row[0];

            // skip the current timestamp.
            while (position < view.Count && (DateTime)view[position].Row[0] == timestamp)
            {
                position++;
            }

            if (position >= view.Count)
            {
                return -1;
            }

            // find the value after.
            while (position < view.Count)
            {
                _ = (DateTime)view[position].Row[0];

                // ignore bad data.
                if (ignoreBad)
                {
                    var value = (DataValue)view[position].Row[2];

                    if (StatusCode.IsBad(value.StatusCode))
                    {
                        position++;
                        dataIgnored = true;
                        continue;
                    }
                }

                break;
            }

            if (position >= view.Count)
            {
                return -1;
            }

            // return the position.
            return position;
        }

        /// <summary>
        /// Constructs a node identifier for a item object.
        /// </summary>
        /// <param name="filePath"></param>
        /// <param name="namespaceIndex"></param>
        public static NodeId ConstructId(string filePath, ushort namespaceIndex)
        {
            var parsedNodeId = new ParsedNodeId
            {
                RootId = filePath,
                NamespaceIndex = namespaceIndex,
                RootType = NodeTypes.Item
            };

            return parsedNodeId.Construct();
        }

        /// <summary>
        /// The item in the archive.
        /// </summary>
        public ArchiveItem ArchiveItem { get; }

        /// <summary>
        /// The item in the archive.
        /// </summary>
        public int SubscribeCount { get; set; }

        protected override void Dispose(bool disposing)
        {
            base.Dispose(disposing);
            _annotations?.Dispose();
            _configuration?.Dispose();
        }

        private readonly HistoricalDataConfigurationState _configuration;
        private readonly PropertyState<Annotation> _annotations;
        private readonly TimeService _timeService;
        private List<DataValue> _pattern;
        private int _patternIndex;
        private DateTime _nextSampleTime;
    }
}
