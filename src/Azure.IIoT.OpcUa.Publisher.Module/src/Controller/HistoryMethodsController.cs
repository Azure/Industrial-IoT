// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Azure.IIoT.OpcUa.Publisher.Module.Controller {
    using Azure.IIoT.OpcUa;
    using Azure.IIoT.OpcUa.Publisher.Module.Filters;
    using Azure.IIoT.OpcUa.Shared.Models;
    using Microsoft.Azure.IIoT.Module.Framework;
    using System;
    using System.Threading.Tasks;

    /// <summary>
    /// History method controller
    /// </summary>
    [Version("_V1")]
    [Version("_V2")]
    [ExceptionsFilter]
    public class HistoryMethodsController : IMethodController {

        /// <summary>
        /// Create controller with service
        /// </summary>
        /// <param name="historian"></param>
        public HistoryMethodsController(IHistoryServices<ConnectionModel> historian) {
            _history = historian ?? throw new ArgumentNullException(nameof(historian));
        }

        /// <summary>
        /// Replace events
        /// </summary>
        /// <param name="connection"></param>
        /// <param name="request"></param>
        /// <returns></returns>
        public async Task<HistoryUpdateResponseModel> HistoryReplaceEventsAsync(ConnectionModel connection,
            HistoryUpdateRequestModel<UpdateEventsDetailsModel> request) {
            if (request == null) {
                throw new ArgumentNullException(nameof(request));
            }
            var result = await _history.HistoryReplaceEventsAsync(connection, request);
            return result;
        }

        /// <summary>
        /// Insert events
        /// </summary>
        /// <param name="connection"></param>
        /// <param name="request"></param>
        /// <returns></returns>
        public async Task<HistoryUpdateResponseModel> HistoryInsertEventsAsync(ConnectionModel connection,
            HistoryUpdateRequestModel<UpdateEventsDetailsModel> request) {
            if (request == null) {
                throw new ArgumentNullException(nameof(request));
            }
            var result = await _history.HistoryInsertEventsAsync(connection, request);
            return result;
        }

        /// <summary>
        /// Upsert events
        /// </summary>
        /// <param name="connection"></param>
        /// <param name="request"></param>
        /// <returns></returns>
        public async Task<HistoryUpdateResponseModel> HistoryUpsertEventsAsync(ConnectionModel connection,
            HistoryUpdateRequestModel<UpdateEventsDetailsModel> request) {
            if (request == null) {
                throw new ArgumentNullException(nameof(request));
            }
            var result = await _history.HistoryUpsertEventsAsync(connection, request);
            return result;
        }

        /// <summary>
        /// Delete events
        /// </summary>
        /// <param name="connection"></param>
        /// <param name="request"></param>
        /// <returns></returns>
        public async Task<HistoryUpdateResponseModel> HistoryDeleteEventsAsync(ConnectionModel connection,
            HistoryUpdateRequestModel<DeleteEventsDetailsModel> request) {
            if (request == null) {
                throw new ArgumentNullException(nameof(request));
            }
            var result = await _history.HistoryDeleteEventsAsync(connection, request);
            return result;
        }

        /// <summary>
        /// Delete values at specified times
        /// </summary>
        /// <param name="connection"></param>
        /// <param name="request"></param>
        /// <returns></returns>
        public async Task<HistoryUpdateResponseModel> HistoryDeleteValuesAtTimesAsync(ConnectionModel connection,
            HistoryUpdateRequestModel<DeleteValuesAtTimesDetailsModel> request) {
            if (request == null) {
                throw new ArgumentNullException(nameof(request));
            }
            var result = await _history.HistoryDeleteValuesAtTimesAsync(connection, request);
            return result;
        }

        /// <summary>
        /// Delete modified values
        /// </summary>
        /// <param name="connection"></param>
        /// <param name="request"></param>
        /// <returns></returns>
        public async Task<HistoryUpdateResponseModel> HistoryDeleteModifiedValuesAsync(ConnectionModel connection,
            HistoryUpdateRequestModel<DeleteValuesDetailsModel> request) {
            if (request == null) {
                throw new ArgumentNullException(nameof(request));
            }
            var result = await _history.HistoryDeleteModifiedValuesAsync(connection, request);
            return result;
        }

        /// <summary>
        /// Delete values
        /// </summary>
        /// <param name="connection"></param>
        /// <param name="request"></param>
        /// <returns></returns>
        public async Task<HistoryUpdateResponseModel> HistoryDeleteValuesAsync(ConnectionModel connection,
            HistoryUpdateRequestModel<DeleteValuesDetailsModel> request) {
            if (request == null) {
                throw new ArgumentNullException(nameof(request));
            }
            var result = await _history.HistoryDeleteValuesAsync(connection, request);
            return result;
        }

        /// <summary>
        /// Replace values
        /// </summary>
        /// <param name="connection"></param>
        /// <param name="request"></param>
        /// <returns></returns>
        public async Task<HistoryUpdateResponseModel> HistoryReplaceValuesAsync(ConnectionModel connection,
            HistoryUpdateRequestModel<UpdateValuesDetailsModel> request) {
            if (request == null) {
                throw new ArgumentNullException(nameof(request));
            }
            var result = await _history.HistoryReplaceValuesAsync(connection, request);
            return result;
        }

        /// <summary>
        /// Insert values
        /// </summary>
        /// <param name="connection"></param>
        /// <param name="request"></param>
        /// <returns></returns>
        public async Task<HistoryUpdateResponseModel> HistoryInsertValuesAsync(ConnectionModel connection,
            HistoryUpdateRequestModel<UpdateValuesDetailsModel> request) {
            if (request == null) {
                throw new ArgumentNullException(nameof(request));
            }
            var result = await _history.HistoryInsertValuesAsync(connection, request);
            return result;
        }

        /// <summary>
        /// Upsert values
        /// </summary>
        /// <param name="connection"></param>
        /// <param name="request"></param>
        /// <returns></returns>
        public async Task<HistoryUpdateResponseModel> HistoryUpsertValuesAsync(ConnectionModel connection,
            HistoryUpdateRequestModel<UpdateValuesDetailsModel> request) {
            if (request == null) {
                throw new ArgumentNullException(nameof(request));
            }
            var result = await _history.HistoryUpsertValuesAsync(connection, request);
            return result;
        }

        /// <summary>
        /// Read historic events
        /// </summary>
        /// <param name="connection"></param>
        /// <param name="request"></param>
        /// <returns></returns>
        public async Task<HistoryReadResponseModel<HistoricEventModel[]>> HistoryReadEventsAsync(
            ConnectionModel connection, HistoryReadRequestModel<ReadEventsDetailsModel> request) {
            if (request == null) {
                throw new ArgumentNullException(nameof(request));
            }
            var result = await _history.HistoryReadEventsAsync(connection, request);
            return result;
        }

        /// <summary>
        /// Read next set of events
        /// </summary>
        /// <param name="connection"></param>
        /// <param name="request"></param>
        /// <returns></returns>
        public async Task<HistoryReadNextResponseModel<HistoricEventModel[]>> HistoryReadEventsNextAsync(
            ConnectionModel connection, HistoryReadNextRequestModel request) {
            if (request == null) {
                throw new ArgumentNullException(nameof(request));
            }
            var result = await _history.HistoryReadEventsNextAsync(connection, request);
            return result;
        }

        /// <summary>
        /// Read historic values
        /// </summary>
        /// <param name="connection"></param>
        /// <param name="request"></param>
        /// <returns></returns>
        public async Task<HistoryReadResponseModel<HistoricValueModel[]>> HistoryReadValuesAsync(
            ConnectionModel connection, HistoryReadRequestModel<ReadValuesDetailsModel> request) {
            if (request == null) {
                throw new ArgumentNullException(nameof(request));
            }
            var result = await _history.HistoryReadValuesAsync(connection, request);
            return result;
        }

        /// <summary>
        /// Read historic values at times
        /// </summary>
        /// <param name="connection"></param>
        /// <param name="request"></param>
        /// <returns></returns>
        public async Task<HistoryReadResponseModel<HistoricValueModel[]>> HistoryReadValuesAtTimesAsync(
            ConnectionModel connection, HistoryReadRequestModel<ReadValuesAtTimesDetailsModel> request) {
            if (request == null) {
                throw new ArgumentNullException(nameof(request));
            }
            var result = await _history.HistoryReadValuesAtTimesAsync(connection, request);
            return result;
        }

        /// <summary>
        /// Read processed historic values
        /// </summary>
        /// <param name="connection"></param>
        /// <param name="request"></param>
        /// <returns></returns>
        public async Task<HistoryReadResponseModel<HistoricValueModel[]>> HistoryReadProcessedValuesAsync(
            ConnectionModel connection, HistoryReadRequestModel<ReadProcessedValuesDetailsModel> request) {
            if (request == null) {
                throw new ArgumentNullException(nameof(request));
            }
            var result = await _history.HistoryReadProcessedValuesAsync(connection, request);
            return result;
        }

        /// <summary>
        /// Read modified values
        /// </summary>
        /// <param name="connection"></param>
        /// <param name="request"></param>
        /// <returns></returns>
        public async Task<HistoryReadResponseModel<HistoricValueModel[]>> HistoryReadModifiedValuesAsync(
            ConnectionModel connection, HistoryReadRequestModel<ReadModifiedValuesDetailsModel> request) {
            if (request == null) {
                throw new ArgumentNullException(nameof(request));
            }
            var result = await _history.HistoryReadModifiedValuesAsync(connection, request);
            return result;
        }

        /// <summary>
        /// Read next set of historic values
        /// </summary>
        /// <param name="connection"></param>
        /// <param name="request"></param>
        /// <returns></returns>
        public async Task<HistoryReadNextResponseModel<HistoricValueModel[]>> HistoryReadValuesNextAsync(
            ConnectionModel connection, HistoryReadNextRequestModel request) {
            if (request == null) {
                throw new ArgumentNullException(nameof(request));
            }
            var result = await _history.HistoryReadValuesNextAsync(connection, request);
            return result;
        }

        private readonly IHistoryServices<ConnectionModel> _history;
    }
}
