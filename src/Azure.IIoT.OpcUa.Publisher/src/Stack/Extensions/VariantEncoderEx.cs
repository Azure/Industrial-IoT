// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Azure.IIoT.OpcUa.Publisher.Stack {
    using Azure.IIoT.OpcUa.Encoders;
    using Azure.IIoT.OpcUa.Encoders.Models;
    using Azure.IIoT.OpcUa.Publisher.Stack.Models;
    using Azure.IIoT.OpcUa.Shared.Models;
    using Microsoft.Azure.IIoT.Serializers;
    using Opc.Ua;
    using Opc.Ua.Extensions;
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Linq;

    /// <summary>
    /// Variant encoder extensions
    /// </summary>
    public static class VariantEncoderEx {

        /// <summary>
        /// Format variant as string
        /// </summary>
        /// <param name="encoder"></param>
        /// <param name="value"></param>
        /// <returns></returns>
        public static VariantValue Encode(this IVariantEncoder encoder, Variant value) {
            return encoder.Encode(value, out var tmp);
        }

        /// <summary>
        /// Decode with data type as string
        /// </summary>
        /// <param name="encoder"></param>
        /// <param name="value"></param>
        /// <param name="type"></param>
        /// <returns></returns>
        public static Variant Decode(this IVariantEncoder encoder, VariantValue value,
            string type) {
            return encoder.Decode(value, string.IsNullOrEmpty(type) ? BuiltInType.Null :
                TypeInfo.GetBuiltInType(type.ToNodeId(encoder.Context)));
        }

        /// <summary>
        /// Convert from diagnostics info to service result
        /// </summary>
        /// <param name="diagnostics"></param>
        /// <param name="code"></param>
        /// <param name="operation"></param>
        /// <param name="codec"></param>
        /// <param name="config"></param>
        /// <returns></returns>
        public static ServiceResultModel Encode(this IVariantEncoder codec,
            DiagnosticInfo diagnostics,
            StatusCode code, string operation, DiagnosticsModel config) {
            if (code == StatusCodes.Good) {
                return null;
            }
            return codec.Encode(new List<OperationResultModel> {
                new OperationResultModel {
                    DiagnosticsInfo = diagnostics,
                    Operation = operation,
                    StatusCode = code
                }
            }, config);
        }

        /// <summary>
        /// Convert to service model
        /// </summary>
        /// <param name="diagnostics"></param>
        /// <param name="codec"></param>
        /// <param name="config"></param>
        /// <returns></returns>
        public static ServiceResultModel Encode(this IVariantEncoder codec,
            List<OperationResultModel> diagnostics, DiagnosticsModel config) {
            if ((diagnostics?.Count ?? 0) == 0) {
                return null; // All well
            }
            var result = diagnostics.LastOrDefault(d => !d.TraceOnly);
            var statusCode = result?.StatusCode;
            return new ServiceResultModel {
                // The last operation result is the one that caused the service to fail.
                StatusCode = statusCode?.Code,
                ErrorMessage = result?.DiagnosticsInfo?.AdditionalInfo ?? (statusCode == null ?
                    null : StatusCode.LookupSymbolicId(statusCode.Value.CodeBits)),
                Diagnostics = codec.Write(diagnostics, config)
            };
        }

        /// <summary>
        /// Convert to service model
        /// </summary>
        /// <param name="statusCode"></param>
        /// <param name="diagnosticsInfo"></param>
        /// <param name="codec"></param>
        /// <param name="config"></param>
        /// <returns></returns>
        public static ServiceResultModel Encode(this IVariantEncoder codec,
            StatusCode? statusCode, DiagnosticInfo diagnosticsInfo = null,
            DiagnosticsModel config = null) {
            if ((statusCode?.Code ?? StatusCodes.Good) == StatusCodes.Good) {
                return null; // All well
            }
            return new ServiceResultModel {
                // The last operation result is the one that caused the service to fail.
                StatusCode = statusCode?.Code,
                ErrorMessage = diagnosticsInfo?.AdditionalInfo ?? (statusCode == null ?
                    null : StatusCode.LookupSymbolicId(statusCode.Value.CodeBits)),
                Diagnostics = config == null ? null : codec.Write(
                    new List<OperationResultModel> {
                        new OperationResultModel {
                            DiagnosticsInfo = diagnosticsInfo,
                            StatusCode = statusCode.Value
                        }
                    }, config)
            };
        }

        /// <summary>
        /// Convert operation results to json
        /// </summary>
        /// <param name="results"></param>
        /// <param name="codec"></param>
        /// <param name="config"></param>
        /// <returns></returns>
        private static VariantValue Write(this IVariantEncoder codec,
            List<OperationResultModel> results, DiagnosticsModel config) {
            var level = config?.Level ?? Shared.Models.DiagnosticsLevel.Status;
            if (level == Shared.Models.DiagnosticsLevel.None) {
                return null;
            }
            using (var stream = new MemoryStream()) {
                var root = kDiagnosticsProperty;
                using (var encoder = new JsonEncoderEx(stream, codec.Context) {
                    UseAdvancedEncoding = true,
                    IgnoreDefaultValues = true
                }) {
                    switch (level) {
                        case Shared.Models.DiagnosticsLevel.Diagnostics:
                        case Shared.Models.DiagnosticsLevel.Verbose:
                            encoder.WriteEncodeableArray(root, results.ToArray(), typeof(OperationResultModel));
                            break;
                        case Shared.Models.DiagnosticsLevel.Operations:
                            var codes = results
                                .GroupBy(d => d.StatusCode.CodeBits);
                            root = null;
                            foreach (var code in codes) {
                                encoder.WriteStringArray(StatusCode.LookupSymbolicId(code.Key),
                                    code.Select(c => c.Operation).ToArray());
                            }
                            break;
                        case Shared.Models.DiagnosticsLevel.Status:
                            var statusCodes = results
                                .Select(d => StatusCode.LookupSymbolicId(d.StatusCode.CodeBits))
                                .Where(s => !string.IsNullOrEmpty(s))
                                .Distinct();
                            if (!statusCodes.Any()) {
                                return null;
                            }
                            encoder.WriteStringArray(root, statusCodes.ToArray());
                            break;
                    }
                }
                var o = codec.Serializer.Parse(stream.ToArray());
                return root != null ? o[root] : o;
            }
        }

        /// <summary>
        /// Convert delete at time details
        /// </summary>
        /// <param name="codec"></param>
        /// <param name="details"></param>
        /// <returns></returns>
        public static VariantValue Encode(this IVariantEncoder codec,
            DeleteValuesAtTimesDetailsModel details) {
            if (details == null) {
                throw new ArgumentNullException(nameof(details));
            }
            if (details.ReqTimes == null || details.ReqTimes.Length == 0) {
                throw new ArgumentException(nameof(details.ReqTimes));
            }
            return codec.Encode(new ExtensionObject(new DeleteAtTimeDetails {
                NodeId = NodeId.Null,
                ReqTimes = new DateTimeCollection(details.ReqTimes)
            }));
        }

        /// <summary>
        /// Convert delete event details
        /// </summary>
        /// <param name="codec"></param>
        /// <param name="details"></param>
        /// <returns></returns>
        public static VariantValue Encode(this IVariantEncoder codec, DeleteEventsDetailsModel details) {
            if (details == null) {
                throw new ArgumentNullException(nameof(details));
            }
            if (details.EventIds == null || details.EventIds.Count == 0) {
                throw new ArgumentException(nameof(details.EventIds));
            }
            return codec.Encode(new ExtensionObject(new DeleteEventDetails {
                NodeId = NodeId.Null,
                EventIds = new ByteStringCollection(details.EventIds)
            }));
        }

        /// <summary>
        /// Convert delete raw modified details
        /// </summary>
        /// <param name="codec"></param>
        /// <param name="details"></param>
        /// <returns></returns>
        public static VariantValue Encode(this IVariantEncoder codec, DeleteValuesDetailsModel details) {
            if (details == null) {
                throw new ArgumentNullException(nameof(details));
            }
            if (details.EndTime == null && details.StartTime == null) {
                throw new ArgumentException("Start time and end time cannot both be null", nameof(details));
            }
            return codec.Encode(new ExtensionObject(new DeleteRawModifiedDetails {
                NodeId = NodeId.Null,
                EndTime = details.EndTime ?? DateTime.MinValue,
                StartTime = details.StartTime ?? DateTime.MinValue,
                IsDeleteModified = false
            }));
        }

        /// <summary>
        /// Convert delete raw modified details
        /// </summary>
        /// <param name="codec"></param>
        /// <param name="details"></param>
        /// <returns></returns>
        public static VariantValue Encode(this IVariantEncoder codec, DeleteModifiedValuesDetailsModel details) {
            if (details == null) {
                throw new ArgumentNullException(nameof(details));
            }
            return codec.Encode(new ExtensionObject(new DeleteRawModifiedDetails {
                NodeId = NodeId.Null,
                EndTime = details.EndTime ?? DateTime.MinValue,
                StartTime = details.StartTime ?? DateTime.MinValue,
                IsDeleteModified = true
            }));
        }

        /// <summary>
        /// Convert update data details
        /// </summary>
        /// <param name="codec"></param>
        /// <param name="details"></param>
        /// <returns></returns>
        public static VariantValue Encode(this IVariantEncoder codec, ReplaceValuesDetailsModel details) {
            if (details == null) {
                throw new ArgumentNullException(nameof(details));
            }
            if (details.Values == null || details.Values.Count == 0) {
                throw new ArgumentException(nameof(details.Values));
            }
            return codec.Encode(new ExtensionObject(new UpdateDataDetails {
                NodeId = NodeId.Null,
                PerformInsertReplace = PerformUpdateType.Replace,
                UpdateValues = new DataValueCollection(details.Values
                    .Select(d => new DataValue {
                        ServerPicoseconds = d.ServerPicoseconds ?? 0,
                        SourcePicoseconds = d.SourcePicoseconds ?? 0,
                        ServerTimestamp = d.ServerTimestamp ?? DateTime.MinValue,
                        SourceTimestamp = d.SourceTimestamp ?? DateTime.MinValue,
                        StatusCode = d.StatusCode ?? StatusCodes.Good,
                        Value = new EncodeableVariantValue(codec.Serializer, d.Value) // TODO: Validate
                    }))
            }));
        }

        /// <summary>
        /// Convert update data details
        /// </summary>
        /// <param name="codec"></param>
        /// <param name="details"></param>
        /// <returns></returns>
        public static VariantValue Encode(this IVariantEncoder codec, InsertValuesDetailsModel details) {
            if (details == null) {
                throw new ArgumentNullException(nameof(details));
            }
            if (details.Values == null || details.Values.Count == 0) {
                throw new ArgumentException(nameof(details.Values));
            }
            return codec.Encode(new ExtensionObject(new UpdateDataDetails {
                NodeId = NodeId.Null,
                PerformInsertReplace = PerformUpdateType.Insert,
                UpdateValues = new DataValueCollection(details.Values
                    .Select(d => new DataValue {
                        ServerPicoseconds = d.ServerPicoseconds ?? 0,
                        SourcePicoseconds = d.SourcePicoseconds ?? 0,
                        ServerTimestamp = d.ServerTimestamp ?? DateTime.MinValue,
                        SourceTimestamp = d.SourceTimestamp ?? DateTime.MinValue,
                        StatusCode = d.StatusCode ?? StatusCodes.Good,
                        Value = new EncodeableVariantValue(codec.Serializer, d.Value) // TODO: Validate
                    }))
            }));
        }

        /// <summary>
        /// Convert update event details
        /// </summary>
        /// <param name="codec"></param>
        /// <param name="details"></param>
        /// <returns></returns>
        public static VariantValue Encode(this IVariantEncoder codec, ReplaceEventsDetailsModel details) {
            if (details == null) {
                throw new ArgumentNullException(nameof(details));
            }
            if (details.Events == null || details.Events.Count == 0) {
                throw new ArgumentException(nameof(details.Events));
            }
            return codec.Encode(new ExtensionObject(new UpdateEventDetails {
                NodeId = NodeId.Null,
                PerformInsertReplace = PerformUpdateType.Replace,
                Filter = codec.Decode(details.Filter),
                EventData = new HistoryEventFieldListCollection(details.Events
                    .Select(d => new HistoryEventFieldList {
                        EventFields = new VariantCollection(d.EventFields
                            .Select(f => new Variant(new EncodeableVariantValue(codec.Serializer, f))))
                    }))
            }));
        }

        /// <summary>
        /// Convert update event details
        /// </summary>
        /// <param name="codec"></param>
        /// <param name="details"></param>
        /// <returns></returns>
        public static VariantValue Encode(this IVariantEncoder codec, InsertEventsDetailsModel details) {
            if (details == null) {
                throw new ArgumentNullException(nameof(details));
            }
            if (details.Events == null || details.Events.Count == 0) {
                throw new ArgumentException(nameof(details.Events));
            }
            return codec.Encode(new ExtensionObject(new UpdateEventDetails {
                NodeId = NodeId.Null,
                PerformInsertReplace = PerformUpdateType.Insert,
                Filter = codec.Decode(details.Filter),
                EventData = new HistoryEventFieldListCollection(details.Events
                    .Select(d => new HistoryEventFieldList {
                        EventFields = new VariantCollection(d.EventFields
                            .Select(f => new Variant(new EncodeableVariantValue(codec.Serializer, f))))
                    }))
            }));
        }

        /// <summary>
        /// Convert read at time details
        /// </summary>
        /// <param name="codec"></param>
        /// <param name="details"></param>
        /// <returns></returns>
        public static VariantValue Encode(this IVariantEncoder codec, ReadValuesAtTimesDetailsModel details) {
            if (details == null) {
                throw new ArgumentNullException(nameof(details));
            }
            if (details.ReqTimes == null || details.ReqTimes.Length == 0) {
                throw new ArgumentException(nameof(details.ReqTimes));
            }
            return codec.Encode(new ExtensionObject(new ReadAtTimeDetails {
                ReqTimes = new DateTimeCollection(details.ReqTimes),
                UseSimpleBounds = details.UseSimpleBounds ?? false
            }));
        }

        /// <summary>
        /// Convert read event details
        /// </summary>
        /// <param name="codec"></param>
        /// <param name="details"></param>
        /// <returns></returns>
        public static VariantValue Encode(this IVariantEncoder codec, ReadEventsDetailsModel details) {
            if (details == null) {
                throw new ArgumentNullException(nameof(details));
            }
            if (details.EndTime == null && details.StartTime == null) {
                throw new ArgumentException("Start time and end time cannot both be null", nameof(details));
            }
            if ((details.StartTime == null || details.EndTime == null) && ((details.NumEvents ?? 0) == 0)) {
                throw new ArgumentException("Value bound must be set", nameof(details.NumEvents));
            }
            return codec.Encode(new ExtensionObject(new ReadEventDetails {
                EndTime = details.EndTime ?? DateTime.MinValue,
                StartTime = details.StartTime ?? DateTime.MinValue,
                Filter = codec.Decode(details.Filter),
                NumValuesPerNode = details.NumEvents ?? 0
            }));
        }

        /// <summary>
        /// Convert read processed details
        /// </summary>
        /// <param name="codec"></param>
        /// <param name="details"></param>
        /// <returns></returns>
        public static VariantValue Encode(this IVariantEncoder codec, ReadProcessedValuesDetailsModel details) {
            if (details == null) {
                throw new ArgumentNullException(nameof(details));
            }
            if (details.EndTime == null && details.StartTime == null) {
                throw new ArgumentException("Start time and end time cannot both be null", nameof(details));
            }
            var aggregate = details.AggregateTypeId?.ToNodeId(codec.Context);
            return codec.Encode(new ExtensionObject(new ReadProcessedDetails {
                EndTime = details.EndTime ?? DateTime.MinValue,
                StartTime = details.StartTime ?? DateTime.MinValue,
                AggregateType = aggregate == null ? null : new NodeIdCollection(aggregate.YieldReturn()),
                ProcessingInterval = details.ProcessingInterval ?? 0,
                AggregateConfiguration = details.AggregateConfiguration.ToStackModel()
            })); // Reapplies the aggregate namespace uri during encoding using the context's table
        }

        /// <summary>
        /// Convert read raw modified details
        /// </summary>
        /// <param name="codec"></param>
        /// <param name="details"></param>
        /// <returns></returns>
        public static VariantValue Encode(this IVariantEncoder codec, ReadValuesDetailsModel details) {
            if (details == null) {
                throw new ArgumentNullException(nameof(details));
            }
            if (details.EndTime == null && details.StartTime == null) {
                throw new ArgumentException("Start time and end time cannot both be null", nameof(details));
            }
            if ((details.StartTime == null || details.EndTime == null) && ((details.NumValues ?? 0) == 0)) {
                throw new ArgumentException("Value bound must be set", nameof(details.NumValues));
            }
            return codec.Encode(new ExtensionObject(new ReadRawModifiedDetails {
                EndTime = details.EndTime ?? DateTime.MinValue,
                StartTime = details.StartTime ?? DateTime.MinValue,
                IsReadModified = false,
                ReturnBounds = details.ReturnBounds ?? false,
                NumValuesPerNode = details.NumValues ?? 0
            }));
        }

        /// <summary>
        /// Convert read modified details
        /// </summary>
        /// <param name="codec"></param>
        /// <param name="details"></param>
        /// <returns></returns>
        public static VariantValue Encode(this IVariantEncoder codec, ReadModifiedValuesDetailsModel details) {
            if (details == null) {
                throw new ArgumentNullException(nameof(details));
            }
            if (details.EndTime == null && details.StartTime == null) {
                throw new ArgumentException("Start time and end time cannot both be null", nameof(details));
            }
            if ((details.StartTime == null || details.EndTime == null) && ((details.NumValues ?? 0) == 0)) {
                throw new ArgumentException("Value bound must be set", nameof(details.NumValues));
            }
            return codec.Encode(new ExtensionObject(new ReadRawModifiedDetails {
                EndTime = details.EndTime ?? DateTime.MinValue,
                StartTime = details.StartTime ?? DateTime.MinValue,
                IsReadModified = true,
                NumValuesPerNode = details.NumValues ?? 0
            }));
        }

        /// <summary>
        /// Convert to results
        /// </summary>
        /// <param name="codec"></param>
        /// <param name="result"></param>
        /// <returns></returns>
        public static HistoricValueModel[] DecodeValues(this IVariantEncoder codec, VariantValue result) {
            var extensionObject = codec.DecodeExtensionObject(result);
            if (extensionObject?.Body is HistoryData data) {
                var results = data.DataValues.Select(d => new HistoricValueModel {
                    ServerPicoseconds = d.ServerPicoseconds.ToNullable((ushort)0),
                    SourcePicoseconds = d.SourcePicoseconds.ToNullable((ushort)0),
                    ServerTimestamp = d.ServerTimestamp.ToNullable(DateTime.MinValue),
                    SourceTimestamp = d.SourceTimestamp.ToNullable(DateTime.MinValue),
                    StatusCode = d.StatusCode.ToNullable(StatusCodes.Good)?.CodeBits,
                    Value = d.WrappedValue == Variant.Null ? null : codec.Encode(d.WrappedValue)
                }).ToArray();
                if (extensionObject?.Body is HistoryModifiedData modified) {
                    if (modified.ModificationInfos.Count != data.DataValues.Count) {
                        throw new FormatException("Modification infos and data value count is not the same");
                    }
                    for (var i = 0; i < modified.ModificationInfos.Count; i++) {
                        results[i].ModificationInfo = new ModificationInfoModel {
                            ModificationTime =
                                modified.ModificationInfos[i].ModificationTime.ToNullable(DateTime.MinValue),
                            UpdateType =
                                (HistoryUpdateOperation)modified.ModificationInfos[i].UpdateType,
                            UserName =
                                modified.ModificationInfos[i].UserName
                        };
                    }
                }
                return results;
            }
            return null;
        }

        /// <summary>
        /// Convert to results
        /// </summary>
        /// <param name="codec"></param>
        /// <param name="result"></param>
        /// <returns></returns>
        public static HistoricEventModel[] DecodeEvents(this IVariantEncoder codec, VariantValue result) {
            var extensionObject = codec.DecodeExtensionObject(result);
            if (extensionObject?.Body is HistoryEvent ev) {
                return ev.Events.Select(d => new HistoricEventModel {
                    EventFields = d.EventFields
                        .Select(v => v == Variant.Null ? null : codec.Encode(v))
                        .ToList()
                }).ToArray();
            }
            return null;
        }

        /// <summary>
        /// Encode extension object
        /// </summary>
        /// <param name="codec"></param>
        /// <param name="o"></param>
        /// <returns></returns>
        internal static VariantValue Encode(this IVariantEncoder codec, ExtensionObject o) {
            var variant = o == null ? Variant.Null : new Variant(o);
            return codec.Encode(variant);
        }

        /// <summary>
        /// Encode extension object
        /// </summary>
        /// <param name="codec"></param>
        /// <param name="result"></param>
        /// <returns></returns>
        internal static ExtensionObject DecodeExtensionObject(this IVariantEncoder codec,
            VariantValue result) {
            if (result == null) {
                return null;
            }
            var variant = codec.Decode(result, BuiltInType.ExtensionObject);
            return variant.Value as ExtensionObject;
        }

        private const string kDiagnosticsProperty = "diagnostics";
    }
}
