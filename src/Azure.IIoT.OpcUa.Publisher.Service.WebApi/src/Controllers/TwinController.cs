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
    /// Node access read services
    /// </summary>
    [ApiVersion("2")]
    [Route("twin/v{version:apiVersion}")]
    [ExceptionsFilter]
    [Authorize(Policy = Policies.CanRead)]
    [ApiController]
    [Produces(ContentMimeType.Json, ContentMimeType.MsgPack)]
    [Consumes(ContentMimeType.Json, ContentMimeType.MsgPack)]
    public class TwinController : ControllerBase
    {
        /// <summary>
        /// Create controller with service
        /// </summary>
        /// <param name="nodes"></param>
        public TwinController(INodeServices<string> nodes)
        {
            _nodes = nodes ?? throw new ArgumentNullException(nameof(nodes));
        }

        /// <summary>
        /// Get the server capabilities
        /// </summary>
        /// <remarks>
        /// Gets the capabilities of the connected server.
        /// The endpoint must be in the registry and the module client
        /// and server must trust each other.
        /// </remarks>
        /// <param name="endpointId">The identifier of the activated endpoint.</param>
        /// <param name="namespaceFormat"></param>
        /// <param name="ct"></param>
        /// <returns>Server capabilities</returns>
        /// <exception cref="ArgumentNullException"></exception>
        /// <response code="200">The operation was successful.</response>
        /// <response code="400">The passed in information is invalid</response>
        /// <response code="500">An internal error ocurred.</response>
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        [HttpGet("capabilities/{endpointId}")]
        public async Task<ServerCapabilitiesModel> GetServerCapabilitiesAsync(
            string endpointId, [FromQuery] NamespaceFormat? namespaceFormat, CancellationToken ct)
        {
            if (string.IsNullOrEmpty(endpointId))
            {
                throw new ArgumentNullException(nameof(endpointId));
            }
            return await _nodes.GetServerCapabilitiesAsync(endpointId,
                new RequestHeaderModel { NamespaceFormat = namespaceFormat }, ct).ConfigureAwait(false);
        }

        /// <summary>
        /// Browse node references
        /// </summary>
        /// <remarks>
        /// Browse a node on the specified endpoint.
        /// The endpoint must be in the registry and the server accessible.
        /// </remarks>
        /// <param name="endpointId">The identifier of the activated endpoint.</param>
        /// <param name="request">The browse request</param>
        /// <param name="ct"></param>
        /// <returns>The browse response</returns>
        /// <exception cref="ArgumentNullException"><paramref name="request"/> is
        /// <c>null</c>.</exception>
        /// <response code="200">The operation was successful.</response>
        /// <response code="400">The passed in information is invalid</response>
        /// <response code="500">An internal error ocurred.</response>
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        [HttpPost("browse/{endpointId}")]
        public async Task<BrowseFirstResponseModel> BrowseAsync(string endpointId,
            [FromBody][Required] BrowseFirstRequestModel request, CancellationToken ct)
        {
            ArgumentNullException.ThrowIfNull(request);
            return await _nodes.BrowseAsync(endpointId, request, ct).ConfigureAwait(false);
        }

        /// <summary>
        /// Browse next set of references
        /// </summary>
        /// <remarks>
        /// Browse next set of references on the endpoint.
        /// The endpoint must be in the registry and the server accessible.
        /// </remarks>
        /// <param name="endpointId">The identifier of the activated endpoint.</param>
        /// <param name="request">The request body with continuation token.</param>
        /// <param name="ct"></param>
        /// <returns>The browse response</returns>
        /// <exception cref="ArgumentNullException"><paramref name="request"/> is
        /// <c>null</c>.</exception>
        /// <exception cref="ArgumentException"></exception>
        /// <response code="200">The operation was successful.</response>
        /// <response code="400">The passed in information is invalid</response>
        /// <response code="500">An internal error ocurred.</response>
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        [HttpPost("browse/{endpointId}/next")]
        public async Task<BrowseNextResponseModel> BrowseNextAsync(
            string endpointId, [FromBody][Required] BrowseNextRequestModel request,
            CancellationToken ct)
        {
            ArgumentNullException.ThrowIfNull(request);
            if (request.ContinuationToken == null)
            {
                throw new ArgumentException("Continuation missing.", nameof(request));
            }
            return await _nodes.BrowseNextAsync(endpointId, request, ct).ConfigureAwait(false);
        }

        /// <summary>
        /// Browse using a browse path
        /// </summary>
        /// <remarks>
        /// Browse using a path from the specified node id.
        /// This call uses TranslateBrowsePathsToNodeIds service under the hood.
        /// The endpoint must be in the registry and the server accessible.
        /// </remarks>
        /// <param name="endpointId">The identifier of the activated endpoint.</param>
        /// <param name="request">The browse path request</param>
        /// <param name="ct"></param>
        /// <returns>The browse path response</returns>
        /// <exception cref="ArgumentNullException"><paramref name="request"/> is
        /// <c>null</c>.</exception>
        /// <response code="200">The operation was successful.</response>
        /// <response code="400">The passed in information is invalid</response>
        /// <response code="500">An internal error ocurred.</response>
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        [HttpPost("browse/{endpointId}/path")]
        public async Task<BrowsePathResponseModel> BrowseUsingPathAsync(string endpointId,
            [FromBody][Required] BrowsePathRequestModel request, CancellationToken ct)
        {
            ArgumentNullException.ThrowIfNull(request);
            return await _nodes.BrowsePathAsync(endpointId, request, ct).ConfigureAwait(false);
        }

        /// <summary>
        /// Browse set of unique target nodes
        /// </summary>
        /// <remarks>
        /// Browse the set of unique hierarchically referenced target nodes on the endpoint.
        /// The endpoint must be in the registry and the server accessible.
        /// The root node id to browse from can be provided as part of the query
        /// parameters.
        /// If it is not provided, the RootFolder node is browsed. Note that this
        /// is the same as the POST method with the model containing the node id
        /// and the targetNodesOnly flag set to true.
        /// </remarks>
        /// <param name="endpointId">The identifier of the activated endpoint.</param>
        /// <param name="nodeId">The node to browse or omit to browse the root node (i=84)
        /// </param>
        /// <param name="ct"></param>
        /// <returns>The browse response</returns>
        /// <response code="200">The operation was successful.</response>
        /// <response code="400">The passed in information is invalid</response>
        /// <response code="500">An internal error ocurred.</response>
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        [HttpGet("browse/{endpointId}")]
        public async Task<BrowseFirstResponseModel> GetSetOfUniqueNodesAsync(
            string endpointId, [FromQuery] string? nodeId, CancellationToken ct)
        {
            if (string.IsNullOrEmpty(nodeId))
            {
                nodeId = null;
            }
            var request = new BrowseFirstRequestModel
            {
                NodeId = nodeId,
                TargetNodesOnly = true,
                ReadVariableValues = true
            };
            return await _nodes.BrowseAsync(endpointId, request, ct: ct).ConfigureAwait(false);
        }

        /// <summary>
        /// Browse next set of unique target nodes
        /// </summary>
        /// <remarks>
        /// Browse the next set of unique hierarchically referenced target nodes on the
        /// endpoint.
        /// The endpoint must be in the registry and the server accessible.
        /// Note that this is the same as the POST method with the model containing
        /// the continuation token and the targetNodesOnly flag set to true.
        /// </remarks>
        /// <param name="endpointId">The identifier of the activated endpoint.</param>
        /// <returns>The browse response</returns>
        /// <param name="continuationToken">Continuation token from GetSetOfUniqueNodes operation
        /// </param>
        /// <param name="ct"></param>
        /// <exception cref="ArgumentNullException"></exception>
        /// <response code="200">The operation was successful.</response>
        /// <response code="400">The passed in information is invalid</response>
        /// <response code="500">An internal error ocurred.</response>
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        [HttpGet("browse/{endpointId}/next")]
        [AutoRestExtension(NextPageLinkName = "continuationToken")]
        public async Task<BrowseNextResponseModel> GetNextSetOfUniqueNodesAsync(
            string endpointId, [FromQuery][Required] string? continuationToken,
            CancellationToken ct)
        {
            if (Request.Headers.TryGetValue(HttpHeader.ContinuationToken, out var value))
            {
                continuationToken = value.FirstOrDefault();
            }
            if (string.IsNullOrEmpty(continuationToken))
            {
                throw new ArgumentNullException(nameof(continuationToken));
            }
            var request = new BrowseNextRequestModel
            {
                ContinuationToken = continuationToken,
                TargetNodesOnly = true,
                ReadVariableValues = true
            };
            return await _nodes.BrowseNextAsync(endpointId, request, ct).ConfigureAwait(false);
        }

        /// <summary>
        /// Read variable value
        /// </summary>
        /// <remarks>
        /// Read a variable node's value.
        /// The endpoint must be in the registry and the server accessible.
        /// </remarks>
        /// <param name="endpointId">The identifier of the activated endpoint.</param>
        /// <param name="request">The read value request</param>
        /// <param name="ct"></param>
        /// <returns>The read value response</returns>
        /// <exception cref="ArgumentNullException"><paramref name="request"/>
        /// is <c>null</c>.</exception>
        /// <response code="200">The operation was successful.</response>
        /// <response code="400">The passed in information is invalid</response>
        /// <response code="500">An internal error ocurred.</response>
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        [HttpPost("read/{endpointId}")]
        public async Task<ValueReadResponseModel> ReadValueAsync(string endpointId,
            [FromBody][Required] ValueReadRequestModel request, CancellationToken ct)
        {
            ArgumentNullException.ThrowIfNull(request);
            return await _nodes.ValueReadAsync(endpointId, request, ct).ConfigureAwait(false);
        }

        /// <summary>
        /// Read node attributes
        /// </summary>
        /// <remarks>
        /// Read attributes of a node.
        /// The endpoint must be in the registry and the server accessible.
        /// </remarks>
        /// <param name="endpointId">The identifier of the activated endpoint.</param>
        /// <param name="request">The read request</param>
        /// <param name="ct"></param>
        /// <returns>The read response</returns>
        /// <exception cref="ArgumentNullException"><paramref name="request"/>
        /// is <c>null</c>.</exception>
        /// <response code="200">The operation was successful.</response>
        /// <response code="400">The passed in information is invalid</response>
        /// <response code="500">An internal error ocurred.</response>
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        [HttpPost("read/{endpointId}/attributes")]
        public async Task<ReadResponseModel> ReadAttributesAsync(string endpointId,
            [FromBody][Required] ReadRequestModel request, CancellationToken ct)
        {
            ArgumentNullException.ThrowIfNull(request);
            return await _nodes.ReadAsync(endpointId, request, ct).ConfigureAwait(false);
        }

        /// <summary>
        /// Get variable value
        /// </summary>
        /// <remarks>
        /// Get a variable node's value using its node id.
        /// The endpoint must be in the registry and the server accessible.
        /// </remarks>
        /// <param name="endpointId">The identifier of the activated endpoint.</param>
        /// <param name="nodeId">The node to read</param>
        /// <param name="ct"></param>
        /// <returns>The read value response</returns>
        /// <exception cref="ArgumentNullException"></exception>
        /// <response code="200">The operation was successful.</response>
        /// <response code="400">The passed in information is invalid</response>
        /// <response code="500">An internal error ocurred.</response>
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        [HttpGet("read/{endpointId}")]
        public async Task<ValueReadResponseModel> GetValueAsync(string endpointId,
            [FromQuery][Required] string nodeId, CancellationToken ct)
        {
            if (string.IsNullOrEmpty(nodeId))
            {
                throw new ArgumentNullException(nameof(nodeId));
            }
            var request = new ValueReadRequestModel { NodeId = nodeId };
            return await _nodes.ValueReadAsync(endpointId, request, ct).ConfigureAwait(false);
        }

        /// <summary>
        /// Write variable value
        /// </summary>
        /// <remarks>
        /// Write variable node's value.
        /// The endpoint must be in the registry and the server accessible.
        /// </remarks>
        /// <param name="endpointId">The identifier of the activated endpoint.</param>
        /// <param name="request">The write value request</param>
        /// <param name="ct"></param>
        /// <returns>The write value response</returns>
        /// <exception cref="ArgumentNullException"><paramref name="request"/>
        /// is <c>null</c>.</exception>
        /// <response code="200">The operation was successful.</response>
        /// <response code="400">The passed in information is invalid</response>
        /// <response code="500">An internal error ocurred.</response>
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        [HttpPost("write/{endpointId}")]
        public async Task<ValueWriteResponseModel> WriteValueAsync(string endpointId,
            [FromBody][Required] ValueWriteRequestModel request, CancellationToken ct)
        {
            ArgumentNullException.ThrowIfNull(request);
            return await _nodes.ValueWriteAsync(endpointId, request, ct).ConfigureAwait(false);
        }

        /// <summary>
        /// Write node attributes
        /// </summary>
        /// <remarks>
        /// Write any attribute of a node.
        /// The endpoint must be in the registry and the server accessible.
        /// </remarks>
        /// <param name="endpointId">The identifier of the activated endpoint.</param>
        /// <param name="request">The batch write request</param>
        /// <param name="ct"></param>
        /// <returns>The batch write response</returns>
        /// <exception cref="ArgumentNullException"><paramref name="request"/>
        /// is <c>null</c>.</exception>
        /// <response code="200">The operation was successful.</response>
        /// <response code="400">The passed in information is invalid</response>
        /// <response code="500">An internal error ocurred.</response>
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        [HttpPost("write/{endpointId}/attributes")]
        public async Task<WriteResponseModel> WriteAttributesAsync(string endpointId,
            [FromBody][Required] WriteRequestModel request, CancellationToken ct)
        {
            ArgumentNullException.ThrowIfNull(request);
            return await _nodes.WriteAsync(endpointId, request, ct).ConfigureAwait(false);
        }

        /// <summary>
        /// Get metadata of a node
        /// </summary>
        /// <remarks>
        /// Get the node metadata which includes the fields
        /// and meta data of the type and can be used when constructing
        /// event filters or calling methods to pass the correct
        /// arguments.
        /// The endpoint must be in the registry and the server accessible.
        /// </remarks>
        /// <param name="endpointId">The identifier of the activated endpoint.</param>
        /// <param name="request">The metadata request</param>
        /// <param name="ct"></param>
        /// <returns>The metadata response</returns>
        /// <exception cref="ArgumentNullException"><paramref name="request"/> is
        /// <c>null</c>.</exception>
        /// <response code="200">The operation was successful.</response>
        /// <response code="400">The passed in information is invalid</response>
        /// <response code="500">An internal error ocurred.</response>
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        [HttpPost("metadata/{endpointId}/node")]
        public async Task<NodeMetadataResponseModel> GetMetadataAsync(
            string endpointId, [FromBody][Required] NodeMetadataRequestModel request,
            CancellationToken ct)
        {
            ArgumentNullException.ThrowIfNull(request);
            return await _nodes.GetMetadataAsync(endpointId, request, ct).ConfigureAwait(false);
        }

        /// <summary>
        /// Get method meta data
        /// </summary>
        /// <remarks>
        /// (Obsolete - use GetMetadata API)
        /// Return method meta data to support a user interface displaying forms to
        /// input and output arguments.
        /// The endpoint must be in the registry and the server accessible.
        /// </remarks>
        /// <param name="endpointId">The identifier of the activated endpoint.</param>
        /// <param name="request">The method metadata request</param>
        /// <param name="ct"></param>
        /// <returns>The method metadata response</returns>
        /// <exception cref="ArgumentNullException"><paramref name="request"/>
        /// is <c>null</c>.</exception>
        /// <response code="200">The operation was successful.</response>
        /// <response code="400">The passed in information is invalid</response>
        /// <response code="500">An internal error ocurred.</response>
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        [HttpPost("call/{endpointId}/metadata")]
        public async Task<MethodMetadataResponseModel> GetCallMetadataAsync(
            string endpointId, [FromBody][Required] MethodMetadataRequestModel request,
            CancellationToken ct)
        {
            ArgumentNullException.ThrowIfNull(request);
            return await _nodes.GetMethodMetadataAsync(endpointId, request, ct).ConfigureAwait(false);
        }

        /// <summary>
        /// Call a method
        /// </summary>
        /// <remarks>
        /// Invoke method node with specified input arguments.
        /// The endpoint must be in the registry and the server accessible.
        /// </remarks>
        /// <param name="endpointId">The identifier of the activated endpoint.</param>
        /// <param name="request">The method call request</param>
        /// <param name="ct"></param>
        /// <returns>The method call response</returns>
        /// <exception cref="ArgumentNullException"><paramref name="request"/>
        /// is <c>null</c>.</exception>
        /// <response code="200">The operation was successful.</response>
        /// <response code="400">The passed in information is invalid</response>
        /// <response code="500">An internal error ocurred.</response>
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        [HttpPost("call/{endpointId}")]
        public async Task<MethodCallResponseModel> CallMethodAsync(string endpointId,
            [FromBody][Required] MethodCallRequestModel request, CancellationToken ct)
        {
            ArgumentNullException.ThrowIfNull(request);

            // TODO: Permissions

            return await _nodes.MethodCallAsync(endpointId, request, ct).ConfigureAwait(false);
        }

        private readonly INodeServices<string> _nodes;
    }
}
