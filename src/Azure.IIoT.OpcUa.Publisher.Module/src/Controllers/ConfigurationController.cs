// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Azure.IIoT.OpcUa.Publisher.Module.Controllers
{
    using Azure.IIoT.OpcUa.Publisher.Module.Filters;
    using Azure.IIoT.OpcUa.Publisher.Models;
    using Asp.Versioning;
    using Furly.Tunnel.Router;
    using Microsoft.AspNetCore.Authorization;
    using Microsoft.AspNetCore.Mvc;
    using System;
    using System.Collections.Generic;
    using System.ComponentModel.DataAnnotations;
    using System.Linq;
    using System.Threading;
    using System.Threading.Tasks;
    using Furly.Extensions.Http;
    using System.Globalization;
    using Furly.Extensions.AspNetCore.OpenApi;

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
    public class ConfigurationController : ControllerBase, IMethodController
    {
        /// <summary>
        /// Create publisher methods controller
        /// </summary>
        /// <param name="configServices"></param>
        public ConfigurationController(IConfigurationServices configServices)
        {
            _configServices = configServices ??
                throw new ArgumentNullException(nameof(configServices));
        }

        /// <summary>
        /// CreateOrUpdateDataSetWriterEntry
        /// </summary>
        /// <remarks>
        /// Create a published nodes entry for a specific writer group and dataset writer.
        /// The entry must specify a unique writer group and dataset writer id. If the
        /// entry is found it is updated, if it is not found, it is created. If more than
        /// one entry is found an error is returned. The entry can include nodes which
        /// will be the initial set. The entries must all specify a unique dataSetFieldId.
        /// </remarks>
        /// <param name="dataSetWriterEntry">The entry to create for the writer</param>
        /// <param name="ct"></param>
        /// <exception cref="ArgumentNullException"><paramref name="dataSetWriterEntry"/>
        /// is <c>null</c>.</exception>
        [HttpPut("writer")]
        public async Task CreateOrUpdateDataSetWriterEntryAsync(
            [FromBody][Required] PublishedNodesEntryModel dataSetWriterEntry,
            CancellationToken ct = default)
        {
            ArgumentNullException.ThrowIfNull(dataSetWriterEntry);
            await _configServices.CreateOrUpdateDataSetWriterEntryAsync(
                dataSetWriterEntry, ct).ConfigureAwait(false);
        }

        /// <summary>
        /// GetDataSetWriterEntry
        /// </summary>
        /// <remarks>
        /// Get the published nodes entry for a specific writer group and dataset writer.
        /// Dedicated errors are returned if no, or no unique entry could be found. The
        /// entry does not contain the nodes
        /// </remarks>
        /// <param name="dataSetWriterGroup">The writer group name of the entry</param>
        /// <param name="dataSetWriterId">The data set writer identifer of the entry</param>
        /// <param name="ct"></param>
        /// <returns>The entry selected without nodes</returns>
        /// <exception cref="ArgumentNullException"><paramref name="dataSetWriterGroup"/>
        /// is <c>null</c>.</exception>
        /// <exception cref="ArgumentNullException"><paramref name="dataSetWriterId"/>
        /// is <c>null</c>.</exception>
        [HttpGet("writer/{dataSetWriterGroup}/{dataSetWriterId}")]
        public async Task<PublishedNodesEntryModel> GetDataSetWriterEntryAsync(
            string dataSetWriterGroup, string dataSetWriterId, CancellationToken ct = default)
        {
            ArgumentNullException.ThrowIfNull(dataSetWriterGroup);
            ArgumentNullException.ThrowIfNull(dataSetWriterId);
            return await _configServices.GetDataSetWriterEntryAsync(
                dataSetWriterGroup, dataSetWriterId, ct).ConfigureAwait(false);
        }

        /// <summary>
        /// AddOrUpdateNodes
        /// </summary>
        /// <remarks>
        /// Add Nodes to a dedicated data set writer in a writer group. Each node must have
        /// a unique DataSetFieldId. If the field already exists, the node is updated.
        /// If a node does not have a dataset field id an error is returned.
        /// </remarks>
        /// <param name="dataSetWriterGroup">The writer group name of the entry</param>
        /// <param name="dataSetWriterId">The data set writer identifer of the entry</param>
        /// <param name="opcNodes">Nodes to add or update</param>
        /// <param name="insertAfterFieldId">Field after which to insert the nodes. If
        /// not specified, nodes are added at the end of the entry</param>
        /// <param name="ct"></param>
        /// <exception cref="ArgumentNullException"><paramref name="dataSetWriterGroup"/>
        /// is <c>null</c>.</exception>
        /// <exception cref="ArgumentNullException"><paramref name="dataSetWriterId"/>
        /// is <c>null</c>.</exception>
        /// <exception cref="ArgumentNullException"><paramref name="opcNodes"/>
        /// is <c>null</c>.</exception>
        [HttpPut("writer/{dataSetWriterGroup}/{dataSetWriterId}/nodes")]
        public async Task AddOrUpdateNodesAsync(string dataSetWriterGroup, string dataSetWriterId,
            [FromBody][Required] IReadOnlyList<OpcNodeModel> opcNodes,
            [FromQuery] string? insertAfterFieldId = null, CancellationToken ct = default)
        {
            ArgumentNullException.ThrowIfNull(dataSetWriterGroup);
            ArgumentNullException.ThrowIfNull(dataSetWriterId);
            ArgumentNullException.ThrowIfNull(opcNodes);
            await _configServices.AddOrUpdateNodesAsync(dataSetWriterGroup, dataSetWriterId,
                opcNodes, insertAfterFieldId, ct).ConfigureAwait(false);
        }

        /// <summary>
        /// RemoveNodes
        /// </summary>
        /// <remarks>
        /// Remove Nodes with the data set field ids from a data set writer in a writer group.
        /// If the field is not found, no error is returned.
        /// </remarks>
        /// <param name="dataSetWriterGroup">The writer group name of the entry</param>
        /// <param name="dataSetWriterId">The data set writer identifer of the entry</param>
        /// <param name="dataSetFieldIds">Fields to add</param>
        /// <param name="ct"></param>
        /// <exception cref="ArgumentNullException"><paramref name="dataSetWriterGroup"/>
        /// is <c>null</c>.</exception>
        /// <exception cref="ArgumentNullException"><paramref name="dataSetWriterId"/>
        /// is <c>null</c>.</exception>
        /// <exception cref="ArgumentNullException"><paramref name="dataSetFieldIds"/>
        /// is <c>null</c>.</exception>
        [HttpDelete("writer/{dataSetWriterGroup}/{dataSetWriterId}/nodes")]
        public async Task RemoveNodesAsync(string dataSetWriterGroup, string dataSetWriterId,
            [FromBody][Required] IReadOnlyList<string> dataSetFieldIds,
            CancellationToken ct = default)
        {
            ArgumentNullException.ThrowIfNull(dataSetWriterGroup);
            ArgumentNullException.ThrowIfNull(dataSetWriterId);
            ArgumentNullException.ThrowIfNull(dataSetFieldIds);
            await _configServices.RemoveNodesAsync(dataSetWriterGroup, dataSetWriterId,
                dataSetFieldIds, ct).ConfigureAwait(false);
        }

        /// <summary>
        /// GetNodes
        /// </summary>
        /// <remarks>
        /// Remove Nodes with the data set field ids from a data set writer in a writer group.
        /// If the field is not found, no error is returned.
        /// </remarks>
        /// <param name="dataSetWriterGroup">The writer group name of the entry</param>
        /// <param name="dataSetWriterId">The data set writer identifer of the entry</param>
        /// <param name="dataSetFieldId">the field id from which to start. If not specified,
        /// nodes from the beginning are returned.</param>
        /// <param name="pageSize">Number of nodes to return</param>
        /// <param name="ct"></param>
        /// <returns>List of nodes or none if none were found</returns>
        /// <exception cref="ArgumentNullException"><paramref name="dataSetWriterGroup"/>
        /// is <c>null</c>.</exception>
        /// <exception cref="ArgumentNullException"><paramref name="dataSetWriterId"/>
        /// is <c>null</c>.</exception>
        [HttpGet("writer/{dataSetWriterGroup}/{dataSetWriterId}/nodes")]
        [AutoRestExtension(NextPageLinkName = "dataSetFieldId")]
        public async Task<IReadOnlyList<OpcNodeModel>> GetNodesAsync(
            string dataSetWriterGroup, string dataSetWriterId,
            [FromQuery] string? dataSetFieldId = null,
            [FromQuery] int? pageSize = null, CancellationToken ct = default)
        {
            ArgumentNullException.ThrowIfNull(dataSetWriterGroup);
            ArgumentNullException.ThrowIfNull(dataSetWriterId);
            if (Request.Headers.TryGetValue(HttpHeader.ContinuationToken, out var value))
            {
                dataSetFieldId = value.FirstOrDefault();
            }
            if (Request.Headers.TryGetValue(HttpHeader.MaxItemCount, out value))
            {
                pageSize = int.Parse(value.FirstOrDefault()!,
                    CultureInfo.InvariantCulture);
            }
            return await _configServices.GetNodesAsync(dataSetWriterGroup, dataSetWriterId,
                dataSetFieldId, pageSize, ct).ConfigureAwait(false);
        }

        /// <summary>
        /// RemoveDataSetWriterEntry
        /// </summary>
        /// <remarks>
        /// Remove the published nodes entry for a specific data set writer in a writer
        /// group. Dedicated errors are returned if no, or no unique entry could be found.
        /// </remarks>
        /// <param name="dataSetWriterGroup">The writer group name of the entry</param>
        /// <param name="dataSetWriterId">The data set writer identifer of the entry</param>
        /// <param name="ct"></param>
        /// <exception cref="ArgumentNullException"><paramref name="dataSetWriterGroup"/>
        /// is <c>null</c>.</exception>
        /// <exception cref="ArgumentNullException"><paramref name="dataSetWriterId"/>
        /// is <c>null</c>.</exception>
        [HttpDelete("writer/{dataSetWriterGroup}/{dataSetWriterId}")]
        public async Task RemoveDataSetWriterEntryAsync(
            string dataSetWriterGroup, string dataSetWriterId, CancellationToken ct = default)
        {
            ArgumentNullException.ThrowIfNull(dataSetWriterGroup);
            ArgumentNullException.ThrowIfNull(dataSetWriterId);
            await _configServices.RemoveDataSetWriterEntryAsync(dataSetWriterGroup,
                dataSetWriterId, ct).ConfigureAwait(false);
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
        [HttpPost("diagnostics")]
        public async Task<List<PublishDiagnosticInfoModel>> GetDiagnosticInfoAsync(
            CancellationToken ct = default)
        {
            return await _configServices.GetDiagnosticInfoAsync(ct).ConfigureAwait(false);
        }

        private readonly IConfigurationServices _configServices;
    }
}
