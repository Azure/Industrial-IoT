// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IIoT.Services.OpcUa.History.v2.Controllers {
    using Microsoft.Azure.IIoT.Services.OpcUa.History.v2.Auth;
    using Microsoft.Azure.IIoT.Services.OpcUa.History.v2.Filters;
    using Microsoft.Azure.IIoT.Services.OpcUa.History.v2.Models;
    using Microsoft.Azure.IIoT.OpcUa.History;
    using Microsoft.AspNetCore.Authorization;
    using Microsoft.AspNetCore.Mvc;
    using System;
    using System.Threading.Tasks;
    using System.ComponentModel.DataAnnotations;

    /// <summary>
    /// Services to delete history
    /// </summary>
    [Route(VersionInfo.PATH + "/delete")]
    [ExceptionsFilter]
    [Produces(ContentEncodings.MimeTypeJson)]
    [Authorize(Policy = Policies.CanDelete)]
    public class DeleteController : Controller {

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
        public async Task<HistoryUpdateResponseApiModel> HistoryDeleteValuesAtTimesAsync(
            string endpointId,
            [FromBody] [Required] HistoryUpdateRequestApiModel<DeleteValuesAtTimesDetailsApiModel> request) {
            if (request == null) {
                throw new ArgumentNullException(nameof(request));
            }
            var writeResult = await _historian.HistoryDeleteValuesAtTimesAsync(
                endpointId, request.ToServiceModel(d => d.ToServiceModel()));
            return new HistoryUpdateResponseApiModel(writeResult);
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
        public async Task<HistoryUpdateResponseApiModel> HistoryDeleteValuesAsync(
            string endpointId,
            [FromBody] [Required] HistoryUpdateRequestApiModel<DeleteValuesDetailsApiModel> request) {
            if (request == null) {
                throw new ArgumentNullException(nameof(request));
            }
            var writeResult = await _historian.HistoryDeleteValuesAsync(
                endpointId, request.ToServiceModel(d => d.ToServiceModel()));
            return new HistoryUpdateResponseApiModel(writeResult);
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
        public async Task<HistoryUpdateResponseApiModel> HistoryDeleteModifiedValuesAsync(
            string endpointId,
            [FromBody] [Required] HistoryUpdateRequestApiModel<DeleteModifiedValuesDetailsApiModel> request) {
            if (request == null) {
                throw new ArgumentNullException(nameof(request));
            }
            var writeResult = await _historian.HistoryDeleteModifiedValuesAsync(
                endpointId, request.ToServiceModel(d => d.ToServiceModel()));
            return new HistoryUpdateResponseApiModel(writeResult);
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
        public async Task<HistoryUpdateResponseApiModel> HistoryDeleteEventsAsync(
            string endpointId,
            [FromBody] [Required] HistoryUpdateRequestApiModel<DeleteEventsDetailsApiModel> request) {
            if (request == null) {
                throw new ArgumentNullException(nameof(request));
            }
            var writeResult = await _historian.HistoryDeleteEventsAsync(
                endpointId, request.ToServiceModel(d => d.ToServiceModel()));
            return new HistoryUpdateResponseApiModel(writeResult);
        }

        private readonly IHistorianServices<string> _historian;
    }
}
