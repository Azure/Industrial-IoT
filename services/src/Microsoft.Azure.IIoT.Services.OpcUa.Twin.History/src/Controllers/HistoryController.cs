// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IIoT.Services.OpcUa.Twin.History.Controllers {
    using Microsoft.Azure.IIoT.Services.OpcUa.Twin.History.Auth;
    using Microsoft.Azure.IIoT.Services.OpcUa.Twin.History.Filters;
    using Microsoft.Azure.IIoT.OpcUa.Api.History.Models;
    using Microsoft.Azure.IIoT.OpcUa.History;
    using Microsoft.AspNetCore.Authorization;
    using Microsoft.AspNetCore.Mvc;
    using Microsoft.Azure.IIoT.Serializers;
    using System;
    using System.ComponentModel.DataAnnotations;
    using System.Threading.Tasks;

    /// <summary>
    /// History raw access services
    /// </summary>
    [ApiVersion("2")][Route("v{version:apiVersion}/history")]
    [ExceptionsFilter]
    [Authorize(Policy = Policies.CanUpdate)]
    [ApiController]
    public class HistoryController : ControllerBase {

        /// <summary>
        /// Create controller with service
        /// </summary>
        /// <param name="client"></param>
        public HistoryController(IHistoricAccessServices<string> client) {
            _client = client ?? throw new ArgumentNullException(nameof(client));
        }

        /// <summary>
        /// Read history using json details
        /// </summary>
        /// <remarks>
        /// Read node history if available using historic access.
        /// The endpoint must be activated and connected and the module client
        /// and server must trust each other.
        /// </remarks>
        /// <param name="endpointId">The identifier of the activated endpoint.</param>
        /// <param name="request">The history read request</param>
        /// <returns>The history read response</returns>
        [HttpPost("read/{endpointId}")]
        public async Task<HistoryReadResponseApiModel<VariantValue>> HistoryReadRawAsync(
            string endpointId, [FromBody] [Required] HistoryReadRequestApiModel<VariantValue> request) {
            if (request == null) {
                throw new ArgumentNullException(nameof(request));
            }
            var readresult = await _client.HistoryReadAsync(
                endpointId, request.ToServiceModel(d => d));
            return readresult.ToApiModel(d => d);
        }

        /// <summary>
        /// Read next batch of history as json
        /// </summary>
        /// <remarks>
        /// Read next batch of node history values using historic access.
        /// The endpoint must be activated and connected and the module client
        /// and server must trust each other.
        /// </remarks>
        /// <param name="endpointId">The identifier of the activated endpoint.</param>
        /// <param name="request">The history read next request</param>
        /// <returns>The history read response</returns>
        [HttpPost("read/{endpointId}/next")]
        public async Task<HistoryReadNextResponseApiModel<VariantValue>> HistoryReadRawNextAsync(
            string endpointId, [FromBody] [Required] HistoryReadNextRequestApiModel request) {
            if (request == null) {
                throw new ArgumentNullException(nameof(request));
            }
            var readresult = await _client.HistoryReadNextAsync(
                endpointId, request.ToServiceModel());
            return readresult.ToApiModel(d => d);
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
        [HttpPost("update/{endpointId}")]
        [Authorize(Policy = Policies.CanDelete)]
        public async Task<HistoryUpdateResponseApiModel> HistoryUpdateRawAsync(
            string endpointId, [FromBody] [Required] HistoryUpdateRequestApiModel<VariantValue> request) {
            if (request == null) {
                throw new ArgumentNullException(nameof(request));
            }
            var writeResult = await _client.HistoryUpdateAsync(
                endpointId, request.ToServiceModel(d => d));
            return writeResult.ToApiModel();
        }

        private readonly IHistoricAccessServices<string> _client;
    }
}
