// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Azure.IIoT.OpcUa.Publisher.Module.Controllers
{
    using Azure.IIoT.OpcUa.Publisher.Module.Filters;
    using Azure.IIoT.OpcUa.Publisher.Models;
    using Asp.Versioning;
    using Furly;
    using Furly.Tunnel.Router;
    using Microsoft.AspNetCore.Authorization;
    using Microsoft.AspNetCore.Http;
    using Microsoft.AspNetCore.Mvc;
    using System;
    using System.Collections.Generic;
    using System.ComponentModel.DataAnnotations;
    using System.Linq;
    using System.Threading;
    using System.Threading.Tasks;

    /// <summary>
    /// <para>
    /// This section contains the API to configure OPC Publisher.
    /// </para>
    /// <para>
    /// The method name for all transports other than HTTP (which uses the shown
    /// HTTP methods and resource uris) is the name of the subsection header.
    /// To use the version specific method append "_V1" or "_V2" to the method
    /// name.
    /// </para>
    /// </summary>
    [Version("_V2")]
    [Version("_V1")]
    [Version("")]
    [RouterExceptionFilter]
    [ControllerExceptionFilter]
    [ApiVersion("2")]
    [Route("v{version:apiVersion}/configuration")]
    [ApiController]
    [Authorize]
    [Produces(ContentMimeType.Json, ContentMimeType.MsgPack)]
    [Consumes(ContentMimeType.Json, ContentMimeType.MsgPack)]
    public class ConfigurationController : ControllerBase, IMethodController
    {
        /// <summary>
        /// Create publisher methods controller
        /// </summary>
        /// <param name="configServices"></param>
        public ConfigurationController(IConfigurationServices configServices)
        {
            _configServices = configServices;
        }

        /// <summary>
        /// PublishStart
        /// </summary>
        /// <remarks>
        /// Start publishing values from a node on a server. The group field in the
        /// Connection Model can be used to specify a writer group identifier that will
        /// be used in the configuration entry that is created from it inside OPC Publisher.
        /// </remarks>
        /// <param name="request">The server and node to publish.</param>
        /// <returns>The results of the operation.</returns>
        /// <exception cref="ArgumentNullException"><paramref name="request"/>
        /// is <c>null</c>.</exception>
        /// <response code="200">The operation was successful.</response>
        /// <response code="400">The passed in information is invalid</response>
        /// <response code="500">An unexpected error occurred</response>
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status500InternalServerError)]
        [HttpPost("start")]
        public async Task<PublishStartResponseModel> PublishStartAsync(
            [FromBody][Required] RequestEnvelope<PublishStartRequestModel> request)
        {
            ArgumentNullException.ThrowIfNull(request);
            ArgumentNullException.ThrowIfNull(request.Connection);
            ArgumentNullException.ThrowIfNull(request.Request);
            return await _configServices.PublishStartAsync(request.Connection,
                request.Request).ConfigureAwait(false);
        }

        /// <summary>
        /// PublishStop
        /// </summary>
        /// <remarks>
        /// Stop publishing values from a node on the specified server. The group field
        /// that was used in the Connection Model to start publishing must also be
        /// specified in this connection model.
        /// </remarks>
        /// <param name="request">The node to stop publishing</param>
        /// <returns>The result of the stop operation.</returns>
        /// <exception cref="ArgumentNullException"><paramref name="request"/>
        /// is <c>null</c>.</exception>
        /// <response code="200">The operation was successful.</response>
        /// <response code="404">The item could not be unpublished</response>
        /// <response code="408">The operation timed out.</response>
        /// <response code="500">An unexpected error occurred</response>
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status408RequestTimeout)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status500InternalServerError)]
        [HttpPost("stop")]
        public async Task<PublishStopResponseModel> PublishStopAsync(
            [FromBody][Required] RequestEnvelope<PublishStopRequestModel> request)
        {
            ArgumentNullException.ThrowIfNull(request);
            ArgumentNullException.ThrowIfNull(request.Connection);
            ArgumentNullException.ThrowIfNull(request.Request);
            return await _configServices.PublishStopAsync(request.Connection,
                request.Request).ConfigureAwait(false);
        }

        /// <summary>
        /// PublishBulk
        /// </summary>
        /// <remarks>
        /// Configure node values to publish and unpublish in bulk. The group field in
        /// the Connection Model can be used to specify a writer group identifier that
        /// will be used in the configuration entry that is created from it inside OPC
        /// Publisher.
        /// </remarks>
        /// <param name="request">The nodes to publish or unpublish.</param>
        /// <returns>The result for each operation.</returns>
        /// <exception cref="ArgumentNullException"><paramref name="request"/>
        /// is <c>null</c>.</exception>
        /// <response code="200">The operation was successful.</response>
        /// <response code="404">The item could not be unpublished</response>
        /// <response code="408">The operation timed out.</response>
        /// <response code="500">An unexpected error occurred</response>
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status408RequestTimeout)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status500InternalServerError)]
        [HttpPost("bulk")]
        public async Task<PublishBulkResponseModel> PublishBulkAsync(
            [FromBody][Required] RequestEnvelope<PublishBulkRequestModel> request)
        {
            ArgumentNullException.ThrowIfNull(request);
            ArgumentNullException.ThrowIfNull(request.Connection);
            ArgumentNullException.ThrowIfNull(request.Request);
            return await _configServices.PublishBulkAsync(request.Connection,
                request.Request).ConfigureAwait(false);
        }

        /// <summary>
        /// PublishList
        /// </summary>
        /// <remarks>
        /// Get all published nodes for a server endpoint.
        /// The group field that was used in the Connection Model to start
        /// publishing must also be specified in this connection model.
        /// </remarks>
        /// <param name="request"></param>
        /// <returns>The list of published nodes.</returns>
        /// <exception cref="ArgumentNullException"><paramref name="request"/>
        /// is <c>null</c>.</exception>
        /// <response code="200">The items were found and returned.</response>
        /// <response code="408">The operation timed out.</response>
        /// <response code="500">An unexpected error occurred</response>
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status408RequestTimeout)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status500InternalServerError)]
        [HttpPost("list")]
        public async Task<PublishedItemListResponseModel> PublishListAsync(
            [FromBody][Required] RequestEnvelope<PublishedItemListRequestModel> request)
        {
            ArgumentNullException.ThrowIfNull(request);
            ArgumentNullException.ThrowIfNull(request.Connection);
            ArgumentNullException.ThrowIfNull(request.Request);
            return await _configServices.PublishListAsync(request.Connection,
                request.Request).ConfigureAwait(false);
        }

        /// <summary>
        /// [PublishNodes](./directmethods.md#publishnodes_v1)
        /// </summary>
        /// <remarks>
        /// PublishNodes enables a client to add a set of nodes to be published.
        /// Further information is provided in the OPC Publisher documentation.
        /// </remarks>
        /// <param name="request">The request contains the nodes to publish.</param>
        /// <param name="ct"></param>
        /// <returns>The result of the operation.</returns>
        /// <exception cref="ArgumentNullException"><paramref name="request"/>
        /// is <c>null</c>.</exception>
        /// <response code="200">The operation was successful.</response>
        /// <response code="408">The operation timed out.</response>
        /// <response code="500">An unexpected error occurred</response>
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status408RequestTimeout)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status500InternalServerError)]
        [HttpPost("nodes")]
        public async Task<PublishedNodesResponseModel> PublishNodesAsync(
            [FromBody][Required] PublishedNodesEntryModel request, CancellationToken ct = default)
        {
            ArgumentNullException.ThrowIfNull(request);
            await _configServices.PublishNodesAsync(request, ct).ConfigureAwait(false);
            return new PublishedNodesResponseModel();
        }

        /// <summary>
        /// [UnpublishNodes](./directmethods.md#unpublishnodes_v1)
        /// </summary>
        /// <remarks>
        /// UnpublishNodes method enables a client to remove nodes from a previously
        /// configured DataSetWriter. Further information is provided in the
        /// OPC Publisher documentation.
        /// </remarks>
        /// <param name="request">The request payload specifying the nodes to
        /// unpublish.</param>
        /// <param name="ct"></param>
        /// <returns>The result of the operation.</returns>
        /// <exception cref="ArgumentNullException"><paramref name="request"/>
        /// is <c>null</c>.</exception>
        /// <response code="200">The operation was successful.</response>
        /// <response code="404">The nodes could not be unpublished</response>
        /// <response code="408">The operation timed out.</response>
        /// <response code="500">An unexpected error occurred</response>
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status408RequestTimeout)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status500InternalServerError)]
        [HttpPost("nodes/unpublish")]
        public async Task<PublishedNodesResponseModel> UnpublishNodesAsync(
            [FromBody][Required] PublishedNodesEntryModel request, CancellationToken ct = default)
        {
            ArgumentNullException.ThrowIfNull(request);
            await _configServices.UnpublishNodesAsync(request, ct).ConfigureAwait(false);
            return new PublishedNodesResponseModel();
        }

        /// <summary>
        /// [UnpublishAllNodes](./directmethods.md#unpublishallnodes_v1)
        /// </summary>
        /// <remarks>
        /// Unpublish all specified nodes or all nodes in the publisher
        /// configuration.  Further information is provided in the
        /// OPC Publisher documentation.
        /// </remarks>
        /// <param name="request">The request contains the parts of the
        /// configuration to remove.</param>
        /// <param name="ct"></param>
        /// <returns>The result of the operation.</returns>
        /// <exception cref="ArgumentNullException"><paramref name="request"/>
        /// is <c>null</c>.</exception>
        /// <response code="200">The operation was successful.</response>
        /// <response code="404">The nodes could not be unpublished</response>
        /// <response code="408">The operation timed out.</response>
        /// <response code="500">An unexpected error occurred</response>
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status408RequestTimeout)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status500InternalServerError)]
        [HttpPost("nodes/unpublish/all")]
        public async Task<PublishedNodesResponseModel> UnpublishAllNodesAsync(
            [FromBody][Required] PublishedNodesEntryModel request, CancellationToken ct = default)
        {
            ArgumentNullException.ThrowIfNull(request);
            await _configServices.UnpublishAllNodesAsync(request, ct).ConfigureAwait(false);
            return new PublishedNodesResponseModel();
        }

        /// <summary>
        /// [AddOrUpdateEndpoints](./directmethods.md#addorupdateendpoints_v1)
        /// </summary>
        /// <remarks>
        /// Add or update endpoint configuration and nodes on a server.
        /// Further information is provided in the OPC Publisher documentation.
        /// </remarks>
        /// <param name="request">The parts of the configuration to add or
        /// update.</param>
        /// <param name="ct"></param>
        /// <returns>The result of the operation.</returns>
        /// <exception cref="ArgumentNullException"><paramref name="request"/>
        /// is <c>null</c>.</exception>
        /// <response code="200">The operation was successful.</response>
        /// <response code="404">The endpoint was not found to add to</response>
        /// <response code="408">The operation timed out.</response>
        /// <response code="500">An unexpected error occurred</response>
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status408RequestTimeout)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status500InternalServerError)]
        [HttpPatch]
        public async Task<PublishedNodesResponseModel> AddOrUpdateEndpointsAsync(
            [FromBody][Required] IReadOnlyList<PublishedNodesEntryModel> request,
            CancellationToken ct = default)
        {
            ArgumentNullException.ThrowIfNull(request);
            await _configServices.AddOrUpdateEndpointsAsync(request, ct).ConfigureAwait(false);
            return new PublishedNodesResponseModel();
        }

        /// <summary>
        /// [GetConfiguredEndpoints](./directmethods.md#getconfiguredendpoints_v1)
        /// </summary>
        /// <remarks>
        /// Get a list of nodes under a configured endpoint in the configuration.
        /// Further information is provided in the OPC Publisher documentation.
        /// configuration.
        /// </remarks>
        /// <param name="request">Optional but can be spcified to include
        /// the nodes in the response.</param>
        /// <param name="ct"></param>
        /// <returns>The result of the operation.</returns>
        /// <response code="200">The data was retrieved.</response>
        /// <response code="400">The passed in information is invalid</response>
        /// <response code="408">The operation timed out.</response>
        /// <response code="500">An unexpected error occurred</response>
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status408RequestTimeout)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status500InternalServerError)]
        [HttpGet]
        public async Task<GetConfiguredEndpointsResponseModel> GetConfiguredEndpointsAsync(
            [FromQuery] GetConfiguredEndpointsRequestModel? request = null,
            CancellationToken ct = default)
        {
            var response = await _configServices.GetConfiguredEndpointsAsync(
                request?.IncludeNodes ?? false, ct).ConfigureAwait(false);
            return new GetConfiguredEndpointsResponseModel
            {
                Endpoints = response
            };
        }

        /// <summary>
        /// [SetConfiguredEndpoints](./directmethods.md#setconfiguredendpoints_v1)
        /// </summary>
        /// <remarks>
        /// Enables clients to update the entire published nodes configuration
        /// in one call. This includes clearing the existing configuration.
        /// Further information is provided in the OPC Publisher documentation.
        /// configuration.
        /// </remarks>
        /// <param name="request">The new published nodes configuration</param>
        /// <param name="ct"></param>
        /// <returns>The result of the operation.</returns>
        /// <exception cref="ArgumentNullException"><paramref name="request"/>
        /// is <c>null</c>.</exception>
        /// <response code="200">The operation was successful.</response>
        /// <response code="400">The passed in information is invalid</response>
        /// <response code="408">The operation timed out.</response>
        /// <response code="500">An unexpected error occurred</response>
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status408RequestTimeout)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status500InternalServerError)]
        [HttpPut]
        public async Task SetConfiguredEndpointsAsync(
            [FromBody][Required] SetConfiguredEndpointsRequestModel request,
            CancellationToken ct = default)
        {
            ArgumentNullException.ThrowIfNull(request);
            await _configServices.SetConfiguredEndpointsAsync(new List<PublishedNodesEntryModel>(
                request.Endpoints ?? Enumerable.Empty<PublishedNodesEntryModel>()),
                ct).ConfigureAwait(false);
        }

        /// <summary>
        /// [GetConfiguredNodesOnEndpoint](./directmethods.md#getconfigurednodesonendpoint_v)
        /// </summary>
        /// <remarks>
        /// Get the nodes of a published nodes entry object returned earlier from
        /// a call to GetConfiguredEndpoints. Further information is provided in
        /// the OPC Publisher documentation.
        /// </remarks>
        /// <param name="request">The entry model from a call to GetConfiguredEndpoints
        /// for which to gather the nodes.</param>
        /// <param name="ct"></param>
        /// <returns>The result of the operation.</returns>
        /// <exception cref="ArgumentNullException"><paramref name="request"/>
        /// is <c>null</c>.</exception>
        /// <response code="200">The information was returned.</response>
        /// <response code="400">The passed in information is invalid</response>
        /// <response code="404">The entry was not found.</response>
        /// <response code="408">The operation timed out.</response>
        /// <response code="500">An unexpected error occurred</response>
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status408RequestTimeout)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status500InternalServerError)]
        [HttpPost("endpoints/list/nodes")]
        public async Task<GetConfiguredNodesOnEndpointResponseModel> GetConfiguredNodesOnEndpointAsync(
            [FromBody][Required] PublishedNodesEntryModel request, CancellationToken ct = default)
        {
            ArgumentNullException.ThrowIfNull(request);
            var response = await _configServices.GetConfiguredNodesOnEndpointAsync(
                request, ct).ConfigureAwait(false);
            return new GetConfiguredNodesOnEndpointResponseModel
            {
                OpcNodes = response
            };
        }

        /// <summary>
        /// [GetDiagnosticInfo](./directmethods.md#getdiagnosticinfo_v1)
        /// </summary>
        /// <remarks>
        /// Get the list of diagnostics info for all dataset writers in the OPC Publisher
        /// at the point the call is received. Further information is provided in the
        /// OPC Publisher documentation.
        /// </remarks>
        /// <param name="ct"></param>
        /// <returns>The list of diagnostic infos for currently active writers.</returns>
        /// <response code="200">The operation was successful.</response>
        /// <response code="405">Call not supported or functionality disabled.</response>
        /// <response code="408">The operation timed out.</response>
        /// <response code="500">An unexpected error occurred</response>
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status405MethodNotAllowed)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status408RequestTimeout)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status500InternalServerError)]
        [HttpPost("diagnostics")]
        public async Task<List<PublishDiagnosticInfoModel>> GetDiagnosticInfoAsync(
            CancellationToken ct = default)
        {
            return await _configServices.GetDiagnosticInfoAsync(ct).ConfigureAwait(false);
        }

        private readonly IConfigurationServices _configServices;
    }
}
