// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Azure.IIoT.OpcUa.Publisher.Service.WebApi.Controllers
{
    using Azure.IIoT.OpcUa.Publisher.Service.WebApi.Filters;
    using Azure.IIoT.OpcUa.Publisher.Models;
    using Asp.Versioning;
    using Furly.Extensions.AspNetCore.OpenApi;
    using Furly.Extensions.Http;
    using Microsoft.AspNetCore.Authorization;
    using Microsoft.AspNetCore.Mvc;
    using System;
    using System.ComponentModel.DataAnnotations;
    using System.Linq;
    using System.Threading;
    using System.Threading.Tasks;
    using Furly;
    using Microsoft.AspNetCore.Http;

    /// <summary>
    /// Value and Event publishing services
    /// </summary>
    [ApiVersion("2")]
    [Route("publisher/v{version:apiVersion}/publish")]
    [ExceptionsFilter]
    [Authorize(Policy = Policies.CanRead)]
    [ApiController]
    [Produces(ContentMimeType.Json, ContentMimeType.MsgPack)]
    [Consumes(ContentMimeType.Json, ContentMimeType.MsgPack)]
    public class PublishController : ControllerBase
    {
        /// <summary>
        /// Create controller with service
        /// </summary>
        /// <param name="publisher"></param>
        public PublishController(IPublishServices<string> publisher)
        {
            _publisher = publisher;
        }

        /// <summary>
        /// Start publishing node values
        /// </summary>
        /// <remarks>
        /// Start publishing variable node values to IoT Hub.
        /// The endpoint must be in the registry and the module client
        /// and server must trust each other.
        /// </remarks>
        /// <param name="endpointId">The identifier of the activated endpoint.</param>
        /// <param name="request">The publish request</param>
        /// <param name="ct"></param>
        /// <returns>The publish response</returns>
        /// <exception cref="ArgumentNullException"><paramref name="request"/>
        /// is <c>null</c>.</exception>
        /// <response code="200">The operation was successful.</response>
        /// <response code="400">The passed in information is invalid</response>
        /// <response code="500">An internal error ocurred.</response>
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        [HttpPost("{endpointId}/start")]
        public async Task<PublishStartResponseModel> StartPublishingValuesAsync(
            string endpointId, [FromBody][Required] PublishStartRequestModel request,
            CancellationToken ct)
        {
            ArgumentNullException.ThrowIfNull(request);
            return await _publisher.PublishStartAsync(endpointId, request, ct).ConfigureAwait(false);
        }

        /// <summary>
        /// Bulk publish node values
        /// </summary>
        /// <remarks>
        /// Adds or removes in bulk values that should be published from a particular
        /// endpoint.
        /// </remarks>
        /// <param name="endpointId">The identifier of a registered endpoint.</param>
        /// <param name="request">The bulk publish request</param>
        /// <param name="ct"></param>
        /// <returns>The bulk publish response</returns>
        /// <exception cref="ArgumentNullException"><paramref name="request"/>
        /// is <c>null</c>.</exception>
        /// <response code="200">The operation was successful.</response>
        /// <response code="400">The passed in information is invalid</response>
        /// <response code="500">An internal error ocurred.</response>
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        [HttpPost("{endpointId}/bulk")]
        public async Task<PublishBulkResponseModel> BulkPublishValuesAsync(
            string endpointId, [FromBody][Required] PublishBulkRequestModel request,
            CancellationToken ct)
        {
            ArgumentNullException.ThrowIfNull(request);
            return await _publisher.PublishBulkAsync(endpointId, request, ct).ConfigureAwait(false);
        }

        /// <summary>
        /// Stop publishing node values
        /// </summary>
        /// <remarks>
        /// Stop publishing variable node values to IoT Hub.
        /// The endpoint must be in the registry and the module client
        /// and server must trust each other.
        /// </remarks>
        /// <param name="endpointId">The identifier of the activated endpoint.</param>
        /// <param name="request">The unpublish request</param>
        /// <param name="ct"></param>
        /// <returns>The unpublish response</returns>
        /// <exception cref="ArgumentNullException"><paramref name="request"/>
        /// is <c>null</c>.</exception>
        /// <response code="200">The operation was successful.</response>
        /// <response code="400">The passed in information is invalid</response>
        /// <response code="500">An internal error ocurred.</response>
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        [HttpPost("{endpointId}/stop")]
        public async Task<PublishStopResponseModel> StopPublishingValuesAsync(
            string endpointId, [FromBody][Required] PublishStopRequestModel request,
            CancellationToken ct)
        {
            ArgumentNullException.ThrowIfNull(request);
            return await _publisher.PublishStopAsync(endpointId, request, ct).ConfigureAwait(false);
        }

        /// <summary>
        /// Get currently published nodes
        /// </summary>
        /// <remarks>
        /// Returns currently published node ids for an endpoint.
        /// The endpoint must be in the registry and the module client
        /// and server must trust each other.
        /// </remarks>
        /// <param name="endpointId">The identifier of the activated endpoint.</param>
        /// <param name="request">The list request</param>
        /// <param name="ct"></param>
        /// <returns>The list of published nodes</returns>
        /// <exception cref="ArgumentNullException"><paramref name="request"/>
        /// is <c>null</c>.</exception>
        /// <response code="200">The operation was successful.</response>
        /// <response code="400">The passed in information is invalid</response>
        /// <response code="500">An internal error ocurred.</response>
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        [HttpPost("{endpointId}")]
        public async Task<PublishedItemListResponseModel> GetFirstListOfPublishedNodesAsync(
            string endpointId, [FromBody][Required] PublishedItemListRequestModel request,
            CancellationToken ct)
        {
            ArgumentNullException.ThrowIfNull(request);
            return await _publisher.PublishListAsync(endpointId, request, ct).ConfigureAwait(false);
        }

        /// <summary>
        /// Get next set of published nodes
        /// </summary>
        /// <remarks>
        /// Returns next set of currently published node ids for an endpoint.
        /// The endpoint must be in the registry and the module client
        /// and server must trust each other.
        /// </remarks>
        /// <param name="endpointId">The identifier of the activated endpoint.</param>
        /// <param name="continuationToken">The continuation token to continue with</param>
        /// <param name="ct"></param>
        /// <returns>The list of published nodes</returns>
        /// <response code="200">The operation was successful.</response>
        /// <response code="400">The passed in information is invalid</response>
        /// <response code="500">An internal error ocurred.</response>
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        [HttpGet("{endpointId}")]
        [AutoRestExtension(NextPageLinkName = "continuationToken")]
        public async Task<PublishedItemListResponseModel> GetNextListOfPublishedNodesAsync(
            string endpointId, [FromQuery][Required] string? continuationToken, CancellationToken ct)
        {
            if (Request.Headers.TryGetValue(HttpHeader.ContinuationToken, out var value))
            {
                continuationToken = value.FirstOrDefault();
            }
            return await _publisher.PublishListAsync(endpointId, new PublishedItemListRequestModel
            {
                ContinuationToken = continuationToken
            }, ct).ConfigureAwait(false);
        }

        private readonly IPublishServices<string> _publisher;
    }
}
