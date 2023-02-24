// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Azure.IIoT.OpcUa.Services.WebApi.Controllers
{
    using Azure.IIoT.OpcUa;
    using Azure.IIoT.OpcUa.Services.WebApi.Auth;
    using Azure.IIoT.OpcUa.Services.WebApi.Filters;
    using Azure.IIoT.OpcUa.Models;
    using Microsoft.AspNetCore.Authorization;
    using Microsoft.AspNetCore.Mvc;
    using System;
    using System.ComponentModel.DataAnnotations;
    using System.Threading.Tasks;

    /// <summary>
    /// Node access read services
    /// </summary>
    [ApiVersion("2")]
    [Route("twin/v{version:apiVersion}/read")]
    [ExceptionsFilter]
    [Authorize(Policy = Policies.CanRead)]
    [ApiController]
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
        /// Read variable value
        /// </summary>
        /// <remarks>
        /// Read a variable node's value.
        /// The endpoint must be in the registry and the server accessible.
        /// </remarks>
        /// <param name="endpointId">The identifier of the activated endpoint.</param>
        /// <param name="request">The read value request</param>
        /// <returns>The read value response</returns>
        [HttpPost("{endpointId}")]
        public async Task<ValueReadResponseModel> ReadValueAsync(
            string endpointId, [FromBody][Required] ValueReadRequestModel request)
        {
            if (request == null)
            {
                throw new ArgumentNullException(nameof(request));
            }
            return await _nodes.ValueReadAsync(
                endpointId, request).ConfigureAwait(false);
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
        /// <returns>The read response</returns>
        [HttpPost("{endpointId}/attributes")]
        public async Task<ReadResponseModel> ReadAttributesAsync(
            string endpointId, [FromBody][Required] ReadRequestModel request)
        {
            if (request == null)
            {
                throw new ArgumentNullException(nameof(request));
            }
            return await _nodes.ReadAsync(
                endpointId, request).ConfigureAwait(false);
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
        /// <returns>The read value response</returns>
        [HttpGet("{endpointId}")]
        public async Task<ValueReadResponseModel> GetValueAsync(
            string endpointId, [FromQuery][Required] string nodeId)
        {
            if (string.IsNullOrEmpty(nodeId))
            {
                throw new ArgumentNullException(nameof(nodeId));
            }
            var request = new ValueReadRequestModel { NodeId = nodeId };
            return await _nodes.ValueReadAsync(
                endpointId, request).ConfigureAwait(false);
        }

        private readonly INodeServices<string> _nodes;
    }
}
