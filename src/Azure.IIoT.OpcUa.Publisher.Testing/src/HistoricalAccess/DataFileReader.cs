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
    using Opc.Ua.Test;
    using System;
    using System.Data;
    using System.Globalization;
    using System.IO;
    using System.Text;
    using System.Xml;

    /// <summary>
    /// Reads an item history from a file.
    /// </summary>
    public class DataFileReader
    {
        private readonly TimeService _timeService;

        public DataFileReader(TimeService timeService)
        {
            _timeService = timeService;
        }

        /// <summary>
        /// Creates a new data set.
        /// </summary>
        private DataSet CreateDataSet()
        {
            var dataset = new DataSet();

            dataset.Tables.Add("CurrentData");

            dataset.Tables[0].Columns.Add("SourceTimestamp", typeof(DateTime));
            dataset.Tables[0].Columns.Add("ServerTimestamp", typeof(DateTime));
            dataset.Tables[0].Columns.Add("Value", typeof(DataValue));
            dataset.Tables[0].Columns.Add("DataType", typeof(BuiltInType));
            dataset.Tables[0].Columns.Add("ValueRank", typeof(int));

            dataset.Tables[0].DefaultView.Sort = "SourceTimestamp";

            dataset.Tables.Add("ModifiedData");

            dataset.Tables[1].Columns.Add("SourceTimestamp", typeof(DateTime));
            dataset.Tables[1].Columns.Add("ServerTimestamp", typeof(DateTime));
            dataset.Tables[1].Columns.Add("Value", typeof(DataValue));
            dataset.Tables[1].Columns.Add("DataType", typeof(BuiltInType));
            dataset.Tables[1].Columns.Add("ValueRank", typeof(int));
            dataset.Tables[1].Columns.Add("UpdateType", typeof(int));
            dataset.Tables[1].Columns.Add("ModificationInfo", typeof(ModificationInfo));

            dataset.Tables[1].DefaultView.Sort = "SourceTimestamp";

            dataset.Tables.Add("AnnotationData");

            dataset.Tables[2].Columns.Add("SourceTimestamp", typeof(DateTime));
            dataset.Tables[2].Columns.Add("ServerTimestamp", typeof(DateTime));
            dataset.Tables[2].Columns.Add("Value", typeof(DataValue));
            dataset.Tables[2].Columns.Add("DataType", typeof(BuiltInType));
            dataset.Tables[2].Columns.Add("ValueRank", typeof(int));
            dataset.Tables[2].Columns.Add("Annotation", typeof(Annotation));

            dataset.Tables[2].DefaultView.Sort = "SourceTimestamp";

            return dataset;
        }

#pragma warning disable IDE0079 // Remove unnecessary suppression
#pragma warning disable IDE0060 // Remove unused parameter
        /// <summary>
        /// Loads the item configuaration.
        /// </summary>
        /// <param name="context"></param>
        /// <param name="item"></param>
        public bool LoadConfiguration(ISystemContext context, ArchiveItem item)
#pragma warning restore IDE0060 // Remove unused parameter
#pragma warning restore IDE0079 // Remove unnecessary suppression
        {
            using var reader = item.OpenArchive();
            while (!reader.EndOfStream)
            {
                var line = reader.ReadLine();

                // check for end or error.
                if (line == null)
                {
                    break;
                }

                // ignore blank lines.
                line = line.Trim();

                if (string.IsNullOrEmpty(line))
                {
                    continue;
                }

                // ignore commented out lines.
                if (line.StartsWith("//", StringComparison.CurrentCulture))
                {
                    continue;
                }

                // get data type.
                if (!ExtractField(1, ref line, out BuiltInType dataType))
                {
                    return false;
                }

                // get value rank.
                if (!ExtractField(1, ref line, out
                int valueRank))
                {
                    return false;
                }

                // get sampling interval.
                if (!ExtractField(1, ref line, out int samplingInterval))
                {
                    return false;
                }

                // get simulation type.
                if (!ExtractField(1, ref line, out int simulationType))
                {
                    return false;
                }

                // get simulation amplitude.
                if (!ExtractField(1, ref line, out int amplitude))
                {
                    return false;
                }

                // get simulation period.
                if (!ExtractField(1, ref line, out int period))
                {
                    return false;
                }

                // get flag indicating whether new data is generated.
                if (!ExtractField(1, ref line, out int archiving))
                {
                    return false;
                }

                // get flag indicating whether stepped interpolation is used.
                if (!ExtractField(1, ref line, out int stepped))
                {
                    return false;
                }

                // get flag indicating whether sloped interpolation should be used.
                if (!ExtractField(1, ref line, out int useSlopedExtrapolation))
                {
                    return false;
                }

                // get flag indicating whether sloped interpolation should be used.
                if (!ExtractField(1, ref line, out int treatUncertainAsBad))
                {
                    return false;
                }

                // get the maximum permitted of bad data in an interval.
                if (!ExtractField(1, ref line, out int percentDataBad))
                {
                    return false;
                }

                // get the minimum amount of good data in an interval.
                if (!ExtractField(1, ref line, out int percentDataGood))
                {
                    return false;
                }

                // update the item.
                item.DataType = dataType;
                item.ValueRank = valueRank;
                item.SimulationType = simulationType;
                item.Amplitude = amplitude;
                item.Period = period;
                item.SamplingInterval = samplingInterval;
                item.Archiving = archiving != 0;
                item.Stepped = stepped != 0;
                item.AggregateConfiguration = new AggregateConfiguration
                {
                    UseServerCapabilitiesDefaults = false,
                    UseSlopedExtrapolation = useSlopedExtrapolation != 0,
                    TreatUncertainAsBad = treatUncertainAsBad != 0,
                    PercentDataBad = (byte)percentDataBad,
                    PercentDataGood = (byte)percentDataGood
                };
                break;
            }

            return true;
        }

        /// <summary>
        /// Creates new data.
        /// </summary>
        /// <param name="item"></param>
        public void CreateData(ArchiveItem item)
        {
            // get the data set to use.
            var dataset = item.DataSet ?? CreateDataSet();

            // generate one hour worth of data by default.
            var startTime = _timeService.UtcNow.AddHours(-1);
            startTime = new DateTime(startTime.Year, startTime.Month, startTime.Day, startTime.Hour, 0, 0, DateTimeKind.Utc);

            // check for existing data.
            if (dataset.Tables[0].Rows.Count > 0)
            {
                var index = dataset.Tables[0].DefaultView.Count;
                _ = (DateTime)dataset.Tables[0].DefaultView[index - 1].Row[0];
                _ = startTime.AddMilliseconds(item.SamplingInterval);
            }

            var currentTime = startTime;
            var generator = new TestDataGenerator();

            var endTime = startTime.AddHours(1);
            while (currentTime < endTime)
            {
                var dataValue = new DataValue
                {
                    SourceTimestamp = currentTime,
                    ServerTimestamp = currentTime,
                    StatusCode = StatusCodes.Good
                };

                // generate random value.
                if (item.ValueRank < 0)
                {
                    dataValue.Value = generator.GetRandom(item.DataType);
                }
                else
                {
                    dataValue.Value = generator.GetRandomArray(item.DataType, 10, false);
                }

                // add record to table.
                var row = dataset.Tables[0].NewRow();

                row[0] = dataValue.SourceTimestamp;
                row[1] = dataValue.ServerTimestamp;
                row[2] = dataValue;
                row[3] = dataValue.WrappedValue.TypeInfo.BuiltInType;
                row[4] = dataValue.WrappedValue.TypeInfo.ValueRank;

                dataset.Tables[0].Rows.Add(row);

                // increment timestamp.
                currentTime = currentTime.AddMilliseconds(item.SamplingInterval);
            }

            dataset.AcceptChanges();
            item.DataSet = dataset;
        }

        /// <summary>
        /// Loads the history for the item.
        /// </summary>
        /// <param name="context"></param>
        /// <param name="item"></param>
        public void LoadHistoryData(ISystemContext context, ArchiveItem item)
        {
            // use the beginning of the current hour for the baseline.
            var baseline = _timeService.UtcNow;
            baseline = new DateTime(baseline.Year, baseline.Month, baseline.Day, baseline.Hour, 0, 0, DateTimeKind.Utc);

            using (var reader = item.OpenArchive())
            {
                // skip configuration line.
                reader.ReadLine();
                item.DataSet = LoadData(context, baseline, reader);
            }

            // create a random dataset if nothing found in the archive,
            if (item.DataSet == null || item.DataSet.Tables[0].Rows.Count == 0)
            {
                CreateData(item);
            }

            // update the timestamp.
            item.LastLoadTime = _timeService.UtcNow;
        }

        /// <summary>
        /// Loads the history data from a stream.
        /// </summary>
        /// <param name="context"></param>
        /// <param name="baseline"></param>
        /// <param name="reader"></param>
        private DataSet LoadData(ISystemContext context, DateTime baseline, StreamReader reader)
        {
            var dataset = CreateDataSet();

            var messageContext = new ServiceMessageContext();

            if (context != null)
            {
                messageContext.NamespaceUris = context.NamespaceUris;
                messageContext.ServerUris = context.ServerUris;
                messageContext.Factory = context.EncodeableFactory;
            }
            else
            {
                messageContext.NamespaceUris = ServiceMessageContext.GlobalContext.NamespaceUris;
                messageContext.ServerUris = ServiceMessageContext.GlobalContext.ServerUris;
                messageContext.Factory = ServiceMessageContext.GlobalContext.Factory;
            }

            var valueType = BuiltInType.String;
            var value = Variant.Null;
            var annotationTimeOffet = 0;
            var annotationUser = string.Empty;
            var annotationMessage = string.Empty;
            var lineCount = 0;

            while (!reader.EndOfStream)
            {
                var line = reader.ReadLine();

                // check for end or error.
                if (line == null)
                {
                    break;
                }

                // ignore blank lines.
                line = line.Trim();
                lineCount++;

                if (string.IsNullOrEmpty(line))
                {
                    continue;
                }

                // ignore commented out lines.
                if (line.StartsWith("//", StringComparison.CurrentCulture))
                {
                    continue;
                }

                // get source time.
                if (!ExtractField(lineCount, ref line, out int sourceTimeOffset))
                {
                    continue;
                }

                // get server time.
                if (!ExtractField(lineCount, ref line, out int serverTimeOffset))
                {
                    continue;
                }

                // get status code.
                if (!ExtractField(lineCount, ref line, out StatusCode status))
                {
                    continue;
                }

                // get modification type.
                if (!ExtractField(lineCount, ref line, out
                int recordType))
                {
                    continue;
                }

                // get modification time.
                if (!ExtractField(lineCount, ref line, out
                int modificationTimeOffet))
                {
                    continue;
                }

                // get modification user.
                if (!ExtractField(ref line, out var modificationUser))
                {
                    continue;
                }

                if (recordType >= 0)
                {
                    // get value type.
                    if (!ExtractField(lineCount, ref line, out valueType))
                    {
                        continue;
                    }

                    // get value.
                    if (!ExtractField(lineCount, ref line, messageContext, valueType, out value))
                    {
                        continue;
                    }
                }
                else
                {
                    // get annotation time.
                    if (!ExtractField(lineCount, ref line, out annotationTimeOffet))
                    {
                        continue;
                    }

                    // get annotation user.
                    if (!ExtractField(ref line, out annotationUser))
                    {
                        continue;
                    }

                    // get annotation message.
                    if (!ExtractField(ref line, out annotationMessage))
                    {
                        continue;
                    }
                }

                // add values to data table.
                var dataValue = new DataValue
                {
                    WrappedValue = value,
                    SourceTimestamp = baseline.AddMilliseconds(sourceTimeOffset),
                    ServerTimestamp = baseline.AddMilliseconds(serverTimeOffset),
                    StatusCode = status
                };

                DataRow row;
                if (recordType == 0)
                {
                    row = dataset.Tables[0].NewRow();

                    row[0] = dataValue.SourceTimestamp;
                    row[1] = dataValue.ServerTimestamp;
                    row[2] = dataValue;
                    row[3] = valueType;
                    row[4] = (value.TypeInfo != null) ? value.TypeInfo.ValueRank : ValueRanks.Any;

                    dataset.Tables[0].Rows.Add(row);
                }
                else if (recordType > 0)
                {
                    row = dataset.Tables[1].NewRow();

                    row[0] = dataValue.SourceTimestamp;
                    row[1] = dataValue.ServerTimestamp;
                    row[2] = dataValue;
                    row[3] = valueType;
                    row[4] = (value.TypeInfo != null) ? value.TypeInfo.ValueRank : ValueRanks.Any;
                    row[5] = recordType;

                    row[6] = new ModificationInfo
                    {
                        UpdateType = (HistoryUpdateType)recordType,
                        ModificationTime = baseline.AddMilliseconds(modificationTimeOffet),
                        UserName = modificationUser
                    };

                    dataset.Tables[1].Rows.Add(row);
                }
                else if (recordType < 0)
                {
                    row = dataset.Tables[2].NewRow();

                    var annotation = new Annotation
                    {
                        AnnotationTime = baseline.AddMilliseconds(annotationTimeOffet),
                        UserName = annotationUser,
                        Message = annotationMessage
                    };
                    dataValue.WrappedValue = new ExtensionObject(annotation);

                    row[0] = dataValue.SourceTimestamp;
                    row[1] = dataValue.ServerTimestamp;
                    row[2] = dataValue;
                    row[3] = valueType;
                    row[4] = (value.TypeInfo != null) ? value.TypeInfo.ValueRank : ValueRanks.Any;
                    row[5] = annotation;

                    dataset.Tables[2].Rows.Add(row);
                }

                dataset.AcceptChanges();
            }

            return dataset;
        }

        /// <summary>
        /// Extracts the next comma seperated field from the line.
        /// </summary>
        /// <param name="line"></param>
        private string ExtractField(ref string line)
        {
            var field = line;
            var index = field.IndexOf(',');

            if (index >= 0)
            {
                field = field[..index];
                line = line[(index + 1)..];
            }

            field = field.Trim();

            if (string.IsNullOrEmpty(field))
            {
                return null;
            }

            return field;
        }

        /// <summary>
        /// Extracts an integer value from the line.
        /// </summary>
        /// <param name="line"></param>
        /// <param name="value"></param>
        private bool ExtractField(ref string line, out string value)
        {
            value = string.Empty;
            var field = ExtractField(ref line);

            if (field == null)
            {
                return true;
            }

            value = field;
            return true;
        }

        /// <summary>
        /// Extracts an integer value from the line.
        /// </summary>
        /// <param name="lineCount"></param>
        /// <param name="line"></param>
        /// <param name="value"></param>
        private bool ExtractField(int lineCount, ref string line, out int value)
        {
            value = 0;
            var field = ExtractField(ref line);

            if (field == null)
            {
                return true;
            }

            try
            {
                value = Convert.ToInt32(field);
            }
            catch (Exception e)
            {
                Utils.Trace("PARSE ERROR [Line:{0}] - '{1}': {2}", lineCount, field, e.Message);
                return false;
            }

            return true;
        }

        /// <summary>
        /// Extracts a StatusCode value from the line.
        /// </summary>
        /// <param name="lineCount"></param>
        /// <param name="line"></param>
        /// <param name="value"></param>
        private bool ExtractField(int lineCount, ref string line, out StatusCode value)
        {
            value = 0;
            var field = ExtractField(ref line);

            if (field == null)
            {
                return true;
            }

            if (field.StartsWith("0x", StringComparison.CurrentCulture))
            {
                field = field[2..];
            }

            try
            {
                var code = Convert.ToUInt32(field, 16);
                value = new StatusCode(code);
            }
            catch (Exception e)
            {
                Utils.Trace("PARSE ERROR [Line:{0}] - '{1}': {2}", lineCount, field, e.Message);
                return false;
            }

            return true;
        }

        /// <summary>
        /// Extracts a BuiltInType value from the line.
        /// </summary>
        /// <param name="lineCount"></param>
        /// <param name="line"></param>
        /// <param name="value"></param>
        private bool ExtractField(int lineCount, ref string line, out BuiltInType value)
        {
            value = BuiltInType.String;
            var field = ExtractField(ref line);

            if (field == null)
            {
                return true;
            }

            try
            {
                value = Enum.Parse<BuiltInType>(field);
            }
            catch (Exception e)
            {
                Utils.Trace("PARSE ERROR [Line:{0}] - '{1}': {2}", lineCount, field, e.Message);
                return false;
            }

            return true;
        }

        /// <summary>
        /// Extracts a BuiltInType value from the line.
        /// </summary>
        /// <param name="lineCount"></param>
        /// <param name="line"></param>
        /// <param name="context"></param>
        /// <param name="valueType"></param>
        /// <param name="value"></param>
        private static bool ExtractField(int lineCount, ref string line,
            IServiceMessageContext context, BuiltInType valueType, out Variant value)
        {
            value = Variant.Null;
            var field = line;

            if (field == null)
            {
                return true;
            }

            if (valueType == BuiltInType.Null)
            {
                return true;
            }

            var builder = new StringBuilder()
                .AppendFormat(CultureInfo.InvariantCulture, "<Value xmlns=\"{0}\">", Opc.Ua.Namespaces.OpcUaXsd)
                .AppendFormat(CultureInfo.InvariantCulture, "<{0}>", valueType)
                .Append(line)
                .AppendFormat(CultureInfo.InvariantCulture, "</{0}>", valueType)
                .Append("</Value>");

            var document = new XmlDocument
            {
                InnerXml = builder.ToString()
            };

            XmlDecoder decoder = null;
            try
            {
                decoder = new XmlDecoder(document.DocumentElement, context);
                value = decoder.ReadVariant(null);
            }
            catch (Exception e)
            {
                Utils.Trace("PARSE ERROR [Line:{0}] - '{1}': {2}", lineCount, field, e.Message);
                return false;
            }
            finally
            {
                decoder?.Dispose();
            }

            return true;
        }
    }
}
