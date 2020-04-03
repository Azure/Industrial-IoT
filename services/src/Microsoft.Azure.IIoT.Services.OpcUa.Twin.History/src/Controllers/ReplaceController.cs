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
    using Microsoft.Azure.IIoT.OpcUa.Api.History.Models;

    /// <summary>
    /// History replace services
    /// </summary>
    [ApiVersion("2")][Route("v{version:apiVersion}/replace")]
    [ExceptionsFilter]
    [Authorize(Policy = Policies.CanUpdate)]
    [ApiController]
    public class ReplaceController : ControllerBase {

        /// <summary>
        /// Create controller with service
        /// </summary>
        /// <param name="historian"></param>
        public ReplaceController(IHistorianServices<string> historian) {
            _historian = historian ?? throw new ArgumentNullException(nameof(historian));
        }

        /// <summary>
        /// Replace historic values
        /// </summary>
        /// <remarks>
        /// Replace historic values using historic access.
        /// The endpoint must be activated and connected and the module client
        /// and server must trust each other.
        /// </remarks>
        /// <param name="endpointId">The identifier of the activated endpoint.</param>
        /// <param name="request">The history replace request</param>
        /// <returns>The history replace result</returns>
        [HttpPost("{endpointId}/values")]
        public async Task<HistoryUpdateResponseApiModel> HistoryReplaceValuesAsync(
            string endpointId,
            [FromBody] [Required] HistoryUpdateRequestApiModel<ReplaceValuesDetailsApiModel> request) {
            if (request == null) {
                throw new ArgumentNullException(nameof(request));
            }
            var writeResult = await _historian.HistoryReplaceValuesAsync(
                endpointId, request.ToServiceModel(d => d.ToServiceModel()));
            return writeResult.ToApiModel();
        }

        /// <summary>
        /// Replace historic events
        /// </summary>
        /// <remarks>
        /// Replace historic events using historic access.
        /// The endpoint must be activated and connected and the module client
        /// and server must trust each other.
        /// </remarks>
        /// <param name="endpointId">The identifier of the activated endpoint.</param>
        /// <param name="request">The history replace request</param>
        /// <returns>The history replace result</returns>
        [HttpPost("{endpointId}/events")]
        public async Task<HistoryUpdateResponseApiModel> HistoryReplaceEventsAsync(
            string endpointId,
            [FromBody] [Required] HistoryUpdateRequestApiModel<ReplaceEventsDetailsApiModel> request) {
            if (request == null) {
                throw new ArgumentNullException(nameof(request));
            }
            var writeResult = await _historian.HistoryReplaceEventsAsync(
                endpointId, request.ToServiceModel(d => d.ToServiceModel()));
            return writeResult.ToApiModel();
        }

        private readonly IHistorianServices<string> _historian;
    }
}
