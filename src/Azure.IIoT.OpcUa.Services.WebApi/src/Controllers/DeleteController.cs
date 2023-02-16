// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Azure.IIoT.OpcUa.Services.WebApi.Controllers {
    using Azure.IIoT.OpcUa.Services.WebApi.Auth;
    using Azure.IIoT.OpcUa.Services.WebApi.Filters;
    using Azure.IIoT.OpcUa.Api.Models;
    using Microsoft.AspNetCore.Authorization;
    using Microsoft.AspNetCore.Mvc;
    using System;
    using System.ComponentModel.DataAnnotations;
    using System.Threading.Tasks;

    /// <summary>
    /// Services to delete history
    /// </summary>
    [ApiVersion("2")][Route("history/v{version:apiVersion}/delete")]
    [ExceptionsFilter]
    [Authorize(Policy = Policies.CanWrite)]
    [ApiController]
    public class DeleteController : ControllerBase {

        /// <summary>
        /// Create controller with service
        /// </summary>
        /// <param name="historian"></param>
        public DeleteController(IHistorianServices<string> historian) {
            _historian = historian ?? throw new ArgumentNullException(nameof(historian));
        }

        /// <summary>
        /// Delete value history at specified times
        /// </summary>
        /// <remarks>
        /// Delete value history using historic access.
        /// The endpoint must be activated and connected and the module client
        /// and server must trust each other.
        /// </remarks>
        /// <param name="endpointId">The identifier of the activated endpoint.</param>
        /// <param name="request">The history update request</param>
        /// <returns>The history delete result</returns>
        [HttpPost("{endpointId}/values/pick")]
        public async Task<HistoryUpdateResponseModel> HistoryDeleteValuesAtTimesAsync(
            string endpointId,
            [FromBody] [Required] HistoryUpdateRequestModel<DeleteValuesAtTimesDetailsModel> request) {
            if (request == null) {
                throw new ArgumentNullException(nameof(request));
            }
            var writeResult = await _historian.HistoryDeleteValuesAtTimesAsync(endpointId, request);
            return writeResult;
        }

        /// <summary>
        /// Delete historic values
        /// </summary>
        /// <remarks>
        /// Delete historic values using historic access.
        /// The endpoint must be activated and connected and the module client
        /// and server must trust each other.
        /// </remarks>
        /// <param name="endpointId">The identifier of the activated endpoint.</param>
        /// <param name="request">The history update request</param>
        /// <returns>The history delete result</returns>
        [HttpPost("{endpointId}/values")]
        public async Task<HistoryUpdateResponseModel> HistoryDeleteValuesAsync(
            string endpointId,
            [FromBody] [Required] HistoryUpdateRequestModel<DeleteValuesDetailsModel> request) {
            if (request == null) {
                throw new ArgumentNullException(nameof(request));
            }
            var writeResult = await _historian.HistoryDeleteValuesAsync(endpointId, request);
            return writeResult;
        }

        /// <summary>
        /// Delete historic values
        /// </summary>
        /// <remarks>
        /// Delete historic values using historic access.
        /// The endpoint must be activated and connected and the module client
        /// and server must trust each other.
        /// </remarks>
        /// <param name="endpointId">The identifier of the activated endpoint.</param>
        /// <param name="request">The history update request</param>
        /// <returns>The history delete result</returns>
        [HttpPost("{endpointId}/values/modified")]
        public async Task<HistoryUpdateResponseModel> HistoryDeleteModifiedValuesAsync(
            string endpointId,
            [FromBody] [Required] HistoryUpdateRequestModel<DeleteModifiedValuesDetailsModel> request) {
            if (request == null) {
                throw new ArgumentNullException(nameof(request));
            }
            var writeResult = await _historian.HistoryDeleteModifiedValuesAsync(endpointId, request);
            return writeResult;
        }

        /// <summary>
        /// Delete historic events
        /// </summary>
        /// <remarks>
        /// Delete historic events using historic access.
        /// The endpoint must be activated and connected and the module client
        /// and server must trust each other.
        /// </remarks>
        /// <param name="endpointId">The identifier of the activated endpoint.</param>
        /// <param name="request">The history update request</param>
        /// <returns>The history delete result</returns>
        [HttpPost("{endpointId}/events")]
        public async Task<HistoryUpdateResponseModel> HistoryDeleteEventsAsync(
            string endpointId,
            [FromBody] [Required] HistoryUpdateRequestModel<DeleteEventsDetailsModel> request) {
            if (request == null) {
                throw new ArgumentNullException(nameof(request));
            }
            var writeResult = await _historian.HistoryDeleteEventsAsync(endpointId, request);
            return writeResult;
        }

        private readonly IHistorianServices<string> _historian;
    }
}
