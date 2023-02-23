// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Azure.IIoT.OpcUa.Services.WebApi.Controllers {
    using Azure.IIoT.OpcUa.Services.WebApi.Auth;
    using Azure.IIoT.OpcUa.Services.WebApi.Filters;
    using Azure.IIoT.OpcUa;
    using Azure.IIoT.OpcUa.Shared.Models;
    using Microsoft.AspNetCore.Authorization;
    using Microsoft.AspNetCore.Mvc;
    using System;
    using System.ComponentModel.DataAnnotations;
    using System.Threading.Tasks;

    /// <summary>
    /// Historic access read services
    /// </summary>
    [ApiVersion("2")]
    [Route("history/v{version:apiVersion}/read")]
    [ExceptionsFilter]
    [Authorize(Policy = Policies.CanRead)]
    [ApiController]
    public class ReadController : ControllerBase {
        /// <summary>
        /// Create controller with service
        /// </summary>
        /// <param name="historian"></param>
        public ReadController(IHistoryServices<string> historian) {
            _historian = historian ?? throw new ArgumentNullException(nameof(historian));
        }

        /// <summary>
        /// Read historic events
        /// </summary>
        /// <remarks>
        /// Read historic events of a node if available using historic access.
        /// The endpoint must be activated and connected and the module client
        /// and server must trust each other.
        /// </remarks>
        /// <param name="endpointId">The identifier of the activated endpoint.</param>
        /// <param name="request">The history read request</param>
        /// <returns>The historic events</returns>
        [HttpPost("{endpointId}/events")]
        public async Task<HistoryReadResponseModel<HistoricEventModel[]>> HistoryReadEventsAsync(
            string endpointId,
            [FromBody][Required] HistoryReadRequestModel<ReadEventsDetailsModel> request) {
            if (request == null) {
                throw new ArgumentNullException(nameof(request));
            }
            return await _historian.HistoryReadEventsAsync(endpointId, request).ConfigureAwait(false);
        }

        /// <summary>
        /// Read next batch of historic events
        /// </summary>
        /// <remarks>
        /// Read next batch of historic events of a node using historic access.
        /// The endpoint must be activated and connected and the module client
        /// and server must trust each other.
        /// </remarks>
        /// <param name="endpointId">The identifier of the activated endpoint.</param>
        /// <param name="request">The history read next request</param>
        /// <returns>The historic events</returns>
        [HttpPost("{endpointId}/events/next")]
        public async Task<HistoryReadNextResponseModel<HistoricEventModel[]>> HistoryReadEventsNextAsync(
            string endpointId,
            [FromBody][Required] HistoryReadNextRequestModel request) {
            if (request == null) {
                throw new ArgumentNullException(nameof(request));
            }
            return await _historian.HistoryReadEventsNextAsync(endpointId, request).ConfigureAwait(false);
        }

        /// <summary>
        /// Read historic processed values at specified times
        /// </summary>
        /// <remarks>
        /// Read processed history values of a node if available using historic access.
        /// The endpoint must be activated and connected and the module client
        /// and server must trust each other.
        /// </remarks>
        /// <param name="endpointId">The identifier of the activated endpoint.</param>
        /// <param name="request">The history read request</param>
        /// <returns>The historic values</returns>
        [HttpPost("{endpointId}/values")]
        public async Task<HistoryReadResponseModel<HistoricValueModel[]>> HistoryReadValuesAsync(
            string endpointId,
            [FromBody][Required] HistoryReadRequestModel<ReadValuesDetailsModel> request) {
            if (request == null) {
                throw new ArgumentNullException(nameof(request));
            }
            return await _historian.HistoryReadValuesAsync(endpointId, request).ConfigureAwait(false);
        }

        /// <summary>
        /// Read historic values at specified times
        /// </summary>
        /// <remarks>
        /// Read historic values of a node if available using historic access.
        /// The endpoint must be activated and connected and the module client
        /// and server must trust each other.
        /// </remarks>
        /// <param name="endpointId">The identifier of the activated endpoint.</param>
        /// <param name="request">The history read request</param>
        /// <returns>The historic values</returns>
        [HttpPost("{endpointId}/values/pick")]
        public async Task<HistoryReadResponseModel<HistoricValueModel[]>> HistoryReadValuesAtTimesAsync(
            string endpointId,
            [FromBody][Required] HistoryReadRequestModel<ReadValuesAtTimesDetailsModel> request) {
            if (request == null) {
                throw new ArgumentNullException(nameof(request));
            }
            return await _historian.HistoryReadValuesAtTimesAsync(endpointId, request).ConfigureAwait(false);
        }

        /// <summary>
        /// Read historic processed values at specified times
        /// </summary>
        /// <remarks>
        /// Read processed history values of a node if available using historic access.
        /// The endpoint must be activated and connected and the module client
        /// and server must trust each other.
        /// </remarks>
        /// <param name="endpointId">The identifier of the activated endpoint.</param>
        /// <param name="request">The history read request</param>
        /// <returns>The historic values</returns>
        [HttpPost("{endpointId}/values/processed")]
        public async Task<HistoryReadResponseModel<HistoricValueModel[]>> HistoryReadProcessedValuesAsync(
            string endpointId,
            [FromBody][Required] HistoryReadRequestModel<ReadProcessedValuesDetailsModel> request) {
            if (request == null) {
                throw new ArgumentNullException(nameof(request));
            }
            return await _historian.HistoryReadProcessedValuesAsync(endpointId, request).ConfigureAwait(false);
        }

        /// <summary>
        /// Read historic modified values at specified times
        /// </summary>
        /// <remarks>
        /// Read processed history values of a node if available using historic access.
        /// The endpoint must be activated and connected and the module client
        /// and server must trust each other.
        /// </remarks>
        /// <param name="endpointId">The identifier of the activated endpoint.</param>
        /// <param name="request">The history read request</param>
        /// <returns>The historic values</returns>
        [HttpPost("{endpointId}/values/modified")]
        public async Task<HistoryReadResponseModel<HistoricValueModel[]>> HistoryReadModifiedValuesAsync(
            string endpointId,
            [FromBody][Required] HistoryReadRequestModel<ReadModifiedValuesDetailsModel> request) {
            if (request == null) {
                throw new ArgumentNullException(nameof(request));
            }
            return await _historian.HistoryReadModifiedValuesAsync(endpointId, request).ConfigureAwait(false);
        }

        /// <summary>
        /// Read next batch of historic values
        /// </summary>
        /// <remarks>
        /// Read next batch of historic values of a node using historic access.
        /// The endpoint must be activated and connected and the module client
        /// and server must trust each other.
        /// </remarks>
        /// <param name="endpointId">The identifier of the activated endpoint.</param>
        /// <param name="request">The history read next request</param>
        /// <returns>The historic values</returns>
        [HttpPost("{endpointId}/values/next")]
        public async Task<HistoryReadNextResponseModel<HistoricValueModel[]>> HistoryReadValueNextAsync(
            string endpointId,
            [FromBody][Required] HistoryReadNextRequestModel request) {
            if (request == null) {
                throw new ArgumentNullException(nameof(request));
            }
            return await _historian.HistoryReadValuesNextAsync(endpointId, request).ConfigureAwait(false);
        }

        private readonly IHistoryServices<string> _historian;
    }
}
