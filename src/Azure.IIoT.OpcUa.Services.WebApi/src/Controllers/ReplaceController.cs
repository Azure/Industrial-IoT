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
    /// History replace services
    /// </summary>
    [ApiVersion("2")]
    [Route("history/v{version:apiVersion}/replace")]
    [ExceptionsFilter]
    [Authorize(Policy = Policies.CanWrite)]
    [ApiController]
    public class ReplaceController : ControllerBase {

        /// <summary>
        /// Create controller with service
        /// </summary>
        /// <param name="historian"></param>
        public ReplaceController(IHistoryServices<string> historian) {
            _historian = historian ?? throw new ArgumentNullException(nameof(historian));
        }

        /// <summary>
        /// Replace historic values
        /// </summary>
        /// <remarks>
        /// Replace historic values using historic access.
        /// The endpoint must be in the registry and the server accessible.
        /// </remarks>
        /// <param name="endpointId">The identifier of the activated endpoint.</param>
        /// <param name="request">The history replace request</param>
        /// <returns>The history replace result</returns>
        [HttpPost("{endpointId}/values")]
        public async Task<HistoryUpdateResponseModel> HistoryReplaceValuesAsync(
            string endpointId,
            [FromBody][Required] HistoryUpdateRequestModel<UpdateValuesDetailsModel> request) {
            if (request == null) {
                throw new ArgumentNullException(nameof(request));
            }
            var writeResult = await _historian.HistoryReplaceValuesAsync(endpointId, request);
            return writeResult;
        }

        /// <summary>
        /// Replace historic events
        /// </summary>
        /// <remarks>
        /// Replace historic events using historic access.
        /// The endpoint must be in the registry and the server accessible.
        /// </remarks>
        /// <param name="endpointId">The identifier of the activated endpoint.</param>
        /// <param name="request">The history replace request</param>
        /// <returns>The history replace result</returns>
        [HttpPost("{endpointId}/events")]
        public async Task<HistoryUpdateResponseModel> HistoryReplaceEventsAsync(
            string endpointId,
            [FromBody][Required] HistoryUpdateRequestModel<UpdateEventsDetailsModel> request) {
            if (request == null) {
                throw new ArgumentNullException(nameof(request));
            }
            var writeResult = await _historian.HistoryReplaceEventsAsync(endpointId, request);
            return writeResult;
        }

        private readonly IHistoryServices<string> _historian;
    }
}
