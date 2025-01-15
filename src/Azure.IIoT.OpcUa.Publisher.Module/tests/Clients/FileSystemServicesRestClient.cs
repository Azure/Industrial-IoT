// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Azure.IIoT.OpcUa.Publisher.Module.Tests.Clients
{
    using Azure.IIoT.OpcUa.Publisher.Models;
    using Azure.IIoT.OpcUa.Publisher.Sdk;
    using Furly.Extensions.Serializers;
    using Furly.Extensions.Serializers.Newtonsoft;
    using Microsoft.Extensions.Options;
    using System;
    using System.Buffers;
    using System.Collections.Generic;
    using System.IO;
    using System.IO.Pipelines;
    using System.Linq;
    using System.Net.Http;
    using System.Net.Http.Headers;
    using System.Threading;
    using System.Threading.Tasks;

    /// <summary>
    /// Implementation of file system services over http
    /// </summary>
    public sealed class FileSystemServicesRestClient : IFileSystemServices<ConnectionModel>
    {
        /// <summary>
        /// Create service client
        /// </summary>
        /// <param name="httpClient"></param>
        /// <param name="options"></param>
        /// <param name="serializer"></param>
        public FileSystemServicesRestClient(IHttpClientFactory httpClient,
            IOptions<SdkOptions> options, ISerializer serializer) :
            this(httpClient, options?.Value.Target, serializer)
        {
        }

        /// <summary>
        /// Create service client
        /// </summary>
        /// <param name="httpClient"></param>
        /// <param name="serviceUri"></param>
        /// <param name="serializer"></param>
        public FileSystemServicesRestClient(IHttpClientFactory httpClient, string serviceUri,
            ISerializer serializer = null)
        {
            if (string.IsNullOrWhiteSpace(serviceUri))
            {
                throw new ArgumentNullException(nameof(serviceUri),
                    "Please configure the Url of the endpoint micro service.");
            }
            _serviceUri = serviceUri.TrimEnd('/');
            _serializer = serializer ?? new NewtonsoftJsonSerializer();
            _httpClient = httpClient ?? throw new ArgumentNullException(nameof(httpClient));
        }

        /// <inheritdoc/>
        public IAsyncEnumerable<ServiceResponse<FileSystemObjectModel>> GetFileSystemsAsync(
            ConnectionModel endpoint, CancellationToken ct)
        {
            ArgumentNullException.ThrowIfNull(endpoint);
            var uri = new Uri($"{_serviceUri}/v2/filesystem/list");
            return _httpClient.PostStreamAsync<ServiceResponse<FileSystemObjectModel>>(uri,
                endpoint, _serializer, ct: ct);
        }

        /// <inheritdoc/>
        public async Task<ServiceResponse<IEnumerable<FileSystemObjectModel>>> GetDirectoriesAsync(
            ConnectionModel endpoint, FileSystemObjectModel fileSystemOrDirectory, CancellationToken ct)
        {
            ArgumentNullException.ThrowIfNull(endpoint);
            ArgumentNullException.ThrowIfNull(fileSystemOrDirectory);
            var uri = new Uri($"{_serviceUri}/v2/filesystem/list/directories");
            return await _httpClient.PostAsync<ServiceResponse<IEnumerable<FileSystemObjectModel>>>(uri,
                RequestBody(endpoint, fileSystemOrDirectory), _serializer, ct: ct).ConfigureAwait(false);
        }

        /// <inheritdoc/>
        public async Task<ServiceResponse<IEnumerable<FileSystemObjectModel>>> GetFilesAsync(
            ConnectionModel endpoint, FileSystemObjectModel fileSystemOrDirectory, CancellationToken ct)
        {
            ArgumentNullException.ThrowIfNull(endpoint);
            ArgumentNullException.ThrowIfNull(fileSystemOrDirectory);
            var uri = new Uri($"{_serviceUri}/v2/filesystem/list/files");
            return await _httpClient.PostAsync<ServiceResponse<IEnumerable<FileSystemObjectModel>>>(uri,
                RequestBody(endpoint, fileSystemOrDirectory), _serializer, ct: ct).ConfigureAwait(false);
        }

        /// <inheritdoc/>
        public async Task<ServiceResponse<FileSystemObjectModel>> GetParentAsync(ConnectionModel endpoint,
            FileSystemObjectModel fileOrDirectoryObject, CancellationToken ct)
        {
            ArgumentNullException.ThrowIfNull(endpoint);
            ArgumentNullException.ThrowIfNull(fileOrDirectoryObject);
            var uri = new Uri($"{_serviceUri}/v2/filesystem/parent");
            return await _httpClient.PostAsync<ServiceResponse<FileSystemObjectModel>>(uri,
                RequestBody(endpoint, fileOrDirectoryObject), _serializer, ct: ct).ConfigureAwait(false);
        }

        /// <inheritdoc/>
        public async Task<ServiceResponse<FileInfoModel>> GetFileInfoAsync(ConnectionModel endpoint,
            FileSystemObjectModel file, CancellationToken ct)
        {
            ArgumentNullException.ThrowIfNull(endpoint);
            ArgumentNullException.ThrowIfNull(file);
            var uri = new Uri($"{_serviceUri}/v2/filesystem/info/file");
            return await _httpClient.PostAsync<ServiceResponse<FileInfoModel>>(uri,
                RequestBody(endpoint, file), _serializer, ct: ct).ConfigureAwait(false);
        }

        /// <inheritdoc/>
        public async Task<ServiceResponse<Stream>> OpenReadAsync(ConnectionModel endpoint,
            FileSystemObjectModel file, CancellationToken ct)
        {
            ArgumentNullException.ThrowIfNull(endpoint);
            ArgumentNullException.ThrowIfNull(file);
            return await DownloadStream.CreateAsync(this, endpoint, file, ct).ConfigureAwait(false);
        }

        /// <inheritdoc/>
        public Task<ServiceResponse<Stream>> OpenWriteAsync(ConnectionModel endpoint,
            FileSystemObjectModel file, FileOpenWriteOptionsModel options, CancellationToken ct)
        {
            ArgumentNullException.ThrowIfNull(endpoint);
            ArgumentNullException.ThrowIfNull(file);

            return Task.FromResult(UploadStream.Create(this, endpoint, file, options, ct));
        }

        /// <inheritdoc/>
        public async Task<ServiceResponse<FileSystemObjectModel>> CreateDirectoryAsync(ConnectionModel endpoint,
            FileSystemObjectModel fileSystemOrDirectory, string name, CancellationToken ct)
        {
            ArgumentNullException.ThrowIfNull(endpoint);
            ArgumentNullException.ThrowIfNull(fileSystemOrDirectory);
            ArgumentNullException.ThrowIfNullOrWhiteSpace(name);
            var uri = new Uri($"{_serviceUri}/v2/filesystem/create/directory/{name.UrlEncode()}");
            return await _httpClient.PostAsync<ServiceResponse<FileSystemObjectModel>>(uri,
                RequestBody(endpoint, fileSystemOrDirectory), _serializer, ct: ct).ConfigureAwait(false);
        }

        /// <inheritdoc/>
        public async Task<ServiceResponse<FileSystemObjectModel>> CreateFileAsync(ConnectionModel endpoint,
            FileSystemObjectModel fileSystemOrDirectory, string name, CancellationToken ct)
        {
            ArgumentNullException.ThrowIfNull(endpoint);
            ArgumentNullException.ThrowIfNull(fileSystemOrDirectory);
            ArgumentNullException.ThrowIfNullOrWhiteSpace(name);
            var uri = new Uri($"{_serviceUri}/v2/filesystem/create/file/{name.UrlEncode()}");
            return await _httpClient.PostAsync<ServiceResponse<FileSystemObjectModel>>(uri,
                RequestBody(endpoint, fileSystemOrDirectory), _serializer, ct: ct).ConfigureAwait(false);
        }

        /// <inheritdoc/>
        public async Task<ServiceResultModel> DeleteFileSystemObjectAsync(ConnectionModel endpoint,
            FileSystemObjectModel fileOrDirectoryObject, FileSystemObjectModel parentFileSystemOrDirectory,
            CancellationToken ct)
        {
            ArgumentNullException.ThrowIfNull(endpoint);
            ArgumentNullException.ThrowIfNull(fileOrDirectoryObject);
            if (parentFileSystemOrDirectory == null)
            {
                var uri = new Uri($"{_serviceUri}/v2/filesystem/delete");
                return await _httpClient.PostAsync<ServiceResultModel>(uri,
                    RequestBody(endpoint, fileOrDirectoryObject), _serializer, ct: ct).ConfigureAwait(false);
            }
            else
            {
                if (fileOrDirectoryObject.BrowsePath?.Count > 0)
                {
                    throw new NotSupportedException("Not yet supported");
                }
                var uri = new Uri($"{_serviceUri}/v2/filesystem/delete/{fileOrDirectoryObject.NodeId.UrlEncode()}");
                return await _httpClient.PostAsync<ServiceResultModel>(uri,
                    RequestBody(endpoint, parentFileSystemOrDirectory), _serializer, ct: ct).ConfigureAwait(false);
            }
        }

        /// <summary>
        /// Create envelope
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="connection"></param>
        /// <param name="request"></param>
        /// <returns></returns>
        private static RequestEnvelope<T> RequestBody<T>(ConnectionModel connection, T request)
        {
            return new RequestEnvelope<T> { Connection = connection, Request = request };
        }

        /// <summary>
        /// File stream wraps a body of a response
        /// </summary>
        internal sealed class DownloadStream : Stream
        {
            /// <inheritdoc/>
            public override bool CanRead => _body.CanRead;
            /// <inheritdoc/>
            public override bool CanSeek => _body.CanSeek;
            /// <inheritdoc/>
            public override bool CanWrite => _body.CanWrite;
            /// <inheritdoc/>
            public override long Length => _body.Length;
            /// <inheritdoc/>
            public override long Position
            {
                get => _body.Position;
                set => _body.Position = value;
            }

            /// <summary>
            /// Create download stream
            /// </summary>
            /// <param name="httpClient"></param>
            /// <param name="request"></param>
            /// <param name="body"></param>
            private DownloadStream(HttpClient httpClient, HttpRequestMessage request, Stream body)
            {
                _httpClient = httpClient;
                _request = request;
                _body = body;
            }

            /// <inheritdoc/>
            public static async Task<ServiceResponse<Stream>> CreateAsync(FileSystemServicesRestClient outer,
                ConnectionModel endpoint, FileSystemObjectModel file, CancellationToken ct)
            {
                var uri = new Uri($"{outer._serviceUri}/v2/filesystem/download");
                var httpClient = outer._httpClient.CreateClient();
                httpClient.Timeout = TimeSpan.FromMinutes(2);

                var _serializer = new NewtonsoftJsonSerializer();
                using var request = new HttpRequestMessage(HttpMethod.Get, uri);
                request.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("application/octet-stream"));
                request.Headers.Add("x-ms-target", _serializer.SerializeObjectToString(file));
                request.Headers.Add("x-ms-connection", _serializer.SerializeObjectToString(endpoint));
                try
                {
                    var response = await httpClient.SendAsync(request, HttpCompletionOption.ResponseHeadersRead,
                        ct).ConfigureAwait(false);
                    var stream = await response.Content.ReadAsStreamAsync(ct);
                    if (!response.IsSuccessStatusCode)
                    {
                        string errorInfo = null;
                        if (response.Headers.TryGetValues("errorInfo", out var header))
                        {
                            errorInfo = header.FirstOrDefault();
                        }
                        response.Dispose();
                        if (errorInfo != null)
                        {
                            // Error response
                            return new ServiceResponse<Stream>
                            {
                                ErrorInfo = _serializer.Deserialize<ServiceResultModel>(errorInfo)
                            };
                        }
                        throw new HttpRequestException($"Failed to download file: {response.StatusCode}");
                    }
                    var client = httpClient;
                    httpClient = null;
                    return new ServiceResponse<Stream>
                    {
                        Result = new DownloadStream(client, request, stream)
                    };
                }
                finally
                {
                    httpClient?.Dispose();
                }
            }

            /// <inheritdoc/>
            protected override void Dispose(bool disposing)
            {
                if (disposing)
                {
                    _body.Dispose();
                    _request.Dispose();
                    _httpClient.Dispose();
                }
                base.Dispose(disposing);
            }

            /// <inheritdoc/>
            public override void Flush()
            {
                _body.Flush();
            }

            /// <inheritdoc/>
            public override int Read(byte[] buffer, int offset, int count)
            {
                return _body.Read(buffer, offset, count);
            }

            /// <inheritdoc/>
            public override long Seek(long offset, SeekOrigin origin)
            {
                return _body.Seek(offset, origin);
            }

            /// <inheritdoc/>
            public override void SetLength(long value)
            {
                _body.SetLength(value);
            }

            /// <inheritdoc/>
            public override void Write(byte[] buffer, int offset, int count)
            {
                _body.Write(buffer, offset, count);
            }

            private readonly HttpClient _httpClient;
            private readonly HttpRequestMessage _request;
            private readonly Stream _body;
        }

        /// <summary>
        /// Write stream wraps a request content body
        /// </summary>
        internal sealed class UploadStream : Stream
        {
            /// <inheritdoc/>
            public override bool CanRead => false;
            /// <inheritdoc/>
            public override bool CanSeek => false;
            /// <inheritdoc/>
            public override bool CanWrite => true;
            /// <inheritdoc/>
            public override long Length => _length;
            /// <inheritdoc/>
            public override long Position { get; set; }

            /// <summary>
            /// Service response
            /// </summary>
            public ServiceResponse<Stream> Result { get; private set; }

            /// <summary>
            /// Create upload stream
            /// </summary>
            /// <param name="httpClient"></param>
            /// <param name="request"></param>
            /// <param name="serializer"></param>
            /// <param name="ct"></param>
            public UploadStream(HttpClient httpClient,
                HttpRequestMessage request, IJsonSerializer serializer, CancellationToken ct)
            {
                _httpClient = httpClient;
                _request = request;
                _serializer = serializer;
                _request.Content = new StreamContent(_pipe.Reader.AsStream(false));
                _streaming = StartAsync(ct);
            }

            /// <inheritdoc/>
            public static ServiceResponse<Stream> Create(FileSystemServicesRestClient outer,
                ConnectionModel endpoint, FileSystemObjectModel file, FileOpenWriteOptionsModel options,
                CancellationToken ct)
            {
                var uri = new Uri($"{outer._serviceUri}/v2/filesystem/upload");
                var httpClient = outer._httpClient.CreateClient();
                httpClient.Timeout = TimeSpan.FromMinutes(2);
                var request = new HttpRequestMessage(HttpMethod.Post, uri);

                var serializer = new NewtonsoftJsonSerializer();
                request.Headers.Add("x-ms-target", serializer.SerializeObjectToString(file));
                request.Headers.Add("x-ms-connection", serializer.SerializeObjectToString(endpoint));
                request.Headers.Add("x-ms-options", serializer.SerializeObjectToString(options));

                var stream = new UploadStream(httpClient, request, serializer, ct);
                return new ServiceResponse<Stream>
                {
                    Result = stream
                };
            }

            /// <inheritdoc/>
            public override void Flush()
            {
            }

            /// <inheritdoc/>
            public override long Seek(long offset, SeekOrigin origin)
            {
                throw new NotSupportedException();
            }

            /// <inheritdoc/>
            public override void SetLength(long value)
            {
                throw new NotSupportedException();
            }

            /// <inheritdoc/>
            public override int Read(byte[] buffer, int offset, int count)
            {
                throw new NotSupportedException();
            }

            /// <inheritdoc/>
            public override void Write(byte[] buffer, int offset, int count)
            {
                _pipe.Writer.Write(buffer.AsSpan().Slice(offset, count));
                _length += count;
            }

            /// <inheritdoc/>
            public override async ValueTask DisposeAsync()
            {
                // Stream owner is done writing and disposes
                if (_streaming != null)
                {
                    try
                    {
                        await _pipe.Writer.CompleteAsync().ConfigureAwait(false);

                        // now wait until fully sent and result received
                        await _streaming.ConfigureAwait(false);
                        _streaming = null;
                    }
                    catch (OperationCanceledException) { } // Ct triggered
                    finally
                    {
                        _request.Dispose();
                        _httpClient.Dispose();
                    }
                }
                await base.DisposeAsync();
            }

            /// <inheritdoc/>
            protected override void Dispose(bool disposing)
            {
                if (disposing && _streaming != null)
                {
                    DisposeAsync().AsTask().GetAwaiter().GetResult();
                }
                base.Dispose(disposing);
            }

            /// <summary>
            /// Start streaming
            /// </summary>
            /// <param name="ct"></param>
            /// <returns></returns>
            /// <exception cref="HttpRequestException"></exception>
            private async Task StartAsync(CancellationToken ct)
            {
                var response = await _httpClient.SendAsync(_request, ct).ConfigureAwait(false);

                // Stream fully sent and response returned
                if (!response.IsSuccessStatusCode)
                {
                    string errorInfo = null;
                    if (response.Headers.TryGetValues("errorInfo", out var header))
                    {
                        errorInfo = header.FirstOrDefault();
                    }
                    response.Dispose();
                    if (errorInfo != null)
                    {
                        // Error response
                        Result = new ServiceResponse<Stream>
                        {
                            ErrorInfo = _serializer.Deserialize<ServiceResultModel>(errorInfo)
                        };
                    }
                    throw new HttpRequestException($"Failed to upload file: {response.StatusCode}");
                }
            }

            private readonly HttpClient _httpClient;
            private readonly HttpRequestMessage _request;
            private readonly Pipe _pipe = new();
            private readonly IJsonSerializer _serializer;
            private int _length;
            private Task _streaming;
        }

        private readonly IHttpClientFactory _httpClient;
        private readonly ISerializer _serializer;
        private readonly string _serviceUri;
    }
}
