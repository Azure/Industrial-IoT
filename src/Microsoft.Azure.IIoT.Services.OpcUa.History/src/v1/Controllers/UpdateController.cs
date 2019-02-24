// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IIoT.Services.OpcUa.History.v1.Controllers {
    using Microsoft.Azure.IIoT.Services.OpcUa.History.v1.Auth;
    using Microsoft.Azure.IIoT.Services.OpcUa.History.v1.Filters;
    using Microsoft.Azure.IIoT.Services.OpcUa.History.v1.Models;
    using Microsoft.Azure.IIoT.OpcUa.History;
    using Microsoft.AspNetCore.Authorization;
    using Microsoft.AspNetCore.Mvc;
    using System;
    using System.Threading.Tasks;
    using System.ComponentModel.DataAnnotations;
    using Newtonsoft.Json.Linq;

    /// <summary>
    /// History update services
    /// </summary>
    [Route(VersionInfo.PATH + "/update")]
    [ExceptionsFilter]
    [Produces(ContentEncodings.MimeTypeJson)]
    [Authorize(Policy = Policies.CanUpdate)]
    public class UpdateController : Controller {

        /// <summary>
        /// Create controller with service
        /// </summary>
        /// <param name="historian"></param>
        /// <param name="client"></param>
        public UpdateController(IHistorianServices<string> historian, IHistoricAccessServices<string> client) {
            _client = client ?? throw new ArgumentNullException(nameof(client));
            _historian = historian ?? throw new ArgumentNullException(nameof(historian));
        }

        /// <summary>
        /// Update historic values
        /// </summary>
        /// <remarks>
        /// Update historic values using historic access.
        /// The endpoint must be activated and connected and the module client
        /// and server must trust each other.
        /// </remarks>
        /// <param name="endpointId">The identifier of the activated endpoint.</param>
        /// <param name="request">The history update request</param>
        /// <returns>The history update result</returns>
        [HttpPost("{endpointId}/values")]
        public async Task<HistoryUpdateResponseApiModel> HistoryUpdateValuesAsync(
            string endpointId,
            [FromBody] [Required] HistoryUpdateRequestApiModel<UpdateValuesDetailsApiModel> request) {
            if (request == null) {
                throw new ArgumentNullException(nameof(request));
            }
            var writeResult = await _historian.HistoryUpdateValuesAsync(
                endpointId, request.ToServiceModel(d => d.ToServiceModel()));
            return new HistoryUpdateResponseApiModel(writeResult);
        }

        /// <summary>
        /// Update historic events
        /// </summary>
        /// <remarks>
        /// Update historic events using historic access.
        /// The endpoint must be activated and connected and the module client
        /// and server must trust each other.
        /// </remarks>
        /// <param name="endpointId">The identifier of the activated endpoint.</param>
        /// <param name="request">The history update request</param>
        /// <returns>The history update result</returns>
        [HttpPost("{endpointId}/events")]
        public async Task<HistoryUpdateResponseApiModel> HistoryUpdateEventsAsync(
            string endpointId,
            [FromBody] [Required] HistoryUpdateRequestApiModel<UpdateEventsDetailsApiModel> request) {
            if (request == null) {
                throw new ArgumentNullException(nameof(request));
            }
            var writeResult = await _historian.HistoryUpdateEventsAsync(
                endpointId, request.ToServiceModel(d => d.ToServiceModel()));
            return new HistoryUpdateResponseApiModel(writeResult);
        }

        /// <summary>
        /// Update node history using raw json
        /// </summary>
        /// <remarks>
        /// Update node history using historic access.
        /// The endpoint must be activated and connected and the module client
        /// and server must trust each other.
        /// </remarks>
        /// <param name="endpointId">The identifier of the activated endpoint.</param>
        /// <param name="request">The history update request</param>
        /// <returns>The history update result</returns>
        [HttpPost("{endpointId}/raw")]
        [Authorize(Policy = Policies.CanDelete)]
        public async Task<HistoryUpdateResponseApiModel> HistoryUpdateRawAsync(
            string endpointId, [FromBody] [Required] HistoryUpdateRequestApiModel<JToken> request) {
            if (request == null) {
                throw new ArgumentNullException(nameof(request));
            }
            var writeResult = await _client.HistoryUpdateAsync(
                endpointId, request.ToServiceModel(d => d));
            return new HistoryUpdateResponseApiModel(writeResult);
        }

        private readonly IHistoricAccessServices<string> _client;
        private readonly IHistorianServices<string> _historian;
    }
}
