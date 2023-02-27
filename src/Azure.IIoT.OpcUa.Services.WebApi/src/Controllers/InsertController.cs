// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Azure.IIoT.OpcUa.Services.WebApi.Controllers
{
    using Azure.IIoT.OpcUa.Services.WebApi.Auth;
    using Azure.IIoT.OpcUa.Services.WebApi.Filters;
    using Azure.IIoT.OpcUa;
    using Azure.IIoT.OpcUa.Models;
    using Microsoft.AspNetCore.Authorization;
    using Microsoft.AspNetCore.Mvc;
    using System;
    using System.ComponentModel.DataAnnotations;
    using System.Threading.Tasks;

    /// <summary>
    /// History insert services
    /// </summary>
    [ApiVersion("2")]
    [Route("history/v{version:apiVersion}/insert")]
    [ExceptionsFilter]
    [Authorize(Policy = Policies.CanWrite)]
    [ApiController]
    public class InsertController : ControllerBase
    {
        /// <summary>
        /// Create controller with service
        /// </summary>
        /// <param name="historian"></param>
        public InsertController(IHistoryServices<string> historian)
        {
            _historian = historian ?? throw new ArgumentNullException(nameof(historian));
        }

        /// <summary>
        /// Insert historic values
        /// </summary>
        /// <remarks>
        /// Insert historic values using historic access.
        /// The endpoint must be in the registry and the server accessible.
        /// </remarks>
        /// <param name="endpointId">The identifier of the activated endpoint.</param>
        /// <param name="request">The history insert request</param>
        /// <returns>The history insert result</returns>
        /// <exception cref="ArgumentNullException"><paramref name="request"/> is <c>null</c>.</exception>
        [HttpPost("{endpointId}/values")]
        public async Task<HistoryUpdateResponseModel> HistoryInsertValuesAsync(
            string endpointId,
            [FromBody][Required] HistoryUpdateRequestModel<UpdateValuesDetailsModel> request)
        {
            if (request == null)
            {
                throw new ArgumentNullException(nameof(request));
            }
            return await _historian.HistoryInsertValuesAsync(endpointId, request).ConfigureAwait(false);
        }

        /// <summary>
        /// Insert historic events
        /// </summary>
        /// <remarks>
        /// Insert historic events using historic access.
        /// The endpoint must be in the registry and the server accessible.
        /// </remarks>
        /// <param name="endpointId">The identifier of the activated endpoint.</param>
        /// <param name="request">The history insert request</param>
        /// <returns>The history insert result</returns>
        /// <exception cref="ArgumentNullException"><paramref name="request"/> is <c>null</c>.</exception>
        [HttpPost("{endpointId}/events")]
        public async Task<HistoryUpdateResponseModel> HistoryInsertEventsAsync(
            string endpointId,
            [FromBody][Required] HistoryUpdateRequestModel<UpdateEventsDetailsModel> request)
        {
            if (request == null)
            {
                throw new ArgumentNullException(nameof(request));
            }
            return await _historian.HistoryInsertEventsAsync(endpointId, request).ConfigureAwait(false);
        }

        private readonly IHistoryServices<string> _historian;
    }
}
