// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IIoT.OpcUa.Api.History.Models {
    using Microsoft.Azure.IIoT.OpcUa.Api.Core.Models;
    using Microsoft.Azure.IIoT.OpcUa.Core.Models;
    using Microsoft.Azure.IIoT.OpcUa.History.Models;
    using Microsoft.Azure.IIoT.Serializers;
    using System;
    using System.Linq;

    /// <summary>
    /// Aggregate configuration
    /// </summary>
    public static class ModelExtensions {

        /// <summary>
        /// Create api model
        /// </summary>
        public static AggregateConfigurationApiModel ToApiModel(
            this AggregateConfigurationModel model) {
            if (model == null) {
                return null;
            }
            return new AggregateConfigurationApiModel {
                UseServerCapabilitiesDefaults = model.UseServerCapabilitiesDefaults,
                TreatUncertainAsBad = model.TreatUncertainAsBad,
                PercentDataBad = model.PercentDataBad,
                PercentDataGood = model.PercentDataGood,
                UseSlopedExtrapolation = model.UseSlopedExtrapolation
            };
        }

        /// <summary>
        /// Create service model from api model
        /// </summary>
        public static AggregateConfigurationModel ToServiceModel(
            this AggregateConfigurationApiModel model) {
            if (model == null) {
                return null;
            }
            return new AggregateConfigurationModel {
                UseServerCapabilitiesDefaults = model.UseServerCapabilitiesDefaults,
                TreatUncertainAsBad = model.TreatUncertainAsBad,
                PercentDataBad = model.PercentDataBad,
                PercentDataGood = model.PercentDataGood,
                UseSlopedExtrapolation = model.UseSlopedExtrapolation
            };
        }

        /// <summary>
        /// Create api model
        /// </summary>
        public static ContentFilterApiModel ToApiModel(
            this ContentFilterModel model) {
            if (model == null) {
                return null;
            }
            return new ContentFilterApiModel {
                Elements = model.Elements?
                    .Select(e => e.ToApiModel())
                    .ToList()
            };
        }

        /// <summary>
        /// Create service model from api model
        /// </summary>
        public static ContentFilterModel ToServiceModel(
            this ContentFilterApiModel model) {
            if (model == null) {
                return null;
            }
            return new ContentFilterModel {
                Elements = model.Elements?
                    .Select(e => e.ToServiceModel())
                    .ToList()
            };
        }

        /// <summary>
        /// Create api model
        /// </summary>
        public static ContentFilterElementApiModel ToApiModel(
            this ContentFilterElementModel model) {
            if (model == null) {
                return null;
            }
            return new ContentFilterElementApiModel {
                FilterOperands = model.FilterOperands?
                    .Select(f => f.ToApiModel())
                    .ToList(),
                FilterOperator = (Core.Models.FilterOperatorType)model.FilterOperator
            };
        }

        /// <summary>
        /// Create service model from api model
        /// </summary>
        public static ContentFilterElementModel ToServiceModel(
            this ContentFilterElementApiModel model) {
            if (model == null) {
                return null;
            }
            return new ContentFilterElementModel {
                FilterOperands = model.FilterOperands?
                    .Select(f => f.ToServiceModel())
                    .ToList(),
                FilterOperator = (OpcUa.Core.Models.FilterOperatorType)model.FilterOperator
            };
        }

        /// <summary>
        /// Create service model from api model
        /// </summary>
        public static CredentialModel ToServiceModel(
            this CredentialApiModel model) {
            if (model == null) {
                return null;
            }
            return new CredentialModel {
                Value = model.Value?.Copy(),
                Type = (OpcUa.Core.Models.CredentialType?)model.Type
            };
        }

        /// <summary>
        /// Create api model from service model
        /// </summary>
        public static CredentialApiModel ToApiModel(
            this CredentialModel model) {
            if (model == null) {
                return null;
            }
            return new CredentialApiModel {
                Value = model.Value?.Copy(),
                Type = (Core.Models.CredentialType?)model.Type
            };
        }

        /// <summary>
        /// Create api model
        /// </summary>
        public static DeleteEventsDetailsApiModel ToApiModel(
            this DeleteEventsDetailsModel model) {
            if (model == null) {
                return null;
            }
            return new DeleteEventsDetailsApiModel {
                EventIds = model.EventIds
            };
        }

        /// <summary>
        /// Create service model from api model
        /// </summary>
        public static DeleteEventsDetailsModel ToServiceModel(
            this DeleteEventsDetailsApiModel model) {
            if (model == null) {
                return null;
            }
            return new DeleteEventsDetailsModel {
                EventIds = model.EventIds
            };
        }

        /// <summary>
        /// Create api model
        /// </summary>
        public static DeleteModifiedValuesDetailsApiModel ToApiModel(
            this DeleteModifiedValuesDetailsModel model) {
            if (model == null) {
                return null;
            }
            return new DeleteModifiedValuesDetailsApiModel {
                EndTime = model.EndTime,
                StartTime = model.StartTime
            };
        }

        /// <summary>
        /// Create service model from api model
        /// </summary>
        public static DeleteModifiedValuesDetailsModel ToServiceModel(
            this DeleteModifiedValuesDetailsApiModel model) {
            if (model == null) {
                return null;
            }
            return new DeleteModifiedValuesDetailsModel {
                EndTime = model.EndTime,
                StartTime = model.StartTime
            };
        }

        /// <summary>
        /// Create api model
        /// </summary>
        public static DeleteValuesAtTimesDetailsApiModel ToApiModel(
            this DeleteValuesAtTimesDetailsModel model) {
            if (model == null) {
                return null;
            }
            return new DeleteValuesAtTimesDetailsApiModel {
                ReqTimes = model.ReqTimes
            };
        }

        /// <summary>
        /// Create service model from api model
        /// </summary>
        public static DeleteValuesAtTimesDetailsModel ToServiceModel(
            this DeleteValuesAtTimesDetailsApiModel model) {
            if (model == null) {
                return null;
            }
            return new DeleteValuesAtTimesDetailsModel {
                ReqTimes = model.ReqTimes
            };
        }

        /// <summary>
        /// Create api model
        /// </summary>
        public static DeleteValuesDetailsApiModel ToApiModel(
            this DeleteValuesDetailsModel model) {
            if (model == null) {
                return null;
            }
            return new DeleteValuesDetailsApiModel {
                EndTime = model.EndTime,
                StartTime = model.StartTime
            };
        }

        /// <summary>
        /// Create service model from api model
        /// </summary>
        public static DeleteValuesDetailsModel ToServiceModel(
            this DeleteValuesDetailsApiModel model) {
            if (model == null) {
                return null;
            }
            return new DeleteValuesDetailsModel {
                EndTime = model.EndTime,
                StartTime = model.StartTime
            };
        }

        /// <summary>
        /// Create service model from api model
        /// </summary>
        public static DiagnosticsApiModel ToApiModel(
            this DiagnosticsModel model) {
            if (model == null) {
                return null;
            }
            return new DiagnosticsApiModel {
                AuditId = model.AuditId,
                Level = (Core.Models.DiagnosticsLevel)model.Level,
                TimeStamp = model.TimeStamp
            };
        }

        /// <summary>
        /// Create service model from api model
        /// </summary>
        public static DiagnosticsModel ToServiceModel(
            this DiagnosticsApiModel model) {
            if (model == null) {
                return null;
            }
            return new DiagnosticsModel {
                AuditId = model.AuditId,
                Level = (OpcUa.Core.Models.DiagnosticsLevel)model.Level,
                TimeStamp = model.TimeStamp
            };
        }

        /// <summary>
        /// Create api model
        /// </summary>
        public static EventFilterApiModel ToApiModel(
            this EventFilterModel model) {
            if (model == null) {
                return null;
            }
            return new EventFilterApiModel {
                SelectClauses = model.SelectClauses?
                    .Select(e => e.ToApiModel())
                    .ToList(),
                WhereClause = model.WhereClause.ToApiModel()
            };
        }

        /// <summary>
        /// Create service model from api model
        /// </summary>
        public static EventFilterModel ToServiceModel(
            this EventFilterApiModel model) {
            if (model == null) {
                return null;
            }
            return new EventFilterModel {
                SelectClauses = model.SelectClauses?
                    .Select(e => e.ToServiceModel())
                    .ToList(),
                WhereClause = model.WhereClause.ToServiceModel()
            };
        }

        /// <summary>
        /// Create service model from api model
        /// </summary>
        public static FilterOperandApiModel ToApiModel(
            this FilterOperandModel model) {
            if (model == null) {
                return null;
            }
            return new FilterOperandApiModel {
                Index = model.Index,
                Alias = model.Alias,
                Value = model.Value,
                NodeId = model.NodeId,
                AttributeId = (Core.Models.NodeAttribute?)model.AttributeId,
                BrowsePath = model.BrowsePath,
                IndexRange = model.IndexRange
            };
        }

        /// <summary>
        /// Create service model from api model
        /// </summary>
        public static FilterOperandModel ToServiceModel(
            this FilterOperandApiModel model) {
            if (model == null) {
                return null;
            }
            return new FilterOperandModel {
                Index = model.Index,
                Alias = model.Alias,
                Value = model.Value,
                NodeId = model.NodeId,
                AttributeId = (OpcUa.Core.Models.NodeAttribute?)model.AttributeId,
                BrowsePath = model.BrowsePath,
                IndexRange = model.IndexRange
            };
        }

        /// <summary>
        /// Create from service model
        /// </summary>
        /// <param name="model"></param>
        public static HistoricEventApiModel ToApiModel(
            this HistoricEventModel model) {
            if (model == null) {
                return null;
            }
            return new HistoricEventApiModel {
                EventFields = model.EventFields
            };
        }

        /// <summary>
        /// Create service model from api model
        /// </summary>
        public static HistoricEventModel ToServiceModel(
            this HistoricEventApiModel model) {
            if (model == null) {
                return null;
            }
            return new HistoricEventModel {
                EventFields = model.EventFields
            };
        }

        /// <summary>
        /// Create from service model
        /// </summary>
        /// <param name="model"></param>
        public static HistoricValueApiModel ToApiModel(
            this HistoricValueModel model) {
            if (model == null) {
                return null;
            }
            return new HistoricValueApiModel {
                Value = model.Value,
                StatusCode = model.StatusCode,
                SourceTimestamp = model.SourceTimestamp,
                SourcePicoseconds = model.SourcePicoseconds,
                ServerTimestamp = model.ServerTimestamp,
                ServerPicoseconds = model.ServerPicoseconds,
                ModificationInfo = model.ModificationInfo.ToApiModel()
            };
        }

        /// <summary>
        /// Create service model from api model
        /// </summary>
        public static HistoricValueModel ToServiceModel(
            this HistoricValueApiModel model) {
            if (model == null) {
                return null;
            }
            return new HistoricValueModel {
                Value = model.Value,
                StatusCode = model.StatusCode,
                SourceTimestamp = model.SourceTimestamp,
                SourcePicoseconds = model.SourcePicoseconds,
                ServerTimestamp = model.ServerTimestamp,
                ServerPicoseconds = model.ServerPicoseconds,
                ModificationInfo = model.ModificationInfo.ToServiceModel()
            };
        }

        /// <summary>
        /// Create from service model
        /// </summary>
        /// <param name="model"></param>
        public static HistoryReadNextRequestApiModel ToApiModel(
            this HistoryReadNextRequestModel model) {
            if (model == null) {
                return null;
            }
            return new HistoryReadNextRequestApiModel {
                ContinuationToken = model.ContinuationToken,
                Abort = model.Abort,
                Header = model.Header.ToApiModel()
            };
        }

        /// <summary>
        /// Convert back to service model
        /// </summary>
        /// <returns></returns>
        public static HistoryReadNextRequestModel ToServiceModel(
            this HistoryReadNextRequestApiModel model) {
            if (model == null) {
                return null;
            }
            return new HistoryReadNextRequestModel {
                ContinuationToken = model.ContinuationToken,
                Abort = model.Abort,
                Header = model.Header.ToServiceModel()
            };
        }

        /// <summary>
        /// Create from service model
        /// </summary>
        /// <typeparam name="S"></typeparam>
        /// <typeparam name="T"></typeparam>
        /// <param name="model"></param>
        /// <param name="convert"></param>
        public static HistoryReadNextResponseApiModel<T> ToApiModel<S, T>(
            this HistoryReadNextResultModel<S> model, Func<S, T> convert) {
            if (model == null) {
                return null;
            }
            return new HistoryReadNextResponseApiModel<T> {
                History = convert(model.History),
                ContinuationToken = model.ContinuationToken,
                ErrorInfo = model.ErrorInfo.ToApiModel()
            };
        }

        /// <summary>
        /// Create from api model
        /// </summary>
        /// <typeparam name="S"></typeparam>
        /// <typeparam name="T"></typeparam>
        /// <param name="model"></param>
        /// <param name="convert"></param>
        public static HistoryReadNextResultModel<T> ToServiceModel<S, T>(
            this HistoryReadNextResponseApiModel<S> model, Func<S, T> convert) {
            if (model == null) {
                return null;
            }
            return new HistoryReadNextResultModel<T> {
                History = convert(model.History),
                ContinuationToken = model.ContinuationToken,
                ErrorInfo = model.ErrorInfo.ToServiceModel()
            };
        }

        /// <summary>
        /// Create from service model
        /// </summary>
        /// <param name="model"></param>
        public static HistoryReadRequestApiModel<VariantValue> ToApiModel(
            this HistoryReadRequestModel<VariantValue> model) {
            if (model == null) {
                return null;
            }
            return new HistoryReadRequestApiModel<VariantValue> {
                NodeId = model.NodeId,
                BrowsePath = model.BrowsePath,
                IndexRange = model.IndexRange,
                Details = model.Details,
                Header = model.Header.ToApiModel()
            };
        }

        /// <summary>
        /// Convert to service model
        /// </summary>
        /// <param name="model"></param>
        public static HistoryReadRequestModel<VariantValue> ToServiceModel(
            this HistoryReadRequestApiModel<VariantValue> model) {
            if (model == null) {
                return null;
            }
            return new HistoryReadRequestModel<VariantValue> {
                NodeId = model.NodeId,
                BrowsePath = model.BrowsePath,
                IndexRange = model.IndexRange,
                Details = model.Details,
                Header = model.Header.ToServiceModel()
            };
        }

        /// <summary>
        /// Create from service model
        /// </summary>
        /// <param name="model"></param>
        public static HistoryReadNextResponseApiModel<VariantValue> ToApiModel(
            this HistoryReadNextResultModel<VariantValue> model) {
            if (model == null) {
                return null;
            }
            return new HistoryReadNextResponseApiModel<VariantValue> {
                History = model.History,
                ContinuationToken = model.ContinuationToken,
                ErrorInfo = model.ErrorInfo.ToApiModel()
            };
        }

        /// <summary>
        /// Convert to service model
        /// </summary>
        /// <param name="model"></param>
        public static HistoryReadNextResultModel<VariantValue> ToServiceModel(
            this HistoryReadNextResponseApiModel<VariantValue> model) {
            if (model == null) {
                return null;
            }
            return new HistoryReadNextResultModel<VariantValue> {
                History = model.History,
                ContinuationToken = model.ContinuationToken,
                ErrorInfo = model.ErrorInfo.ToServiceModel()
            };
        }

        /// <summary>
        /// Create from service model
        /// </summary>
        /// <param name="model"></param>
        public static HistoryReadResponseApiModel<VariantValue> ToApiModel(
            this HistoryReadResultModel<VariantValue> model) {
            if (model == null) {
                return null;
            }
            return new HistoryReadResponseApiModel<VariantValue> {
                History = model.History,
                ContinuationToken = model.ContinuationToken,
                ErrorInfo = model.ErrorInfo.ToApiModel()
            };
        }

        /// <summary>
        /// Convert to service model
        /// </summary>
        /// <param name="model"></param>
        public static HistoryReadResultModel<VariantValue> ToServiceModel(
            this HistoryReadResponseApiModel<VariantValue> model) {
            if (model == null) {
                return null;
            }
            return new HistoryReadResultModel<VariantValue> {
                History = model.History,
                ContinuationToken = model.ContinuationToken,
                ErrorInfo = model.ErrorInfo.ToServiceModel()
            };
        }

        /// <summary>
        /// Create from service model
        /// </summary>
        /// <param name="model"></param>
        public static HistoryUpdateRequestApiModel<VariantValue> ToApiModel(
            this HistoryUpdateRequestModel<VariantValue> model) {
            if (model == null) {
                return null;
            }
            return new HistoryUpdateRequestApiModel<VariantValue> {
                Details = model.Details,
                NodeId = model.NodeId,
                BrowsePath = model.BrowsePath,
                Header = model.Header.ToApiModel()
            };
        }

        /// <summary>
        /// Convert back to service model
        /// </summary>
        /// <returns></returns>
        public static HistoryUpdateRequestModel<VariantValue> ToServiceModel(
            this HistoryUpdateRequestApiModel<VariantValue> model) {
            if (model == null) {
                return null;
            }
            return new HistoryUpdateRequestModel<VariantValue> {
                Details = model.Details,
                NodeId = model.NodeId,
                BrowsePath = model.BrowsePath,
                Header = model.Header.ToServiceModel()
            };
        }

        /// <summary>
        /// Convert to api model
        /// </summary>
        /// <typeparam name="S"></typeparam>
        /// <typeparam name="T"></typeparam>
        /// <param name="model"></param>
        /// <param name="convert"></param>
        /// <returns></returns>
        public static HistoryReadRequestApiModel<S> ToApiModel<S, T>(
            this HistoryReadRequestModel<T> model, Func<T, S> convert) {
            if (model == null) {
                return null;
            }
            return new HistoryReadRequestApiModel<S> {
                Details = convert(model.Details),
                BrowsePath = model.BrowsePath,
                NodeId = model.NodeId,
                IndexRange = model.IndexRange,
                Header = model.Header.ToApiModel()
            };
        }

        /// <summary>
        /// Convert to service model
        /// </summary>
        /// <typeparam name="S"></typeparam>
        /// <typeparam name="T"></typeparam>
        /// <param name="model"></param>
        /// <param name="convert"></param>
        /// <returns></returns>
        public static HistoryReadRequestModel<S> ToServiceModel<S, T>(
            this HistoryReadRequestApiModel<T> model, Func<T, S> convert) {
            if (model == null) {
                return null;
            }
            return new HistoryReadRequestModel<S> {
                Details = convert(model.Details),
                BrowsePath = model.BrowsePath,
                NodeId = model.NodeId,
                IndexRange = model.IndexRange,
                Header = model.Header.ToServiceModel()
            };
        }

        /// <summary>
        /// Create from service model
        /// </summary>
        /// <typeparam name="S"></typeparam>
        /// <typeparam name="T"></typeparam>
        /// <param name="model"></param>
        /// <param name="convert"></param>
        public static HistoryReadResponseApiModel<T> ToApiModel<S, T>(
            this HistoryReadResultModel<S> model, Func<S, T> convert) {
            if (model == null) {
                return null;
            }
            return new HistoryReadResponseApiModel<T> {
                History = convert(model.History),
                ContinuationToken = model.ContinuationToken,
                ErrorInfo = model.ErrorInfo.ToApiModel()
            };
        }

        /// <summary>
        /// Create to service model
        /// </summary>
        /// <typeparam name="S"></typeparam>
        /// <typeparam name="T"></typeparam>
        /// <param name="model"></param>
        /// <param name="convert"></param>
        public static HistoryReadResultModel<T> ToServiceModel<S, T>(
            this HistoryReadResponseApiModel<S> model, Func<S, T> convert) {
            if (model == null) {
                return null;
            }
            return new HistoryReadResultModel<T> {
                History = convert(model.History),
                ContinuationToken = model.ContinuationToken,
                ErrorInfo = model.ErrorInfo.ToServiceModel()
            };
        }

        /// <summary>
        /// Convert to api model
        /// </summary>
        /// <typeparam name="S"></typeparam>
        /// <typeparam name="T"></typeparam>
        /// <param name="model"></param>
        /// <param name="convert"></param>
        /// <returns></returns>
        public static HistoryUpdateRequestApiModel<S> ToApiModel<S, T>(
            this HistoryUpdateRequestModel<T> model, Func<T, S> convert) {
            if (model == null) {
                return null;
            }
            return new HistoryUpdateRequestApiModel<S> {
                Details = convert(model.Details),
                NodeId = model.NodeId,
                BrowsePath = model.BrowsePath,
                Header = model.Header.ToApiModel()
            };
        }

        /// <summary>
        /// Convert to service model
        /// </summary>
        /// <typeparam name="S"></typeparam>
        /// <typeparam name="T"></typeparam>
        /// <param name="model"></param>
        /// <param name="convert"></param>
        /// <returns></returns>
        public static HistoryUpdateRequestModel<S> ToServiceModel<S, T>(
            this HistoryUpdateRequestApiModel<T> model, Func<T, S> convert) {
            if (model == null) {
                return null;
            }
            return new HistoryUpdateRequestModel<S> {
                Details = convert(model.Details),
                NodeId = model.NodeId,
                BrowsePath = model.BrowsePath,
                Header = model.Header.ToServiceModel()
            };
        }

        /// <summary>
        /// Create from service model
        /// </summary>
        /// <param name="model"></param>
        public static HistoryUpdateResponseApiModel ToApiModel(
            this HistoryUpdateResultModel model) {
            if (model == null) {
                return null;
            }
            return new HistoryUpdateResponseApiModel {
                Results = model.Results?
                    .Select(r => r.ToApiModel())
                    .ToList(),
                ErrorInfo = model.ErrorInfo.ToApiModel()
            };
        }

        /// <summary>
        /// Convert to service model
        /// </summary>
        /// <param name="model"></param>
        public static HistoryUpdateResultModel ToServiceModel(
            this HistoryUpdateResponseApiModel model) {
            if (model == null) {
                return null;
            }
            return new HistoryUpdateResultModel {
                Results = model.Results?
                    .Select(r => r.ToServiceModel())
                    .ToList(),
                ErrorInfo = model.ErrorInfo.ToServiceModel()
            };
        }

        /// <summary>
        /// Create api model
        /// </summary>
        public static InsertEventsDetailsApiModel ToApiModel(
            this InsertEventsDetailsModel model) {
            if (model == null) {
                return null;
            }
            return new InsertEventsDetailsApiModel {
                Filter = model.Filter.ToApiModel(),
                Events = model.Events?
                    .Select(v => v.ToApiModel())
                    .ToList()
            };
        }

        /// <summary>
        /// Create service model from api model
        /// </summary>
        public static InsertEventsDetailsModel ToServiceModel(
            this InsertEventsDetailsApiModel model) {
            if (model == null) {
                return null;
            }
            return new InsertEventsDetailsModel {
                Filter = model.Filter.ToServiceModel(),
                Events = model.Events?
                    .Select(v => v.ToServiceModel())
                    .ToList()
            };
        }

        /// <summary>
        /// Create api model
        /// </summary>
        public static InsertValuesDetailsApiModel ToApiModel(
            this InsertValuesDetailsModel model) {
            if (model == null) {
                return null;
            }
            return new InsertValuesDetailsApiModel {
                Values = model.Values?
                    .Select(v => v.ToApiModel())
                    .ToList()
            };
        }

        /// <summary>
        /// Create service model from api model
        /// </summary>
        public static InsertValuesDetailsModel ToServiceModel(
            this InsertValuesDetailsApiModel model) {
            if (model == null) {
                return null;
            }
            return new InsertValuesDetailsModel {
                Values = model.Values?
                    .Select(v => v.ToServiceModel())
                    .ToList()
            };
        }

        /// <summary>
        /// Create from service model
        /// </summary>
        /// <param name="model"></param>
        public static ModificationInfoApiModel ToApiModel(
            this ModificationInfoModel model) {
            if (model == null) {
                return null;
            }
            return new ModificationInfoApiModel {
                ModificationTime = model.ModificationTime,
                UpdateType = (HistoryUpdateOperation?)model.UpdateType,
                UserName = model.UserName
            };
        }

        /// <summary>
        /// Create service model from api model
        /// </summary>
        public static ModificationInfoModel ToServiceModel(
            this ModificationInfoApiModel model) {
            if (model == null) {
                return null;
            }
            return new ModificationInfoModel {
                ModificationTime = model.ModificationTime,
                UpdateType = (OpcUa.History.Models.HistoryUpdateOperation?)model.UpdateType,
                UserName = model.UserName
            };
        }

        /// <summary>
        /// Create api model
        /// </summary>
        public static ReadEventsDetailsApiModel ToApiModel(
            this ReadEventsDetailsModel model) {
            if (model == null) {
                return null;
            }
            return new ReadEventsDetailsApiModel {
                EndTime = model.EndTime,
                StartTime = model.StartTime,
                NumEvents = model.NumEvents,
                Filter = model.Filter.ToApiModel()
            };
        }

        /// <summary>
        /// Create service model from api model
        /// </summary>
        public static ReadEventsDetailsModel ToServiceModel(
            this ReadEventsDetailsApiModel model) {
            if (model == null) {
                return null;
            }
            return new ReadEventsDetailsModel {
                EndTime = model.EndTime,
                StartTime = model.StartTime,
                NumEvents = model.NumEvents,
                Filter = model.Filter.ToServiceModel()
            };
        }

        /// <summary>
        /// Create api model
        /// </summary>
        public static ReadModifiedValuesDetailsApiModel ToApiModel(
            this ReadModifiedValuesDetailsModel model) {
            if (model == null) {
                return null;
            }
            return new ReadModifiedValuesDetailsApiModel {
                EndTime = model.EndTime,
                StartTime = model.StartTime,
                NumValues = model.NumValues
            };
        }

        /// <summary>
        /// Create service model from api model
        /// </summary>
        public static ReadModifiedValuesDetailsModel ToServiceModel(
            this ReadModifiedValuesDetailsApiModel model) {
            if (model == null) {
                return null;
            }
            return new ReadModifiedValuesDetailsModel {
                EndTime = model.EndTime,
                StartTime = model.StartTime,
                NumValues = model.NumValues
            };
        }

        /// <summary>
        /// Create api model
        /// </summary>
        public static ReadProcessedValuesDetailsApiModel ToApiModel(
            this ReadProcessedValuesDetailsModel model) {
            if (model == null) {
                return null;
            }
            return new ReadProcessedValuesDetailsApiModel {
                EndTime = model.EndTime,
                StartTime = model.StartTime,
                ProcessingInterval = model.ProcessingInterval,
                AggregateConfiguration = model.AggregateConfiguration.ToApiModel(),
                AggregateTypeId = model.AggregateTypeId
            };
        }

        /// <summary>
        /// Create service model from api model
        /// </summary>
        public static ReadProcessedValuesDetailsModel ToServiceModel(
            this ReadProcessedValuesDetailsApiModel model) {
            if (model == null) {
                return null;
            }
            return new ReadProcessedValuesDetailsModel {
                EndTime = model.EndTime,
                StartTime = model.StartTime,
                ProcessingInterval = model.ProcessingInterval,
                AggregateConfiguration = model.AggregateConfiguration.ToServiceModel(),
                AggregateTypeId = model.AggregateTypeId
            };
        }

        /// <summary>
        /// Create api model
        /// </summary>
        public static ReadValuesAtTimesDetailsApiModel ToApiModel(
            this ReadValuesAtTimesDetailsModel model) {
            if (model == null) {
                return null;
            }
            return new ReadValuesAtTimesDetailsApiModel {
                ReqTimes = model.ReqTimes,
                UseSimpleBounds = model.UseSimpleBounds
            };
        }

        /// <summary>
        /// Create service model from api model
        /// </summary>
        public static ReadValuesAtTimesDetailsModel ToServiceModel(
            this ReadValuesAtTimesDetailsApiModel model) {
            if (model == null) {
                return null;
            }
            return new ReadValuesAtTimesDetailsModel {
                ReqTimes = model.ReqTimes,
                UseSimpleBounds = model.UseSimpleBounds
            };
        }

        /// <summary>
        /// Create api model
        /// </summary>
        public static ReadValuesDetailsApiModel ToApiModel(
            this ReadValuesDetailsModel model) {
            if (model == null) {
                return null;
            }
            return new ReadValuesDetailsApiModel {
                EndTime = model.EndTime,
                StartTime = model.StartTime,
                NumValues = model.NumValues,
                ReturnBounds = model.ReturnBounds
            };
        }

        /// <summary>
        /// Create service model from api model
        /// </summary>
        public static ReadValuesDetailsModel ToServiceModel(
            this ReadValuesDetailsApiModel model) {
            if (model == null) {
                return null;
            }
            return new ReadValuesDetailsModel {
                EndTime = model.EndTime,
                StartTime = model.StartTime,
                NumValues = model.NumValues,
                ReturnBounds = model.ReturnBounds
            };
        }

        /// <summary>
        /// Create api model
        /// </summary>
        public static ReplaceEventsDetailsApiModel ToApiModel(
            this ReplaceEventsDetailsModel model) {
            if (model == null) {
                return null;
            }
            return new ReplaceEventsDetailsApiModel {
                Filter = model.Filter.ToApiModel(),
                Events = model.Events?
                    .Select(v => v.ToApiModel())
                    .ToList()
            };
        }

        /// <summary>
        /// Create service model from api model
        /// </summary>
        public static ReplaceEventsDetailsModel ToServiceModel(
            this ReplaceEventsDetailsApiModel model) {
            if (model == null) {
                return null;
            }
            return new ReplaceEventsDetailsModel {
                Filter = model.Filter.ToServiceModel(),
                Events = model.Events?
                    .Select(v => v.ToServiceModel())
                    .ToList()
            };
        }

        /// <summary>
        /// Create service model from api model
        /// </summary>
        public static ReplaceValuesDetailsApiModel ToApiModel(
            this ReplaceValuesDetailsModel model) {
            if (model == null) {
                return null;
            }
            return new ReplaceValuesDetailsApiModel {
                Values = model.Values?
                    .Select(v => v.ToApiModel())
                    .ToList()
            };
        }

        /// <summary>
        /// Create service model from api model
        /// </summary>
        public static ReplaceValuesDetailsModel ToServiceModel(
            this ReplaceValuesDetailsApiModel model) {
            if (model == null) {
                return null;
            }
            return new ReplaceValuesDetailsModel {
                Values = model.Values?
                    .Select(v => v.ToServiceModel())
                    .ToList()
            };
        }

        /// <summary>
        /// Create from service model
        /// </summary>
        /// <param name="model"></param>
        public static RequestHeaderApiModel ToApiModel(
            this RequestHeaderModel model) {
            if (model == null) {
                return null;
            }
            return new RequestHeaderApiModel {
                Diagnostics = model.Diagnostics.ToApiModel(),
                Elevation = model.Elevation.ToApiModel(),
                Locales = model.Locales?.ToList()
            };
        }

        /// <summary>
        /// Convert back to service model
        /// </summary>
        /// <returns></returns>
        public static RequestHeaderModel ToServiceModel(
            this RequestHeaderApiModel model) {
            if (model == null) {
                return null;
            }
            return new RequestHeaderModel {
                Diagnostics = model.Diagnostics.ToServiceModel(),
                Elevation = model.Elevation.ToServiceModel(),
                Locales = model.Locales?.ToList()
            };
        }

        /// <summary>
        /// Create api model from service model
        /// </summary>
        /// <param name="model"></param>
        public static ServiceResultApiModel ToApiModel(
            this ServiceResultModel model) {
            if (model == null) {
                return null;
            }
            return new ServiceResultApiModel {
                Diagnostics = model.Diagnostics?.Copy(),
                StatusCode = model.StatusCode,
                ErrorMessage = model.ErrorMessage
            };
        }

        /// <summary>
        /// Create service model from api model
        /// </summary>
        /// <param name="model"></param>
        public static ServiceResultModel ToServiceModel(
            this ServiceResultApiModel model) {
            if (model == null) {
                return null;
            }
            return new ServiceResultModel {
                Diagnostics = model.Diagnostics?.Copy(),
                StatusCode = model.StatusCode,
                ErrorMessage = model.ErrorMessage
            };
        }

        /// <summary>
        /// Create service model from api model
        /// </summary>
        public static SimpleAttributeOperandModel ToServiceModel(
            this SimpleAttributeOperandApiModel model) {
            if (model == null) {
                return null;
            }
            return new SimpleAttributeOperandModel {
                NodeId = model.NodeId,
                AttributeId = (OpcUa.Core.Models.NodeAttribute?)model.AttributeId,
                BrowsePath = model.BrowsePath,
                IndexRange = model.IndexRange
            };
        }

        /// <summary>
        /// Create api model
        /// </summary>
        public static SimpleAttributeOperandApiModel ToApiModel(
            this SimpleAttributeOperandModel model) {
            if (model == null) {
                return null;
            }
            return new SimpleAttributeOperandApiModel {
                NodeId = model.NodeId,
                AttributeId = (Core.Models.NodeAttribute?)model.AttributeId,
                BrowsePath = model.BrowsePath,
                IndexRange = model.IndexRange
            };
        }
    }
}