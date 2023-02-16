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

namespace Reference {
    using Opc.Ua;
    using Opc.Ua.Server;
    using System;
    using System.Collections.Generic;
    using System.Data;

    /// <summary>
    /// The interface that a server exposes to objects that it contains.
    /// </summary>
    public static class ServerUtils {

        private enum EventType {
            WriteValue,
            CreateItem,
            ModifyItem,
            QueueValue,
            FilterValue,
            DiscardValue,
            PublishValue
        }

        private class Event {
            public DateTime Timestamp;
            public EventType EventType;
            public NodeId NodeId;
            public uint ServerHandle;
            public DataValue Value;
            public MonitoringParameters Parameters;
            public MonitoringMode MonitoringMode;
        }

        private static readonly Queue<Event> _events = new Queue<Event>();
        private static bool _eventsEnabled;

        /// <summary>
        /// Whether event queuing is enabled.
        /// </summary>
        public static bool EventsEnabled {
            get => _eventsEnabled;

            set {
                if (_eventsEnabled != value) {
                    if (!value) {
                        lock (_events) {
                            _events.Clear();
                        }
                    }
                }

                _eventsEnabled = value;
            }
        }

        /// <summary>
        /// Empties the event queue and saves it in the dataset.
        /// </summary>
        public static DataSet EmptyQueue(DataSet dataset) {
            if (dataset == null) {
                dataset = new DataSet();
                dataset.Tables.Add("MonitoredItems");

                dataset.Tables[0].Columns.Add("Id", typeof(uint));
                dataset.Tables[0].Columns.Add("Timestamp", typeof(string));
                dataset.Tables[0].Columns.Add("EventType", typeof(string));
                dataset.Tables[0].Columns.Add("NodeId", typeof(NodeId));
                dataset.Tables[0].Columns.Add("MonitoringMode", typeof(MonitoringMode));
                dataset.Tables[0].Columns.Add("SamplingInterval", typeof(double));
                dataset.Tables[0].Columns.Add("QueueSize", typeof(uint));
                dataset.Tables[0].Columns.Add("DiscardOldest", typeof(bool));
                dataset.Tables[0].Columns.Add("Filter", typeof(string));
                dataset.Tables[0].Columns.Add("Value", typeof(Variant));
                dataset.Tables[0].Columns.Add("StatusCode", typeof(StatusCode));
                dataset.Tables[0].Columns.Add("SourceTimestamp", typeof(string));
                dataset.Tables[0].Columns.Add("ServerTimestamp", typeof(string));

                dataset.Tables[0].DefaultView.Sort = "Timestamp";
            }

            lock (_events) {
                while (_events.Count > 0) {
                    var e = _events.Dequeue();

                    var row = dataset.Tables[0].NewRow();

                    row[0] = e.ServerHandle;
                    row[1] = e.Timestamp.ToLocalTime().ToString("HH:mm:ss.ffffff");
                    row[2] = e.EventType.ToString();
                    row[3] = e.NodeId;

                    if (e.Parameters != null) {
                        row[4] = e.MonitoringMode;
                        row[5] = e.Parameters.SamplingInterval;
                        row[6] = e.Parameters.QueueSize;
                        row[7] = e.Parameters.DiscardOldest;

                        if (e.Parameters.Filter != null) {
                            row[8] = e.Parameters.Filter.ToString();
                        }
                    }

                    if (e.Value != null) {
                        row[9] = e.Value.WrappedValue;
                        row[10] = e.Value.StatusCode;
                        row[11] = e.Value.ServerTimestamp.ToLocalTime().ToString("HH:mm:ss.fff");
                        row[12] = e.Value.ServerTimestamp.ToLocalTime().ToString("HH:mm:ss.fff");
                    }

                    dataset.Tables[0].Rows.Add(row);
                }
            }

            dataset.AcceptChanges();
            return dataset;
        }

        /// <summary>
        /// Reports a value written.
        /// </summary>
        public static void ReportWriteValue(NodeId nodeId, DataValue value, StatusCode error) {
            if (!_eventsEnabled) {
                return;
            }

            lock (_events) {
                var e = new Event {
                    EventType = EventType.WriteValue,
                    NodeId = nodeId,
                    ServerHandle = 0,
                    Timestamp = HiResClock.UtcNow,
                    Value = value,
                    Parameters = null,
                    MonitoringMode = MonitoringMode.Disabled
                };

                if (StatusCode.IsBad(error)) {
                    e.Value = new DataValue(error) {
                        WrappedValue = value.WrappedValue
                    };
                }

                _events.Enqueue(e);
            }
        }

        /// <summary>
        /// Reports a value queued.
        /// </summary>
        public static void ReportQueuedValue(NodeId nodeId, uint serverHandle, DataValue value) {
            if (!_eventsEnabled) {
                return;
            }

            lock (_events) {
                var e = new Event {
                    EventType = EventType.QueueValue,
                    NodeId = nodeId,
                    ServerHandle = serverHandle,
                    Timestamp = HiResClock.UtcNow,
                    Value = value,
                    Parameters = null,
                    MonitoringMode = MonitoringMode.Disabled
                };
                _events.Enqueue(e);
            }
        }

        /// <summary>
        /// Reports a value excluded by the filter.
        /// </summary>
        public static void ReportFilteredValue(NodeId nodeId, uint serverHandle, DataValue value) {
            if (!_eventsEnabled) {
                return;
            }

            lock (_events) {
                var e = new Event {
                    EventType = EventType.FilterValue,
                    NodeId = nodeId,
                    ServerHandle = serverHandle,
                    Timestamp = HiResClock.UtcNow,
                    Value = value,
                    Parameters = null,
                    MonitoringMode = MonitoringMode.Disabled
                };
                _events.Enqueue(e);
            }
        }

        /// <summary>
        /// Reports a value discarded because of queue overflow.
        /// </summary>
        public static void ReportDiscardedValue(NodeId nodeId, uint serverHandle, DataValue value) {
            if (!_eventsEnabled) {
                return;
            }

            lock (_events) {
                var e = new Event {
                    EventType = EventType.DiscardValue,
                    NodeId = nodeId,
                    ServerHandle = serverHandle,
                    Timestamp = HiResClock.UtcNow,
                    Value = value,
                    Parameters = null,
                    MonitoringMode = MonitoringMode.Disabled
                };
                _events.Enqueue(e);
            }
        }

        /// <summary>
        /// Reports a value published.
        /// </summary>
        public static void ReportPublishValue(NodeId nodeId, uint serverHandle, DataValue value) {
            if (!_eventsEnabled) {
                return;
            }

            lock (_events) {
                var e = new Event {
                    EventType = EventType.PublishValue,
                    NodeId = nodeId,
                    ServerHandle = serverHandle,
                    Timestamp = HiResClock.UtcNow,
                    Value = value,
                    Parameters = null,
                    MonitoringMode = MonitoringMode.Disabled
                };
                _events.Enqueue(e);
            }
        }

        /// <summary>
        /// Reports a new monitored item.
        /// </summary>
        public static void ReportCreateMonitoredItem(
            NodeId nodeId,
            uint serverHandle,
            double samplingInterval,
            uint queueSize,
            bool discardOldest,
            MonitoringFilter filter,
            MonitoringMode monitoringMode) {
            if (!_eventsEnabled) {
                return;
            }

            lock (_events) {
                var e = new Event {
                    EventType = EventType.CreateItem,
                    NodeId = nodeId,
                    ServerHandle = serverHandle,
                    Timestamp = HiResClock.UtcNow,
                    Value = null,
                    Parameters = new MonitoringParameters {
                        SamplingInterval = samplingInterval,
                        QueueSize = queueSize,
                        DiscardOldest = discardOldest,
                        Filter = new ExtensionObject(filter)
                    },
                    MonitoringMode = monitoringMode
                };
                _events.Enqueue(e);
            }
        }

        /// <summary>
        /// Reports a modified monitored item.
        /// </summary>
        public static void ReportModifyMonitoredItem(
            NodeId nodeId,
            uint serverHandle,
            double samplingInterval,
            uint queueSize,
            bool discardOldest,
            MonitoringFilter filter,
            MonitoringMode monitoringMode) {
            if (!_eventsEnabled) {
                return;
            }

            lock (_events) {
                var e = new Event {
                    EventType = EventType.ModifyItem,
                    NodeId = nodeId,
                    ServerHandle = serverHandle,
                    Timestamp = HiResClock.UtcNow,
                    Value = null,
                    Parameters = new MonitoringParameters {
                        SamplingInterval = samplingInterval,
                        QueueSize = queueSize,
                        DiscardOldest = discardOldest,
                        Filter = new ExtensionObject(filter)
                    },
                    MonitoringMode = monitoringMode
                };
                _events.Enqueue(e);
            }
        }


        /// <summary>
        /// Fills in the diagnostic information after an error.
        /// </summary>
        public static uint CreateError(
            uint code,
            OperationContext context,
            DiagnosticInfoCollection diagnosticInfos,
            int index) {
            var error = new ServiceResult(code);

            if ((context.DiagnosticsMask & DiagnosticsMasks.OperationAll) != 0) {
                diagnosticInfos[index] = new DiagnosticInfo(error, context.DiagnosticsMask, false, context.StringTable);
            }

            return error.Code;
        }

        /// <summary>
        /// Fills in the diagnostic information after an error.
        /// </summary>
        public static bool CreateError(
            uint code,
            StatusCodeCollection results,
            DiagnosticInfoCollection diagnosticInfos,
            OperationContext context) {
            var error = new ServiceResult(code);
            results.Add(error.Code);

            if ((context.DiagnosticsMask & DiagnosticsMasks.OperationAll) != 0) {
                diagnosticInfos.Add(new DiagnosticInfo(error, context.DiagnosticsMask, false, context.StringTable));
                return true;
            }

            return false;
        }

        /// <summary>
        /// Fills in the diagnostic information after an error.
        /// </summary>
        public static bool CreateError(
            uint code,
            StatusCodeCollection results,
            DiagnosticInfoCollection diagnosticInfos,
            int index,
            OperationContext context) {
            var error = new ServiceResult(code);
            results[index] = error.Code;

            if ((context.DiagnosticsMask & DiagnosticsMasks.OperationAll) != 0) {
                diagnosticInfos[index] = new DiagnosticInfo(error, context.DiagnosticsMask, false, context.StringTable);
                return true;
            }

            return false;
        }

        /// <summary>
        /// Creates a place holder in the lists for the results.
        /// </summary>
        public static void CreateSuccess(
            StatusCodeCollection results,
            DiagnosticInfoCollection diagnosticInfos,
            OperationContext context) {
            results.Add(StatusCodes.Good);

            if ((context.DiagnosticsMask & DiagnosticsMasks.OperationAll) != 0) {
                diagnosticInfos.Add(null);
            }
        }

        /// <summary>
        /// Creates a collection of diagnostics from a set of errors.
        /// </summary>
        public static DiagnosticInfoCollection CreateDiagnosticInfoCollection(
            OperationContext context,
            IList<ServiceResult> errors) {
            // all done if no diagnostics requested.
            if ((context.DiagnosticsMask & DiagnosticsMasks.OperationAll) == 0) {
                return null;
            }

            // create diagnostics.
            var results = new DiagnosticInfoCollection(errors.Count);

            foreach (var error in errors) {
                if (ServiceResult.IsBad(error)) {
                    results.Add(new DiagnosticInfo(error, context.DiagnosticsMask, false, context.StringTable));
                }
                else {
                    results.Add(null);
                }
            }

            return results;
        }

        /// <summary>
        /// Creates a collection of status codes and diagnostics from a set of errors.
        /// </summary>
        public static StatusCodeCollection CreateStatusCodeCollection(
            OperationContext context,
            IList<ServiceResult> errors,
            out DiagnosticInfoCollection diagnosticInfos) {
            diagnosticInfos = null;

            var noErrors = true;
            var results = new StatusCodeCollection(errors.Count);

            foreach (var error in errors) {
                if (ServiceResult.IsBad(error)) {
                    results.Add(error.Code);
                    noErrors = false;
                }
                else {
                    results.Add(StatusCodes.Good);
                }
            }

            // only generate diagnostics if errors exist.
            if (noErrors) {
                diagnosticInfos = CreateDiagnosticInfoCollection(context, errors);
            }

            return results;
        }

        /// <summary>
        /// Creates the diagnostic info and translates any strings.
        /// </summary>
        /// <param name="server">The server.</param>
        /// <param name="context">The context containing the string stable.</param>
        /// <param name="error">The error to translate.</param>
        /// <returns>The diagnostics with references to the strings in the context string table.</returns>
        public static DiagnosticInfo CreateDiagnosticInfo(
            IServerInternal server,
            OperationContext context,
            ServiceResult error) {
            if (error == null) {
                return null;
            }

            var translatedError = error;

            if ((context.DiagnosticsMask & DiagnosticsMasks.LocalizedText) != 0) {
                translatedError = server.ResourceManager.Translate(context.PreferredLocales, error);
            }

            var diagnosticInfo = new DiagnosticInfo(
                translatedError,
                context.DiagnosticsMask,
                false,
                context.StringTable);

            return diagnosticInfo;
        }
    }
}
