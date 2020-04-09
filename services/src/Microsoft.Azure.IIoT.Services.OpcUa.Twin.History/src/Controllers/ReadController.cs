// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IIoT.Services.OpcUa.Twin.History.Controllers {
    using Microsoft.Azure.IIoT.Services.OpcUa.Twin.History.Auth;
    using Microsoft.Azure.IIoT.Services.OpcUa.Twin.History.Filters;
    using Microsoft.Azure.IIoT.OpcUa.History;
    using Microsoft.AspNetCore.Authorization;
    using Microsoft.AspNetCore.Mvc;
    using System;
    using System.Threading.Tasks;
    using System.ComponentModel.DataAnnotations;
    using System.Linq;
    using Microsoft.Azure.IIoT.OpcUa.Api.History.Models;

    /// <summary>
    /// Historic access read services
    /// </summary>
    [ApiVersion("2")][Route("v{version:apiVersion}/read")]
    [ExceptionsFilter]
    [Authorize(Policy = Policies.CanRead)]
    [ApiController]
    public class ReadController : ControllerBase {

        /// <summary>
        /// Create controller with service
        /// </summary>
        /// <param name="historian"></param>
        public ReadController(IHistorianServices<string> historian) {
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
        public async Task<HistoryReadResponseApiModel<HistoricEventApiModel[]>> HistoryReadEventsAsync(
            string endpointId,
            [FromBody] [Required] HistoryReadRequestApiModel<ReadEventsDetailsApiModel> request) {
            if (request == null) {
                throw new ArgumentNullException(nameof(request));
            }
            var readresult = await _historian.HistoryReadEventsAsync(
                endpointId, request.ToServiceModel(d => d.ToServiceModel()));
            return readresult.ToApiModel(d => d?.Select(v => v.ToApiModel()).ToArray());
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
        public async Task<HistoryReadNextResponseApiModel<HistoricEventApiModel[]>> HistoryReadEventsNextAsync(
            string endpointId,
            [FromBody] [Required] HistoryReadNextRequestApiModel request) {
            if (request == null) {
                throw new ArgumentNullException(nameof(request));
            }
            var readresult = await _historian.HistoryReadEventsNextAsync(
                endpointId, request.ToServiceModel());
            return readresult.ToApiModel(d => d?.Select(v => v.ToApiModel()).ToArray());
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
        public async Task<HistoryReadResponseApiModel<HistoricValueApiModel[]>> HistoryReadValuesAsync(
            string endpointId,
            [FromBody] [Required] HistoryReadRequestApiModel<ReadValuesDetailsApiModel> request) {
            if (request == null) {
                throw new ArgumentNullException(nameof(request));
            }
            var readresult = await _historian.HistoryReadValuesAsync(
                endpointId, request.ToServiceModel(d => d.ToServiceModel()));
            return readresult.ToApiModel(d => d?.Select(v => v.ToApiModel()).ToArray());
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
        public async Task<HistoryReadResponseApiModel<HistoricValueApiModel[]>> HistoryReadValuesAtTimesAsync(
            string endpointId,
            [FromBody] [Required] HistoryReadRequestApiModel<ReadValuesAtTimesDetailsApiModel> request) {
            if (request == null) {
                throw new ArgumentNullException(nameof(request));
            }
            var readresult = await _historian.HistoryReadValuesAtTimesAsync(
                endpointId, request.ToServiceModel(d => d.ToServiceModel()));
            return readresult.ToApiModel(d => d?.Select(v => v.ToApiModel()).ToArray());
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
        public async Task<HistoryReadResponseApiModel<HistoricValueApiModel[]>> HistoryReadProcessedValuesAsync(
            string endpointId,
            [FromBody] [Required] HistoryReadRequestApiModel<ReadProcessedValuesDetailsApiModel> request) {
            if (request == null) {
                throw new ArgumentNullException(nameof(request));
            }
            var readresult = await _historian.HistoryReadProcessedValuesAsync(
                endpointId, request.ToServiceModel(d => d.ToServiceModel()));
            return readresult.ToApiModel(d => d?.Select(v => v.ToApiModel()).ToArray());
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
        public async Task<HistoryReadResponseApiModel<HistoricValueApiModel[]>> HistoryReadModifiedValuesAsync(
            string endpointId,
            [FromBody] [Required] HistoryReadRequestApiModel<ReadModifiedValuesDetailsApiModel> request) {
            if (request == null) {
                throw new ArgumentNullException(nameof(request));
            }
            var readresult = await _historian.HistoryReadModifiedValuesAsync(
                endpointId, request.ToServiceModel(d => d.ToServiceModel()));
            return readresult.ToApiModel(d => d?.Select(v => v.ToApiModel()).ToArray());
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
        public async Task<HistoryReadNextResponseApiModel<HistoricValueApiModel[]>> HistoryReadValueNextAsync(
            string endpointId,
            [FromBody] [Required] HistoryReadNextRequestApiModel request) {
            if (request == null) {
                throw new ArgumentNullException(nameof(request));
            }
            var readresult = await _historian.HistoryReadValuesNextAsync(
                endpointId, request.ToServiceModel());
            return readresult.ToApiModel(d => d?.Select(v => v.ToApiModel()).ToArray());
        }

        private readonly IHistorianServices<string> _historian;
    }
}
