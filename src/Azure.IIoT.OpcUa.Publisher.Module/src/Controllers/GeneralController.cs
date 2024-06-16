// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Azure.IIoT.OpcUa.Publisher.Module.Controllers
{
    using Azure.IIoT.OpcUa.Publisher.Module.Filters;
    using Azure.IIoT.OpcUa.Publisher.Models;
    using Asp.Versioning;
    using Furly.Extensions.Serializers;
    using Furly.Tunnel.Router;
    using Microsoft.AspNetCore.Authorization;
    using Microsoft.AspNetCore.Mvc;
    using System;
    using System.Collections.Generic;
    using System.ComponentModel.DataAnnotations;
    using System.Threading;
    using System.Threading.Tasks;

    /// <summary>
    /// <para>
    /// This section lists the general APi provided by OPC Publisher providing
    /// all connection, endpoint and address space related API methods.
    /// </para>
    /// <para>
    /// The method name for all transports other than HTTP (which uses the shown
    /// HTTP methods and resource uris) is the name of the subsection header.
    /// To use the version specific method append "_V1" or "_V2" to the method
    /// name.
    /// </para>
    /// </summary>
    [Version("_V1")]
    [Version("_V2")]
    [Version("")]
    [RouterExceptionFilter]
    [ControllerExceptionFilter]
    [ApiVersion("2")]
    [Route("v{version:apiVersion}")]
    [ApiController]
    [Authorize]
    public class GeneralController : ControllerBase, IMethodController
    {
        /// <summary>
        /// Create controller with service
        /// </summary>
        /// <param name="endpoints"></param>
        /// <param name="certificates"></param>
        /// <param name="nodes"></param>
        public GeneralController(IConnectionServices<ConnectionModel> endpoints,
            ICertificateServices<EndpointModel> certificates,
            INodeServices<ConnectionModel> nodes)
        {
            _certificates = certificates ??
                throw new ArgumentNullException(nameof(certificates));
            _nodes = nodes ??
                throw new ArgumentNullException(nameof(nodes));
            _endpoints = endpoints ??
                throw new ArgumentNullException(nameof(endpoints));
        }

        /// <summary>
        /// GetServerCapabilities
        /// </summary>
        /// <remarks>
        /// Get the capabilities of the server. The server capabilities are exposed
        /// as a property of the server object and this method provides a convinient
        /// way to retrieve them.
        /// </remarks>
        /// <param name="request">The request payload and connection information
        /// identifying the server to connect to perform the operation on.</param>
        /// <param name="ct"></param>
        /// <returns>The server capabilities.</returns>
        /// <exception cref="ArgumentNullException"><paramref name="request"/>
        /// is <c>null</c>.</exception>
        [HttpPost("capabilities")]
        public async Task<ServerCapabilitiesModel> GetServerCapabilitiesAsync(
            [FromBody][Required] RequestEnvelope<RequestHeaderModel?> request,
            CancellationToken ct = default)
        {
            ArgumentNullException.ThrowIfNull(request);
            ArgumentNullException.ThrowIfNull(request.Connection);
            return await _nodes.GetServerCapabilitiesAsync(request.Connection,
                request.Request, ct).ConfigureAwait(false);
        }

        /// <summary>
        /// Browse
        /// </summary>
        /// <remarks>
        /// Browse a a node to discover its references. For more information consult
        /// <a href="https://reference.opcfoundation.org/Core/Part4/v105/docs/5.8.2">
        /// the relevant section of the OPC UA reference specification</a>.
        /// The operation might return a continuation token. The continuation token
        /// can be used in the BrowseNext method call to retrieve the remainder of
        /// references or additional continuation tokens.
        /// </remarks>
        /// <param name="request">The request payload and connection information
        /// identifying the server to connect to perform the operation on.</param>
        /// <param name="ct"></param>
        /// <returns>The references and optional continuation token</returns>
        /// <exception cref="ArgumentNullException"><paramref name="request"/>
        /// is <c>null</c>.</exception>
        [HttpPost("browse/first")]
        public async Task<BrowseFirstResponseModel> BrowseAsync(
            [FromBody][Required] RequestEnvelope<BrowseFirstRequestModel> request,
            CancellationToken ct = default)
        {
            ArgumentNullException.ThrowIfNull(request);
            ArgumentNullException.ThrowIfNull(request.Connection);
            ArgumentNullException.ThrowIfNull(request.Request);
            return await _nodes.BrowseFirstAsync(request.Connection,
                request.Request, ct).ConfigureAwait(false);
        }

        /// <summary>
        /// BrowseNext
        /// </summary>
        /// <remarks>
        /// Browse next
        /// </remarks>
        /// <param name="request">The request payload and connection information
        /// identifying the server to connect to perform the operation on.</param>
        /// <param name="ct"></param>
        /// <returns>The references and optional continuation token</returns>
        /// <exception cref="ArgumentNullException"><paramref name="request"/>
        /// is <c>null</c>.</exception>
        [HttpPost("browse/next")]
        public async Task<BrowseNextResponseModel> BrowseNextAsync(
            [FromBody][Required] RequestEnvelope<BrowseNextRequestModel> request,
            CancellationToken ct = default)
        {
            ArgumentNullException.ThrowIfNull(request);
            ArgumentNullException.ThrowIfNull(request.Connection);
            ArgumentNullException.ThrowIfNull(request.Request);
            return await _nodes.BrowseNextAsync(request.Connection,
                request.Request, ct).ConfigureAwait(false);
        }

        /// <summary>
        /// BrowseStream (only HTTP transport)
        /// </summary>
        /// <remarks>
        /// Recursively browse a node to discover its references and nodes.
        /// The results are returned as a stream of nodes and references. Consult
        /// <a href="https://reference.opcfoundation.org/Core/Part4/v105/docs/5.8.2">
        /// the relevant section of the OPC UA reference specification</a> for more
        /// information.
        /// </remarks>
        /// <param name="request">The request payload and connection information
        /// identifying the server to connect to perform the operation on.</param>
        /// <param name="ct"></param>
        /// <returns>The nodes and references as stream.</returns>
        /// <exception cref="ArgumentNullException"><paramref name="request"/>
        /// is <c>null</c>.</exception>
        [HttpPost("browse")]
        public IAsyncEnumerable<BrowseStreamChunkModel> BrowseStreamAsync(
            [FromBody][Required] RequestEnvelope<BrowseStreamRequestModel> request,
            CancellationToken ct = default)
        {
            ArgumentNullException.ThrowIfNull(request);
            ArgumentNullException.ThrowIfNull(request.Connection);
            ArgumentNullException.ThrowIfNull(request.Request);
            return _nodes.BrowseAsync(request.Connection, request.Request, ct);
        }

        /// <summary>
        /// BrowsePath
        /// </summary>
        /// <remarks>
        /// Translate a start node and browse path into 0 or more target nodes.
        /// Allows programming aginst types in OPC UA. For more information consult
        /// <a href="https://reference.opcfoundation.org/Core/Part4/v105/docs/5.8.4">
        /// the relevant section of the OPC UA reference specification</a>.
        /// </remarks>
        /// <param name="request">The request payload and connection information
        /// identifying the server to connect to perform the operation on.</param>
        /// <param name="ct"></param>
        /// <returns>The target nodes found.</returns>
        /// <exception cref="ArgumentNullException"><paramref name="request"/>
        /// is <c>null</c>.</exception>
        [HttpPost("browse/path")]
        public async Task<BrowsePathResponseModel> BrowsePathAsync(
            [FromBody][Required] RequestEnvelope<BrowsePathRequestModel> request,
            CancellationToken ct = default)
        {
            ArgumentNullException.ThrowIfNull(request);
            ArgumentNullException.ThrowIfNull(request.Connection);
            ArgumentNullException.ThrowIfNull(request.Request);
            return await _nodes.BrowsePathAsync(request.Connection,
                request.Request, ct).ConfigureAwait(false);
        }

        /// <summary>
        /// ValueRead
        /// </summary>
        /// <remarks>
        /// Read the value of a variable node. This uses the service detailed in the
        /// <a href="https://reference.opcfoundation.org/Core/Part4/v105/docs/5.10.1">
        /// relevant section of the OPC UA reference specification</a>.
        /// </remarks>
        /// <param name="request">The request payload and connection information
        /// identifying the server to connect to perform the operation on.</param>
        /// <param name="ct"></param>
        /// <returns>The value read from the node variable or error information.</returns>
        /// <exception cref="ArgumentNullException"><paramref name="request"/>
        /// is <c>null</c>.</exception>
        [HttpPost("read")]
        public async Task<ValueReadResponseModel> ValueReadAsync(
            [FromBody][Required] RequestEnvelope<ValueReadRequestModel> request,
            CancellationToken ct = default)
        {
            ArgumentNullException.ThrowIfNull(request);
            ArgumentNullException.ThrowIfNull(request.Connection);
            ArgumentNullException.ThrowIfNull(request.Request);
            return await _nodes.ValueReadAsync(request.Connection,
                request.Request, ct).ConfigureAwait(false);
        }

        /// <summary>
        /// ValueWrite
        /// </summary>
        /// <remarks>
        /// Write the value of a variable node. This uses the service detailed in
        /// <a href="https://reference.opcfoundation.org/Core/Part4/v105/docs/5.10.4">
        /// the relevant section of the OPC UA reference specification</a>.
        /// </remarks>
        /// <param name="request">The request payload and connection information
        /// identifying the server to connect to perform the operation on.</param>
        /// <param name="ct"></param>
        /// <returns>The result of the write operation or error information.</returns>
        /// <exception cref="ArgumentNullException"><paramref name="request"/>
        /// is <c>null</c>.</exception>
        [HttpPost("write")]
        public async Task<ValueWriteResponseModel> ValueWriteAsync(
            [FromBody][Required] RequestEnvelope<ValueWriteRequestModel> request,
            CancellationToken ct = default)
        {
            ArgumentNullException.ThrowIfNull(request);
            ArgumentNullException.ThrowIfNull(request.Connection);
            ArgumentNullException.ThrowIfNull(request.Request);
            return await _nodes.ValueWriteAsync(request.Connection,
                request.Request, ct).ConfigureAwait(false);
        }

        /// <summary>
        /// GetMetadata
        /// </summary>
        /// <remarks>
        /// Get the type metadata for a any node. For data type nodes the
        /// response contains the data type metadata including fields. For
        /// method nodes the output and input arguments metadata is provided.
        /// For objects and object types the instance declaration is returned.
        /// </remarks>
        /// <param name="request">The request payload and connection information
        /// identifying the server to connect to perform the operation on.</param>
        /// <param name="ct"></param>
        /// <returns>The meta data of the node and optionally error information
        /// in case of failure.</returns>
        /// <exception cref="ArgumentNullException"><paramref name="request"/>
        /// is <c>null</c>.</exception>
        [HttpPost("metadata")]
        public async Task<NodeMetadataResponseModel> GetMetadataAsync(
            [FromBody][Required] RequestEnvelope<NodeMetadataRequestModel> request,
            CancellationToken ct = default)
        {
            ArgumentNullException.ThrowIfNull(request);
            ArgumentNullException.ThrowIfNull(request.Connection);
            ArgumentNullException.ThrowIfNull(request.Request);
            return await _nodes.GetMetadataAsync(request.Connection,
                request.Request, ct).ConfigureAwait(false);
        }

        /// <summary>
        /// CompileQuery
        /// </summary>
        /// <remarks>
        /// Compile a query string into a query spec that can be used when
        /// setting up event filters on monitored items that monitor events.
        /// </remarks>
        /// <param name="request">The compilation request and connection
        /// information.</param>
        /// <param name="ct"></param>
        /// <returns>The compilation response.</returns>
        [HttpPost("query/compile")]
        public async Task<QueryCompilationResponseModel> CompileQueryAsync(
            [FromBody][Required] RequestEnvelope<QueryCompilationRequestModel> request,
            CancellationToken ct = default)
        {
            ArgumentNullException.ThrowIfNull(request);
            ArgumentNullException.ThrowIfNull(request.Connection);
            ArgumentNullException.ThrowIfNull(request.Request);
            return await _nodes.CompileQueryAsync(request.Connection,
                request.Request, ct).ConfigureAwait(false);
        }

        /// <summary>
        /// MethodMetadata
        /// </summary>
        /// <remarks>
        /// Get the metadata for calling the method. This API is obsolete.
        /// Use the more powerful GetMetadata method instead.
        /// </remarks>
        /// <param name="request">The request payload and connection information
        /// identifying the server to connect to perform the operation on.</param>
        /// <param name="ct"></param>
        /// <returns>The method meta data and optionally error information
        /// in case of failure.</returns>
        /// <exception cref="ArgumentNullException"><paramref name="request"/>
        /// is <c>null</c>.</exception>
        [HttpPost("call/$metadata")]
        public async Task<MethodMetadataResponseModel> MethodMetadataAsync(
            [FromBody][Required] RequestEnvelope<MethodMetadataRequestModel> request,
            CancellationToken ct = default)
        {
            ArgumentNullException.ThrowIfNull(request);
            ArgumentNullException.ThrowIfNull(request.Connection);
            ArgumentNullException.ThrowIfNull(request.Request);
            return await _nodes.GetMethodMetadataAsync(request.Connection,
                request.Request, ct).ConfigureAwait(false);
        }

        /// <summary>
        /// MethodCall
        /// </summary>
        /// <remarks>
        /// Call a method on the OPC UA server endpoint with the specified input
        /// arguments and received the result in the form of the method output arguments.
        /// See <a href="https://reference.opcfoundation.org/Core/Part4/v105/docs/5.11.2">
        /// the relevant section of the OPC UA reference specification</a> for more
        /// information.
        /// </remarks>
        /// <param name="request">The request payload and connection information
        /// identifying the server to connect to perform the operation on.</param>
        /// <param name="ct"></param>
        /// <returns>The output argument values of the method call and optionally
        /// error information in case of failure.</returns>
        /// <exception cref="ArgumentNullException"><paramref name="request"/>
        /// is <c>null</c>.</exception>
        [HttpPost("call")]
        public async Task<MethodCallResponseModel> MethodCallAsync(
            [FromBody][Required] RequestEnvelope<MethodCallRequestModel> request,
            CancellationToken ct = default)
        {
            ArgumentNullException.ThrowIfNull(request);
            ArgumentNullException.ThrowIfNull(request.Connection);
            ArgumentNullException.ThrowIfNull(request.Request);
            return await _nodes.MethodCallAsync(request.Connection,
                request.Request, ct).ConfigureAwait(false);
        }

        /// <summary>
        /// NodeRead
        /// </summary>
        /// <remarks>
        /// Read any writeable attribute of a specified node on the server. See
        /// <a href="https://reference.opcfoundation.org/Core/Part4/v105/docs/5.10.2">
        /// the relevant section of the OPC UA reference specification</a> for more
        /// information. The attributes supported by the node are dependend
        /// on the node class of the node.
        /// </remarks>
        /// <param name="request">The request payload and connection information
        /// identifying the server to connect to perform the operation on.</param>
        /// <param name="ct"></param>
        /// <returns>The value of the attribute read and optionally
        /// error information in case of failure.</returns>
        /// <exception cref="ArgumentNullException"><paramref name="request"/>
        /// is <c>null</c>.</exception>
        [HttpPost("read/attributes")]
        public async Task<ReadResponseModel> NodeReadAsync(
            [FromBody][Required] RequestEnvelope<ReadRequestModel> request,
            CancellationToken ct = default)
        {
            ArgumentNullException.ThrowIfNull(request);
            ArgumentNullException.ThrowIfNull(request.Connection);
            ArgumentNullException.ThrowIfNull(request.Request);
            return await _nodes.ReadAsync(request.Connection,
                request.Request, ct).ConfigureAwait(false);
        }

        /// <summary>
        /// NodeWrite
        /// </summary>
        /// <remarks>
        /// Write any writeable attribute of a specified node on the server. See
        /// <a href="https://reference.opcfoundation.org/Core/Part4/v105/docs/5.10.4">
        /// the relevant section of the OPC UA reference specification</a> for more
        /// information. The attributes supported by the node are dependend
        /// on the node class of the node.
        /// </remarks>
        /// <param name="request">The request payload and connection information
        /// identifying the server to connect to perform the operation on.</param>
        /// <param name="ct"></param>
        /// <returns>The write operation result and optionally
        /// error information in case of failure.</returns>
        /// <exception cref="ArgumentNullException"><paramref name="request"/>
        /// is <c>null</c>.</exception>
        [HttpPost("write/attributes")]
        public async Task<WriteResponseModel> NodeWriteAsync(
            [FromBody][Required] RequestEnvelope<WriteRequestModel> request,
            CancellationToken ct = default)
        {
            ArgumentNullException.ThrowIfNull(request);
            ArgumentNullException.ThrowIfNull(request.Connection);
            ArgumentNullException.ThrowIfNull(request.Request);
            return await _nodes.WriteAsync(request.Connection,
                request.Request, ct).ConfigureAwait(false);
        }

        /// <summary>
        /// HistoryRead
        /// </summary>
        /// <remarks>
        /// Read the history using the respective OPC UA service call. See
        /// <a href="https://reference.opcfoundation.org/Core/Part4/v105/docs/5.10.3">
        /// the relevant section of the OPC UA reference specification</a> for more
        /// information. If continuation is returned the remaining results
        /// of the operation can be read using the HistoryReadNext method.
        /// </remarks>
        /// <param name="request">The request payload and connection information
        /// identifying the server to connect to perform the operation on.</param>
        /// <param name="ct"></param>
        /// <returns></returns>
        /// <exception cref="ArgumentNullException"><paramref name="request"/>
        /// is <c>null</c>.</exception>
        [HttpPost("historyread/first")]
        public async Task<HistoryReadResponseModel<VariantValue>> HistoryReadAsync(
            [FromBody][Required] RequestEnvelope<HistoryReadRequestModel<VariantValue>> request,
            CancellationToken ct = default)
        {
            ArgumentNullException.ThrowIfNull(request);
            ArgumentNullException.ThrowIfNull(request.Connection);
            ArgumentNullException.ThrowIfNull(request.Request);
            return await _nodes.HistoryReadAsync(request.Connection,
                request.Request, ct).ConfigureAwait(false);
        }

        /// <summary>
        /// HistoryReadNext
        /// </summary>
        /// <remarks>
        /// Read next history using the respective OPC UA service call. See
        /// <a href="https://reference.opcfoundation.org/Core/Part4/v105/docs/5.10.3">
        /// the relevant section of the OPC UA reference specification</a> for more
        /// information.
        /// </remarks>
        /// <param name="request">The request payload and connection information
        /// identifying the server to connect to perform the operation on.</param>
        /// <param name="ct"></param>
        /// <returns>The history read results.</returns>
        /// <exception cref="ArgumentNullException"><paramref name="request"/>
        /// is <c>null</c>.</exception>
        [HttpPost("historyread/next")]
        public async Task<HistoryReadNextResponseModel<VariantValue>> HistoryReadNextAsync(
            [FromBody][Required] RequestEnvelope<HistoryReadNextRequestModel> request,
            CancellationToken ct = default)
        {
            ArgumentNullException.ThrowIfNull(request);
            ArgumentNullException.ThrowIfNull(request.Connection);
            ArgumentNullException.ThrowIfNull(request.Request);
            return await _nodes.HistoryReadNextAsync(request.Connection,
                request.Request, ct).ConfigureAwait(false);
        }

        /// <summary>
        /// HistoryUpdate
        /// </summary>
        /// <remarks>
        /// Update history using the respective OPC UA service call. Consult the
        /// <a href="https://reference.opcfoundation.org/Core/Part4/v105/docs/5.10.5">
        /// relevant section of the OPC UA reference specification</a> for more
        /// information.
        /// </remarks>
        /// <param name="request">The request payload and connection information
        /// identifying the server to connect to perform the operation on.</param>
        /// <param name="ct"></param>
        /// <returns>The update result.</returns>
        /// <exception cref="ArgumentNullException"><paramref name="request"/>
        /// is <c>null</c>.</exception>
        [HttpPost("historyupdate")]
        public async Task<HistoryUpdateResponseModel> HistoryUpdateAsync(
            [FromBody][Required] RequestEnvelope<HistoryUpdateRequestModel<VariantValue>> request,
            CancellationToken ct = default)
        {
            ArgumentNullException.ThrowIfNull(request);
            ArgumentNullException.ThrowIfNull(request.Connection);
            ArgumentNullException.ThrowIfNull(request.Request);
            return await _nodes.HistoryUpdateAsync(request.Connection,
                request.Request, ct).ConfigureAwait(false);
        }

        /// <summary>
        /// GetEndpointCertificate
        /// </summary>
        /// <remarks>
        /// Get a server endpoint's certificate and certificate chain if
        /// available.
        /// </remarks>
        /// <param name="endpoint">The server endpoint to get the certificate
        /// for.</param>
        /// <param name="ct"></param>
        /// <returns>The certificate of the server endpoint</returns>
        /// <exception cref="ArgumentNullException"><paramref name="endpoint"/>
        /// is <c>null</c>.</exception>
        [HttpPost("certificate")]
        public async Task<X509CertificateChainModel> GetEndpointCertificateAsync(
            [FromBody][Required] EndpointModel endpoint, CancellationToken ct = default)
        {
            ArgumentNullException.ThrowIfNull(endpoint);
            return await _certificates.GetEndpointCertificateAsync(endpoint,
                ct).ConfigureAwait(false);
        }

        /// <summary>
        /// HistoryGetServerCapabilities
        /// </summary>
        /// <remarks>
        /// Get the historian capabilities exposed as part of the OPC UA server
        /// server object.
        /// </remarks>
        /// <param name="request">The request payload and connection information
        /// identifying the server to connect to perform the operation on.</param>
        /// <param name="ct"></param>
        /// <returns>The historian capabilities of the server.</returns>
        /// <exception cref="ArgumentNullException"><paramref name="request"/>
        /// is <c>null</c>.</exception>
        [HttpPost("history/capabilities")]
        public async Task<HistoryServerCapabilitiesModel> HistoryGetServerCapabilitiesAsync(
            [FromBody][Required] RequestEnvelope<RequestHeaderModel?> request, CancellationToken ct = default)
        {
            ArgumentNullException.ThrowIfNull(request);
            ArgumentNullException.ThrowIfNull(request.Connection);
            return await _nodes.HistoryGetServerCapabilitiesAsync(request.Connection,
                request.Request, ct).ConfigureAwait(false);
        }

        /// <summary>
        /// HistoryGetConfiguration
        /// </summary>
        /// <remarks>
        /// Get the historian configuration of a historizing node in the OPC UA server
        /// </remarks>
        /// <param name="request">The request payload and connection information
        /// identifying the server to connect to perform the operation on.</param>
        /// <param name="ct"></param>
        /// <returns>The node historian configuration.</returns>
        /// <exception cref="ArgumentNullException"><paramref name="request"/>
        /// is <c>null</c>.</exception>
        [HttpPost("history/configuration")]
        public async Task<HistoryConfigurationResponseModel> HistoryGetConfigurationAsync(
            [FromBody][Required] RequestEnvelope<HistoryConfigurationRequestModel> request,
            CancellationToken ct = default)
        {
            ArgumentNullException.ThrowIfNull(request);
            ArgumentNullException.ThrowIfNull(request.Connection);
            ArgumentNullException.ThrowIfNull(request.Request);
            return await _nodes.HistoryGetConfigurationAsync(request.Connection,
                request.Request, ct).ConfigureAwait(false);
        }

        /// <summary>
        /// TestConnection
        /// </summary>
        /// <remarks>
        /// Test connection to an opc ua server. The call will not establish
        /// any persistent connection but will just allow a client to test
        /// that the server is available.
        /// </remarks>
        /// <param name="request">The request payload and connection information
        /// identifying the server to connect to perform the operation on.</param>
        /// <param name="ct"></param>
        /// <returns>The test results.</returns>
        /// <exception cref="ArgumentNullException"><paramref name="request"/>
        /// is <c>null</c>.</exception>
        [HttpPost("test")]
        public async Task<TestConnectionResponseModel> TestConnectionAsync(
            [FromBody][Required] RequestEnvelope<TestConnectionRequestModel> request,
            CancellationToken ct = default)
        {
            ArgumentNullException.ThrowIfNull(request);
            ArgumentNullException.ThrowIfNull(request.Connection);
            ArgumentNullException.ThrowIfNull(request.Request);
            return await _endpoints.TestConnectionAsync(request.Connection,
                request.Request, ct).ConfigureAwait(false);
        }

        private readonly ICertificateServices<EndpointModel> _certificates;
        private readonly IConnectionServices<ConnectionModel> _endpoints;
        private readonly INodeServices<ConnectionModel> _nodes;
    }
}
