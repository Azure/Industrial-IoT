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
    using Furly.Extensions.AspNetCore.OpenApi;
    using Furly.Extensions.Http;
    using Furly.Tunnel.Router;
    using Microsoft.AspNetCore.Authorization;
    using Microsoft.AspNetCore.Http;
    using Microsoft.AspNetCore.Mvc;
    using System;
    using System.Collections.Generic;
    using System.ComponentModel.DataAnnotations;
    using System.Globalization;
    using System.Linq;
    using System.Threading;
    using System.Threading.Tasks;

    /// <summary>
    /// <para>
    /// This section contains the API to configure data set writers and writer
    /// groups inside OPC Publisher. It supersedes the configuration API.
    /// Applications should use one or the other, but not both at the same
    /// time.
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
    [Route("v{version:apiVersion}/writer")]
    [ApiController]
    [Authorize]
    [Produces(ContentMimeType.Json, ContentMimeType.MsgPack)]
    [Consumes(ContentMimeType.Json, ContentMimeType.MsgPack)]
    public class WriterController : ControllerBase, IMethodController
    {
        /// <summary>
        /// Create writer configuration methods controller
        /// </summary>
        /// <param name="configServices"></param>
        public WriterController(IConfigurationServices configServices)
        {
            _configServices = configServices;
        }

        /// <summary>
        /// CreateOrUpdateDataSetWriterEntry
        /// </summary>
        /// <remarks>
        /// Create a published nodes entry for a specific writer group and dataset writer.
        /// The entry must specify a unique writer group and dataset writer id. A null value
        /// is treated as empty string. If the entry is found it is updated, if it is not
        /// found, it is created. If more than one entry is found with the same writer group
        /// and writer id an error is returned. The writer entry provided must include at
        /// least one node which will be the initial set. All nodes must specify a unique
        /// dataSetFieldId. A null value is treated as empty string. Publishing intervals
        /// at node level are also not supported and generate an error. Publishing
        /// intervals must be configured at the data set writer level.
        /// </remarks>
        /// <param name="dataSetWriterEntry">The entry to create for the writer</param>
        /// <param name="ct"></param>
        /// <exception cref="ArgumentNullException"><paramref name="dataSetWriterEntry"/>
        /// is <c>null</c>.</exception>
        /// <response code="200">The item was created</response>
        /// <response code="400">The passed in information is invalid</response>
        /// <response code="403">A unique item could not be found to update.</response>
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
        [HttpPut]
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
        /// entry does not contain the nodes. Nodes can be retrieved using the GetNodes
        /// API.
        /// </remarks>
        /// <param name="dataSetWriterGroup">The writer group name of the entry</param>
        /// <param name="dataSetWriterId">The data set writer identifer of the entry</param>
        /// <param name="ct"></param>
        /// <returns>The entry selected without nodes</returns>
        /// <exception cref="ArgumentNullException"><paramref name="dataSetWriterGroup"/>
        /// is <c>null</c>.</exception>
        /// <exception cref="ArgumentNullException"><paramref name="dataSetWriterId"/>
        /// is <c>null</c>.</exception>
        /// <response code="200">The item was found</response>
        /// <response code="400">The passed in information is invalid</response>
        /// <response code="403">There is no unique item present.</response>
        /// <response code="404">The item was not found</response>
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
        [HttpGet("{dataSetWriterGroup}/{dataSetWriterId}")]
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
        /// If a node does not have a dataset field id an error is returned. Publishing
        /// intervals at node level are also not supported and generate an error. Publishing
        /// intervals must be configured at the data set writer level.
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
        /// <response code="200">The items were added</response>
        /// <response code="400">The passed in information is invalid</response>
        /// <response code="403">A unique entry could not be found to add to.</response>
        /// <response code="404">The entry was not found</response>
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
        [HttpPost("{dataSetWriterGroup}/{dataSetWriterId}/add")]
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
        /// AddOrUpdateNode
        /// </summary>
        /// <remarks>
        /// Add a node to a dedicated data set writer in a writer group. A node must have
        /// a unique DataSetFieldId. If the field already exists, the node is updated.
        /// If a node does not have a dataset field id an error is returned. Publishing
        /// intervals at node level are also not supported and generate an error. Publishing
        /// intervals must be configured at the data set writer level.
        /// </remarks>
        /// <param name="dataSetWriterGroup">The writer group name of the entry</param>
        /// <param name="dataSetWriterId">The data set writer identifer of the entry</param>
        /// <param name="opcNode">Node to add or update</param>
        /// <param name="insertAfterFieldId">Field after which to insert the nodes. If
        /// not specified, nodes are added at the end of the entry</param>
        /// <param name="ct"></param>
        /// <exception cref="ArgumentNullException"><paramref name="dataSetWriterGroup"/>
        /// is <c>null</c>.</exception>
        /// <exception cref="ArgumentNullException"><paramref name="dataSetWriterId"/>
        /// is <c>null</c>.</exception>
        /// <exception cref="ArgumentNullException"><paramref name="opcNode"/>
        /// is <c>null</c>.</exception>
        /// <response code="200">The item was added</response>
        /// <response code="400">The passed in information is invalid</response>
        /// <response code="403">A unique item could not be found to update.</response>
        /// <response code="404">An entry was not found to add the node to</response>
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
        [HttpPut("{dataSetWriterGroup}/{dataSetWriterId}")]
        public async Task AddOrUpdateNodeAsync(string dataSetWriterGroup, string dataSetWriterId,
            [FromBody][Required] OpcNodeModel opcNode,
            [FromQuery] string? insertAfterFieldId = null, CancellationToken ct = default)
        {
            ArgumentNullException.ThrowIfNull(dataSetWriterGroup);
            ArgumentNullException.ThrowIfNull(dataSetWriterId);
            ArgumentNullException.ThrowIfNull(opcNode);
            await _configServices.AddOrUpdateNodesAsync(dataSetWriterGroup, dataSetWriterId,
                new[] { opcNode }, insertAfterFieldId, ct).ConfigureAwait(false);
        }

        /// <summary>
        /// RemoveNodes
        /// </summary>
        /// <remarks>
        /// Remove Nodes that match the provided data set field ids from a data set writer
        /// in a writer group. If one of the fields is not found, no error is returned,
        /// however, if all fields are not found an error is returned.
        /// </remarks>
        /// <param name="dataSetWriterGroup">The writer group name of the entry</param>
        /// <param name="dataSetWriterId">The data set writer identifer of the entry</param>
        /// <param name="dataSetFieldIds">The identifiers of the fields to remove</param>
        /// <param name="ct"></param>
        /// <exception cref="ArgumentNullException"><paramref name="dataSetWriterGroup"/>
        /// is <c>null</c>.</exception>
        /// <exception cref="ArgumentNullException"><paramref name="dataSetWriterId"/>
        /// is <c>null</c>.</exception>
        /// <exception cref="ArgumentNullException"><paramref name="dataSetFieldIds"/>
        /// is <c>null</c>.</exception>
        /// <response code="200">Some or all items were removed</response>
        /// <response code="400">The passed in information is invalid</response>
        /// <response code="403">A unique item could not be found to remove from.</response>
        /// <response code="404">The entry or all items to remove were not found</response>
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
        [HttpPost("{dataSetWriterGroup}/{dataSetWriterId}/remove")]
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
        /// RemoveNode
        /// </summary>
        /// <remarks>
        /// Remove a node with the specified data set field id from a data set writer
        /// in a writer group. If the field is not found, an error is returned.
        /// </remarks>
        /// <param name="dataSetWriterGroup">The writer group name of the entry</param>
        /// <param name="dataSetWriterId">The data set writer identifer of the entry</param>
        /// <param name="dataSetFieldId">Identifier of the field to remove</param>
        /// <param name="ct"></param>
        /// <exception cref="ArgumentNullException"><paramref name="dataSetWriterGroup"/>
        /// is <c>null</c>.</exception>
        /// <exception cref="ArgumentNullException"><paramref name="dataSetWriterId"/>
        /// is <c>null</c>.</exception>
        /// <exception cref="ArgumentNullException"><paramref name="dataSetFieldId"/>
        /// is <c>null</c>.</exception>
        /// <response code="200">The item was removed</response>
        /// <response code="400">The passed in information is invalid</response>
        /// <response code="403">A unique item could not be found to remove from.</response>
        /// <response code="404">The entry or item to remove was not found</response>
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
        [HttpDelete("{dataSetWriterGroup}/{dataSetWriterId}/{dataSetFieldId}")]
        public async Task RemoveNodeAsync(string dataSetWriterGroup, string dataSetWriterId,
            string dataSetFieldId, CancellationToken ct = default)
        {
            ArgumentNullException.ThrowIfNull(dataSetWriterGroup);
            ArgumentNullException.ThrowIfNull(dataSetWriterId);
            ArgumentNullException.ThrowIfNull(dataSetFieldId);
            await _configServices.RemoveNodesAsync(dataSetWriterGroup, dataSetWriterId,
                new[] { dataSetFieldId }, ct).ConfigureAwait(false);
        }

        /// <summary>
        /// GetNode
        /// </summary>
        /// <remarks>
        /// Get a node from a dataset in a writer group.
        /// Dedicated errors are returned if no, or no unique entry could be found, or
        /// the node does not exist.
        /// </remarks>
        /// <param name="dataSetWriterGroup">The writer group name of the entry</param>
        /// <param name="dataSetWriterId">The data set writer identifer of the entry</param>
        /// <param name="dataSetFieldId">The data set field id of the node to return</param>
        /// <param name="ct"></param>
        /// <returns>The node inside the dataset</returns>
        /// <exception cref="ArgumentNullException"><paramref name="dataSetWriterGroup"/>
        /// is <c>null</c>.</exception>
        /// <exception cref="ArgumentNullException"><paramref name="dataSetWriterId"/>
        /// is <c>null</c>.</exception>
        /// <exception cref="ArgumentNullException"><paramref name="dataSetFieldId"/>
        /// is <c>null</c>.</exception>
        /// <response code="200">The item was retrieved</response>
        /// <response code="400">The passed in information is invalid</response>
        /// <response code="403">A unique item could not be found to get a node from.</response>
        /// <response code="404">The entry or item was not found</response>
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
        [HttpGet("{dataSetWriterGroup}/{dataSetWriterId}/{dataSetFieldId}")]
        public async Task<OpcNodeModel> GetNodeAsync(
            string dataSetWriterGroup, string dataSetWriterId, string dataSetFieldId,
            CancellationToken ct = default)
        {
            ArgumentNullException.ThrowIfNull(dataSetWriterGroup);
            ArgumentNullException.ThrowIfNull(dataSetWriterId);
            ArgumentNullException.ThrowIfNull(dataSetFieldId);
            return await _configServices.GetNodeAsync(
                dataSetWriterGroup, dataSetWriterId, dataSetFieldId, ct).ConfigureAwait(false);
        }

        /// <summary>
        /// GetNodes
        /// </summary>
        /// <remarks>
        /// Get Nodes from a data set writer in a writer group. The nodes can optionally
        /// be offset from a previous last node identified by the dataSetFieldId and
        /// pageanated by the pageSize. If the dataSetFieldId is not found, an empty list
        /// is returned. If the dataSetFieldId is not specified, the first page is returned.
        /// </remarks>
        /// <param name="dataSetWriterGroup">The writer group name of the entry</param>
        /// <param name="dataSetWriterId">The data set writer identifer of the entry</param>
        /// <param name="lastDataSetFieldId">the field id after which to start the page.
        /// If not specified, nodes from the beginning are returned.</param>
        /// <param name="pageSize">Number of nodes to return</param>
        /// <param name="ct"></param>
        /// <returns>List of nodes or none if none were found</returns>
        /// <exception cref="ArgumentNullException"><paramref name="dataSetWriterGroup"/>
        /// is <c>null</c>.</exception>
        /// <exception cref="ArgumentNullException"><paramref name="dataSetWriterId"/>
        /// is <c>null</c>.</exception>
        /// <response code="200">The items were found</response>
        /// <response code="400">The passed in information is invalid</response>
        /// <response code="403">A unique item could not be found to get nodes from.</response>
        /// <response code="404">The entry was not found</response>
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
        [HttpGet("{dataSetWriterGroup}/{dataSetWriterId}/nodes")]
        [AutoRestExtension(NextPageLinkName = "lastDataSetFieldId")]
        public async Task<IReadOnlyList<OpcNodeModel>> GetNodesAsync(
            string dataSetWriterGroup, string dataSetWriterId,
            [FromQuery] string? lastDataSetFieldId = null,
            [FromQuery] int? pageSize = null, CancellationToken ct = default)
        {
            ArgumentNullException.ThrowIfNull(dataSetWriterGroup);
            ArgumentNullException.ThrowIfNull(dataSetWriterId);
            if (Request != null)
            {
                if (Request.Headers.TryGetValue(HttpHeader.ContinuationToken, out var value))
                {
                    lastDataSetFieldId = value.FirstOrDefault();
                }
                if (Request.Headers.TryGetValue(HttpHeader.MaxItemCount, out value))
                {
                    pageSize = int.Parse(value.FirstOrDefault()!,
                        CultureInfo.InvariantCulture);
                }
            }
            return await _configServices.GetNodesAsync(dataSetWriterGroup, dataSetWriterId,
                lastDataSetFieldId, pageSize, ct).ConfigureAwait(false);
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
        /// <param name="force">Force delete all writers even if more than one were
        /// found. Does not error when none were found.</param>
        /// <param name="ct"></param>
        /// <exception cref="ArgumentNullException"><paramref name="dataSetWriterGroup"/>
        /// is <c>null</c>.</exception>
        /// <exception cref="ArgumentNullException"><paramref name="dataSetWriterId"/>
        /// is <c>null</c>.</exception>
        /// <response code="200">The entry was removed</response>
        /// <response code="400">The passed in information is invalid</response>
        /// <response code="403">A unique item could not be found to remove.</response>
        /// <response code="404">The entry to remove was not found</response>
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
        [HttpDelete("{dataSetWriterGroup}/{dataSetWriterId}")]
        public async Task RemoveDataSetWriterEntryAsync(string dataSetWriterGroup,
            string dataSetWriterId, [FromQuery] bool force = false, CancellationToken ct = default)
        {
            ArgumentNullException.ThrowIfNull(dataSetWriterGroup);
            ArgumentNullException.ThrowIfNull(dataSetWriterId);
            await _configServices.RemoveDataSetWriterEntryAsync(dataSetWriterGroup,
                dataSetWriterId, force, ct).ConfigureAwait(false);
        }

        private readonly IConfigurationServices _configServices;
    }
}
