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
    using Furly.Extensions.Serializers;
    using Furly.Tunnel.Router;
    using Microsoft.AspNetCore.Authorization;
    using Microsoft.AspNetCore.Http;
    using Microsoft.AspNetCore.Mvc;
    using Microsoft.Extensions.Primitives;
    using System;
    using System.Collections.Generic;
    using System.ComponentModel.DataAnnotations;
    using System.Runtime.Serialization;
    using System.Threading;
    using System.Threading.Tasks;

    /// <summary>
    /// <para>
    /// This section lists the file transfer API provided by OPC Publisher providing
    /// access to file transfer services to move files in and out of a server
    /// using the File transfer specification.
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
    [Route("v{version:apiVersion}/filesystem")]
    [ApiController]
    [Authorize]
    [Produces(ContentMimeType.Json, ContentMimeType.MsgPack)]
    [Consumes(ContentMimeType.Json, ContentMimeType.MsgPack)]
    public class FileSystemController : ControllerBase, IMethodController
    {
        /// <summary>
        /// Create controller with service
        /// </summary>
        /// <param name="files"></param>
        /// <param name="serializer"></param>
        public FileSystemController(IFileSystemServices<ConnectionModel> files,
            IJsonSerializer serializer)
        {
            _files = files ?? throw new ArgumentNullException(nameof(files));
            _serializer = serializer ?? throw new ArgumentNullException(nameof(serializer));
        }

        /// <summary>
        /// GetFileSystems
        /// </summary>
        /// <remarks>
        /// Gets all file systems of the server.
        /// </remarks>
        /// <param name="connection">The connection information identifying the
        /// server to connect to perform the operation on.</param>
        /// <param name="ct"></param>
        /// <returns>The directories.</returns>
        /// <exception cref="ArgumentNullException"><paramref name="connection"/>
        /// is <c>null</c>.</exception>
        /// <response code="200">The operation was successful or the response payload
        /// contains relevant error information.</response>
        /// <response code="400">The passed in information is invalid</response>
        /// <response code="408">The operation timed out.</response>
        /// <response code="500">An unexpected error occurred</response>
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status408RequestTimeout)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status500InternalServerError)]
        [HttpPost("list")]
        public IAsyncEnumerable<ServiceResponse<FileSystemObjectModel>> GetFileSystemsAsync(
            [FromBody][Required] ConnectionModel connection,
            CancellationToken ct = default)
        {
            ArgumentNullException.ThrowIfNull(connection);
            return _files.GetFileSystemsAsync(connection, ct);
        }

        /// <summary>
        /// GetDirectories
        /// </summary>
        /// <remarks>
        /// Gets all directories in a directory or file system
        /// </remarks>
        /// <param name="request">The directory or filesystem object and connection
        /// information identifying the server to connect to perform the operation
        /// on.</param>
        /// <param name="ct"></param>
        /// <returns>The directories.</returns>
        /// <exception cref="ArgumentNullException"><paramref name="request"/>
        /// is <c>null</c>.</exception>
        /// <response code="200">The operation was successful or the response payload
        /// contains relevant error information.</response>
        /// <response code="400">The passed in information is invalid</response>
        /// <response code="408">The operation timed out.</response>
        /// <response code="500">An unexpected error occurred</response>
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status408RequestTimeout)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status500InternalServerError)]
        [HttpPost("list/directories")]
        public async Task<ServiceResponse<IEnumerable<FileSystemObjectModel>>> GetDirectoriesAsync(
            [FromBody][Required] RequestEnvelope<FileSystemObjectModel> request,
            CancellationToken ct = default)
        {
            ArgumentNullException.ThrowIfNull(request);
            ArgumentNullException.ThrowIfNull(request.Connection);
            ArgumentNullException.ThrowIfNull(request.Request);
            return await _files.GetDirectoriesAsync(request.Connection,
                request.Request, ct).ConfigureAwait(false);
        }

        /// <summary>
        /// GetFiles
        /// </summary>
        /// <remarks>
        /// Get files in a directory or file system on a server.
        /// </remarks>
        /// <param name="request">The directory or filesystem object and connection
        /// information identifying the server to connect to perform the operation
        /// on.</param>
        /// <param name="ct"></param>
        /// <returns>The file information.</returns>
        /// <exception cref="ArgumentNullException"><paramref name="request"/>
        /// is <c>null</c>.</exception>
        /// <response code="200">The operation was successful or the response payload
        /// contains relevant error information.</response>
        /// <response code="400">The passed in information is invalid</response>
        /// <response code="408">The operation timed out.</response>
        /// <response code="500">An unexpected error occurred</response>
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status408RequestTimeout)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status500InternalServerError)]
        [HttpPost("list/files")]
        public async Task<ServiceResponse<IEnumerable<FileSystemObjectModel>>> GetFilesAsync(
            [FromBody][Required] RequestEnvelope<FileSystemObjectModel> request,
            CancellationToken ct = default)
        {
            ArgumentNullException.ThrowIfNull(request);
            ArgumentNullException.ThrowIfNull(request.Connection);
            ArgumentNullException.ThrowIfNull(request.Request);
            return await _files.GetFilesAsync(request.Connection,
                request.Request, ct).ConfigureAwait(false);
        }

        /// <summary>
        /// GetParent
        /// </summary>
        /// <remarks>
        /// Gets the parent directory or filesystem of a file or directory.
        /// </remarks>
        /// <param name="request">The file or directory object and connection information
        /// identifying the server to connect to perform the operation on.</param>
        /// <param name="ct"></param>
        /// <returns>The parent directory or filesystem.</returns>
        /// <exception cref="ArgumentNullException"><paramref name="request"/>
        /// is <c>null</c>.</exception>
        /// <response code="200">The operation was successful or the response payload
        /// contains relevant error information.</response>
        /// <response code="400">The passed in information is invalid</response>
        /// <response code="408">The operation timed out.</response>
        /// <response code="500">An unexpected error occurred</response>
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status408RequestTimeout)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status500InternalServerError)]
        [HttpPost("parent")]
        public async Task<ServiceResponse<FileSystemObjectModel>> GetParentAsync(
            [FromBody][Required] RequestEnvelope<FileSystemObjectModel> request,
            CancellationToken ct = default)
        {
            ArgumentNullException.ThrowIfNull(request);
            ArgumentNullException.ThrowIfNull(request.Connection);
            ArgumentNullException.ThrowIfNull(request.Request);
            return await _files.GetParentAsync(request.Connection,
                request.Request, ct).ConfigureAwait(false);
        }

        /// <summary>
        /// GetFileInfo
        /// </summary>
        /// <remarks>
        /// Gets the file information for a file on the server.
        /// </remarks>
        /// <param name="request">The file object and connection information
        /// identifying the server to connect to perform the operation on.</param>
        /// <param name="ct"></param>
        /// <returns>The file information.</returns>
        /// <exception cref="ArgumentNullException"><paramref name="request"/>
        /// is <c>null</c>.</exception>
        /// <response code="200">The operation was successful or the response payload
        /// contains relevant error information.</response>
        /// <response code="400">The passed in information is invalid</response>
        /// <response code="408">The operation timed out.</response>
        /// <response code="500">An unexpected error occurred</response>
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status408RequestTimeout)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status500InternalServerError)]
        [HttpPost("info/file")]
        public async Task<ServiceResponse<FileInfoModel>> GetFileInfoAsync(
            [FromBody][Required] RequestEnvelope<FileSystemObjectModel> request,
            CancellationToken ct = default)
        {
            ArgumentNullException.ThrowIfNull(request);
            ArgumentNullException.ThrowIfNull(request.Connection);
            ArgumentNullException.ThrowIfNull(request.Request);
            return await _files.GetFileInfoAsync(request.Connection,
                request.Request, ct).ConfigureAwait(false);
        }

        /// <summary>
        /// CreateFile
        /// </summary>
        /// <remarks>
        /// Create a new file in a directory or file system on the server
        /// </remarks>
        /// <param name="request">The file system or directory object to create the
        /// file in and the connection information identifying the server to
        /// connect to perform the operation on.</param>
        /// <param name="name">The name of the file to create as child
        /// under the directory or filesystem provided</param>
        /// <param name="ct"></param>
        /// <returns>The new directory.</returns>
        /// <exception cref="ArgumentNullException"><paramref name="request"/>
        /// is <c>null</c>.</exception>
        /// <response code="200">The operation was successful or the response payload
        /// contains relevant error information.</response>
        /// <response code="400">The passed in information is invalid</response>
        /// <response code="408">The operation timed out.</response>
        /// <response code="500">An unexpected error occurred</response>
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status408RequestTimeout)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status500InternalServerError)]
        [HttpPost("create/file/{name}")]
        public async Task<ServiceResponse<FileSystemObjectModel>> CreateFileAsync(
            [FromBody][Required] RequestEnvelope<FileSystemObjectModel> request,
            string name, CancellationToken ct = default)
        {
            ArgumentNullException.ThrowIfNull(request);
            ArgumentNullException.ThrowIfNull(request.Connection);
            ArgumentNullException.ThrowIfNull(request.Request);
            ArgumentNullException.ThrowIfNullOrWhiteSpace(name);
            return await _files.CreateFileAsync(request.Connection,
                request.Request, name, ct).ConfigureAwait(false);
        }

        /// <summary>
        /// CreateDirectory
        /// </summary>
        /// <remarks>
        /// Create a new directory in an existing file system or directory on the server.
        /// </remarks>
        /// <param name="request">The file system or directory object to create the
        /// directory in and the connection information identifying the server to
        /// connect to perform the operation on.</param>
        /// <param name="name">The name of the directory to create as child
        /// under the parent directory provided</param>
        /// <param name="ct"></param>
        /// <returns>The new file.</returns>
        /// <exception cref="ArgumentNullException"><paramref name="request"/>
        /// is <c>null</c>.</exception>
        /// <response code="200">The operation was successful or the response payload
        /// contains relevant error information.</response>
        /// <response code="400">The passed in information is invalid</response>
        /// <response code="408">The operation timed out.</response>
        /// <response code="500">An unexpected error occurred</response>
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status408RequestTimeout)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status500InternalServerError)]
        [HttpPost("create/directory/{name}")]
        public async Task<ServiceResponse<FileSystemObjectModel>> CreateDirectoryAsync(
            [FromBody][Required] RequestEnvelope<FileSystemObjectModel> request,
            string name, CancellationToken ct = default)
        {
            ArgumentNullException.ThrowIfNull(request);
            ArgumentNullException.ThrowIfNull(request.Connection);
            ArgumentNullException.ThrowIfNull(request.Request);
            ArgumentNullException.ThrowIfNullOrWhiteSpace(name);
            return await _files.CreateDirectoryAsync(request.Connection,
                request.Request, name, ct).ConfigureAwait(false);
        }

        /// <summary>
        /// DeleteFileSystemObject
        /// </summary>
        /// <remarks>
        /// Delete a file or directory in an existing file system on the server.
        /// </remarks>
        /// <param name="request">The file or directory object to delete and the
        /// connection information identifying the server to connect to perform
        /// the operation on.</param>
        /// <param name="ct"></param>
        /// <returns>The new file.</returns>
        /// <exception cref="ArgumentNullException"><paramref name="request"/>
        /// is <c>null</c>.</exception>
        /// <response code="200">The operation was successful or the response payload
        /// contains relevant error information.</response>
        /// <response code="400">The passed in information is invalid</response>
        /// <response code="408">The operation timed out.</response>
        /// <response code="500">An unexpected error occurred</response>
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status408RequestTimeout)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status500InternalServerError)]
        [HttpPost("delete")]
        public async Task<ServiceResultModel> DeleteFileSystemObjectAsync(
            [FromBody][Required] RequestEnvelope<FileSystemObjectModel> request,
            CancellationToken ct = default)
        {
            ArgumentNullException.ThrowIfNull(request);
            ArgumentNullException.ThrowIfNull(request.Connection);
            ArgumentNullException.ThrowIfNull(request.Request);
            return await _files.DeleteFileSystemObjectAsync(request.Connection,
                request.Request, ct: ct).ConfigureAwait(false);
        }

        /// <summary>
        /// DeleteFileOrDirectory
        /// </summary>
        /// <remarks>
        /// Delete a file or directory in the specified directory or file system.
        /// </remarks>
        /// <param name="request">The filesystem or directory object in which to
        /// delete the specified file or directory and the connection to use for
        /// the operation.</param>
        /// <param name="fileOrDirectoryNodeId">The node id of the file or
        /// directory to delete</param>
        /// <param name="ct"></param>
        /// <returns>The new file.</returns>
        /// <exception cref="ArgumentNullException"><paramref name="request"/>
        /// is <c>null</c>.</exception>
        /// <response code="200">The operation was successful or the response payload
        /// contains relevant error information.</response>
        /// <response code="400">The passed in information is invalid</response>
        /// <response code="408">The operation timed out.</response>
        /// <response code="500">An unexpected error occurred</response>
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status408RequestTimeout)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status500InternalServerError)]
        [HttpPost("delete/{fileOrDirectoryNodeId}")]
        public async Task<ServiceResultModel> DeleteFileOrDirectoryAsync(
            [FromBody][Required] RequestEnvelope<FileSystemObjectModel> request,
            string fileOrDirectoryNodeId, CancellationToken ct = default)
        {
            ArgumentNullException.ThrowIfNull(request);
            ArgumentNullException.ThrowIfNull(request.Connection);
            ArgumentNullException.ThrowIfNull(request.Request);
            ArgumentNullException.ThrowIfNullOrWhiteSpace(fileOrDirectoryNodeId);
            return await _files.DeleteFileSystemObjectAsync(request.Connection,
                new FileSystemObjectModel
                {
                    NodeId = fileOrDirectoryNodeId
                }, request.Request, ct).ConfigureAwait(false);
        }

        /// <summary>
        /// Download
        /// </summary>
        /// <remarks>
        /// Download a file from the server
        /// </remarks>
        /// <param name="connectionJson">The connection information identifying the server
        /// to connect to perform the operation on. This is passed as json serialized via
        /// the header "x-ms-connection"</param>
        /// <param name="fileObjectJson">The file object to upload. This is passed as json
        /// serialized via the header "x-ms-target"</param>
        /// <param name="ct"></param>
        /// <returns></returns>
        /// <exception cref="ArgumentNullException"><paramref name="connectionJson"/>
        /// is <c>null</c>.</exception>
        /// <exception cref="ArgumentNullException"><paramref name="fileObjectJson"/>
        /// is <c>null</c>.</exception>
        /// <exception cref="NotSupportedException">The operation is not supported
        /// over the transport chosen</exception>
        /// <response code="200">The operation was successful or the response payload
        /// contains relevant error information.</response>
        /// <response code="400">The passed in information is invalid</response>
        /// <response code="408">The operation timed out.</response>
        /// <response code="500">An unexpected error occurred</response>
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status408RequestTimeout)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status500InternalServerError)]
        [HttpGet("download")]
        public async Task DownloadAsync(
            [FromHeader(Name = "x-ms-connection")][Required] string connectionJson,
            [FromHeader(Name = "x-ms-target")][Required] string fileObjectJson,
            CancellationToken ct = default)
        {
            ArgumentNullException.ThrowIfNullOrWhiteSpace(connectionJson);
            ArgumentNullException.ThrowIfNullOrWhiteSpace(fileObjectJson);

            var connection = _serializer.Deserialize<ConnectionModel>(connectionJson);
            var fileObject = _serializer.Deserialize<FileSystemObjectModel>(fileObjectJson);

            ArgumentNullException.ThrowIfNull(connection);
            ArgumentNullException.ThrowIfNull(fileObject);

            if (HttpContext == null)
            {
                throw new NotSupportedException("Download not supported");
            }
            var response = HttpContext.Response;
            await response.StartAsync(ct).ConfigureAwait(false);
            var result = await _files.CopyToAsync(connection,
                fileObject, response.Body, ct).ConfigureAwait(false);
            if (result?.StatusCode != 0)
            {
                HttpContext.Response.StatusCode = StatusCodes.Status500InternalServerError;
                HttpContext.Response.Headers.Append("errorInfo",
                    new StringValues(_serializer.SerializeObjectToString(result)));
            }
            await response.CompleteAsync().ConfigureAwait(false);
        }

        /// <summary>
        /// Upload
        /// </summary>
        /// <remarks>
        /// Upload a file to the server.
        /// </remarks>
        /// <param name="connectionJson">The connection information identifying the server
        /// to connect to perform the operation on. This is passed as json serialized via
        /// the header "x-ms-connection"</param>
        /// <param name="fileObjectJson">The file object to upload. This is passed as json
        /// serialized via the header "x-ms-target"</param>
        /// <param name="modeJson">The file write mode to use passed as header "x-ms-mode"
        /// </param>
        /// <param name="ct"></param>
        /// <returns></returns>
        /// <exception cref="ArgumentNullException"><paramref name="connectionJson"/>
        /// is <c>null</c>.</exception>
        /// <exception cref="ArgumentNullException"><paramref name="fileObjectJson"/>
        /// is <c>null</c>.</exception>
        /// <exception cref="ArgumentNullException"><paramref name="modeJson"/>
        /// is <c>null</c>.</exception>
        /// <exception cref="NotSupportedException">The operation is not supported
        /// over the transport chosen</exception>
        /// <response code="200">The operation was successful or the response payload
        /// contains relevant error information.</response>
        /// <response code="400">The passed in information is invalid</response>
        /// <response code="408">The operation timed out.</response>
        /// <response code="500">An unexpected error occurred</response>
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status408RequestTimeout)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status500InternalServerError)]
        [HttpPost("upload")]
        public async Task UploadAsync(
            [FromHeader(Name = "x-ms-connection")][Required] string connectionJson,
            [FromHeader(Name = "x-ms-target")][Required] string fileObjectJson,
            [FromHeader(Name = "x-ms-mode")][Required] string modeJson,
            CancellationToken ct = default)
        {
            ArgumentNullException.ThrowIfNullOrWhiteSpace(connectionJson);
            ArgumentNullException.ThrowIfNullOrWhiteSpace(fileObjectJson);
            ArgumentNullException.ThrowIfNullOrWhiteSpace(modeJson);

            var connection = _serializer.Deserialize<ConnectionModel>(connectionJson);
            var fileObject = _serializer.Deserialize<FileSystemObjectModel>(fileObjectJson);
            var mode = _serializer.Deserialize<FileWriteMode>(modeJson);

            ArgumentNullException.ThrowIfNull(connection);
            ArgumentNullException.ThrowIfNull(fileObject);

            if (HttpContext == null)
            {
                throw new NotSupportedException("Upload not supported");
            }

            await using (var _ = HttpContext.Request.Body.ConfigureAwait(false))
            {
                var result = await _files.CopyFromAsync(connection,
                    fileObject, HttpContext.Request.Body, mode, ct).ConfigureAwait(false);

                if (result?.StatusCode != 0)
                {
                    HttpContext.Response.StatusCode = StatusCodes.Status500InternalServerError;
                    HttpContext.Response.Headers.Append("errorInfo",
                        new StringValues(_serializer.SerializeObjectToString(result)));
                }
            }
        }
        private readonly IFileSystemServices<ConnectionModel> _files;
        private readonly IJsonSerializer _serializer;
    }

    /// <summary>
    /// Combines a request envelope and file
    /// </summary>
    /// <typeparam name="T"></typeparam>
    public record RequestEnvelopeWithFile<T> : RequestEnvelope<T>
    {
        /// <summary>
        /// File to upload
        /// </summary>
        [DataMember(Name = "file", Order = 2)]
        public IFormFile? File { get; set; }
    }
}
