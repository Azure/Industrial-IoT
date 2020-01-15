// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IIoT.Services.OpcUa.Twin.History.Models {
    using Microsoft.Azure.IIoT.OpcUa.Api.History.Models;
    using Microsoft.Azure.IIoT.OpcUa.Core.Models;
    using Microsoft.Azure.IIoT.OpcUa.History.Models;
    using System;
    using System.Linq;

    /// <summary>
    /// Aggregate configuration
    /// </summary>
    public static class ModelExtensions {

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
                FilterOperator = (IIoT.OpcUa.Core.Models.FilterOperatorType)model.FilterOperator
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
                Value = model.Value,
                Type = (IIoT.OpcUa.Core.Models.CredentialType?)model.Type
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
        public static DiagnosticsModel ToServiceModel(
            this DiagnosticsApiModel model) {
            if (model == null) {
                return null;
            }
            return new DiagnosticsModel {
                AuditId = model.AuditId,
                Level = (IIoT.OpcUa.Core.Models.DiagnosticsLevel)model.Level,
                TimeStamp = model.TimeStamp
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
                AttributeId = (IIoT.OpcUa.Core.Models.NodeAttribute?)model.AttributeId,
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
                BrowsePath = model.BrowsePath,
                NodeId = model.NodeId,
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
                UpdateType = (IIoT.OpcUa.Api.History.Models.HistoryUpdateOperation?)model.UpdateType,
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
                UpdateType = (IIoT.OpcUa.History.Models.HistoryUpdateOperation?)model.UpdateType,
                UserName = model.UserName
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
        /// Create node api model from service model
        /// </summary>
        /// <param name="model"></param>
        public static ServiceResultApiModel ToApiModel(
            this ServiceResultModel model) {
            if (model == null) {
                return null;
            }
            return new ServiceResultApiModel {
                Diagnostics = model.Diagnostics,
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
                AttributeId = (IIoT.OpcUa.Core.Models.NodeAttribute?)model.AttributeId,
                BrowsePath = model.BrowsePath,
                IndexRange = model.IndexRange
            };
        }
    }
}