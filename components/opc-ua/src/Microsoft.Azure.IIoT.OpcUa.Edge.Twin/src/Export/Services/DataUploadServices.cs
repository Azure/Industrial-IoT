// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IIoT.OpcUa.Edge.Export.Services {
    using Microsoft.Azure.IIoT.OpcUa.Protocol;
    using Microsoft.Azure.IIoT.OpcUa.Protocol.Models;
    using Microsoft.Azure.IIoT.OpcUa.Twin;
    using Microsoft.Azure.IIoT.OpcUa.Core.Models;
    using Microsoft.Azure.IIoT.OpcUa.Twin.Models;
    using Serilog;
    using Microsoft.Azure.IIoT.Tasks;
    using Microsoft.Azure.IIoT.Module;
    using System;
    using System.IO;
    using System.IO.Compression;
    using System.Threading;
    using System.Threading.Tasks;
    using System.Collections.Concurrent;
    using System.Linq;

    /// <summary>
    /// Bulk upload exported data to blob
    /// </summary>
    public sealed class DataUploadServices : IUploadServices<EndpointModel>, IDisposable {

        /// <summary>
        /// Create upload services
        /// </summary>
        /// <param name="client"></param>
        /// <param name="upload"></param>
        /// <param name="scheduler"></param>
        /// <param name="logger"></param>
        public DataUploadServices(IEndpointServices client, IBlobUpload upload,
            ITaskScheduler scheduler, ILogger logger) {
            _client = client ?? throw new ArgumentNullException(nameof(client));
            _upload = upload ?? throw new ArgumentNullException(nameof(upload));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _scheduler = scheduler ?? throw new ArgumentNullException(nameof(scheduler));
            _tasks = new ConcurrentDictionary<EndpointIdentifier, ModelUploadTask>();
        }

        /// <inheritdoc/>
        public Task<ModelUploadStartResultModel> ModelUploadStartAsync(EndpointModel endpoint,
            ModelUploadStartRequestModel request) {
            if (endpoint == null) {
                throw new ArgumentNullException(nameof(endpoint));
            }
            if (request == null) {
                throw new ArgumentNullException(nameof(request));
            }

            // Get or add new task
            var task = _tasks.GetOrAdd(new EndpointIdentifier(endpoint),
                id => new ModelUploadTask(this, id, request.ContentEncoding, request.Diagnostics));

            // Return info about task
            return Task.FromResult(new ModelUploadStartResultModel {
                BlobName = task.FileName,
                ContentEncoding = task.Encoding + "+gzip",
                TimeStamp = task.StartTime
            });
        }

        /// <summary>
        /// Dispose
        /// </summary>
        public void Dispose() {
            if (_tasks.Count != 0) {
                Task.WaitAll(_tasks.Values.Select(t => t.CancelAsync()).ToArray());
                _tasks.Clear();
            }
        }

        /// <summary>
        /// A scheduled model upload task
        /// </summary>
        private class ModelUploadTask {

            /// <summary>
            /// File name to upload to
            /// </summary>
            public string FileName { get; }

            /// <summary>
            /// Content encoding
            /// </summary>
            public string Encoding { get; internal set; }

            /// <summary>
            /// Start time
            /// </summary>
            public DateTime StartTime { get; internal set; }

            /// <summary>
            /// Create model upload task
            /// </summary>
            /// <param name="outer"></param>
            /// <param name="id"></param>
            /// <param name="contentType"></param>
            /// <param name="diagnostics"></param>
            public ModelUploadTask(DataUploadServices outer, EndpointIdentifier id,
                string contentType, DiagnosticsModel diagnostics) {
                Encoding = ValidateEncoding(contentType, out var extension);
                StartTime = DateTime.UtcNow;
                FileName = $"{id.GetHashCode()}_{StartTime.ToBinary()}{extension}";
                _outer = outer;
                _cts = new CancellationTokenSource();
                _job = _outer._scheduler.Run(() => UploadModelAsync(id, diagnostics, _cts.Token));
            }

            /// <summary>
            /// Run export cycles
            /// </summary>
            /// <param name="id"></param>
            /// <param name="diagnostics"></param>
            /// <param name="ct"></param>
            /// <returns></returns>
            private async Task UploadModelAsync(EndpointIdentifier id, DiagnosticsModel diagnostics,
                CancellationToken ct) {
                var fullPath = Path.Combine(Path.GetTempPath(), FileName);
                try {
                    _outer._logger.Information("Start model upload to {fileName} for {url}.",
                        FileName, id.Endpoint.Url);
                    using (var file = new FileStream(fullPath, FileMode.Create))
                    using (var stream = new GZipStream(file, CompressionMode.Compress)) {
                        // TODO: Try read nodeset from namespace metadata!
                        // ...

                        // Otherwise browse model
                        await BrowseEncodeModelAsync(id.Endpoint, diagnostics, stream, ct);
                    }
                    // now upload file
                    await _outer._upload.SendFileAsync(fullPath, Encoding);
                    _outer._logger.Information("Model uploaded to {fileName} for {url}.",
                        FileName, id.Endpoint.Url);
                }
                catch (OperationCanceledException) {
                    _outer._logger.Information("Cancelled model upload of {fileName} for {url}",
                        FileName, id.Endpoint.Url);
                }
                catch (Exception ex) {
                    _outer._logger.Error(ex, "Error during exportto {fileName} for {url}.",
                        FileName, id.Endpoint.Url);
                }
                finally {
                    File.Delete(fullPath);
                    _outer._tasks.TryRemove(id, out var tmp);
                }
            }

            /// <summary>
            /// Export using browse encoder
            /// </summary>
            /// <param name="endpoint"></param>
            /// <param name="diagnostics"></param>
            /// <param name="stream"></param>
            /// <param name="ct"></param>
            /// <returns></returns>
            private async Task BrowseEncodeModelAsync(EndpointModel endpoint,
                DiagnosticsModel diagnostics, Stream stream, CancellationToken ct) {
                using (var encoder = new BrowseStreamEncoder(_outer._client, endpoint,
                    stream, Encoding, diagnostics, _outer._logger, null)) {
                    await encoder.EncodeAsync(ct);
                }
            }

            /// <summary>
            /// Get file extension for content type
            /// </summary>
            /// <param name="contentType"></param>
            /// <param name="extension"></param>
            /// <returns></returns>
            private static string ValidateEncoding(string contentType, out string extension) {
                if (contentType == null) {
                    contentType = ContentMimeType.UaJson;
                }
                switch (contentType.ToLowerInvariant()) {
#if NO_SUPPORT
                    case ContentEncodings.MimeTypeUaBinary:
                        extension = ".ua.bin.gzip";
                        break;
                    case ContentEncodings.MimeTypeUaXml:
                        extension = ".ua.xml.gzip";
                        break;
#endif
                    case ContentMimeType.UaBson:
                        extension = ".ua.bson.gzip";
                        break;
                    default:
                        extension = ".ua.json.gzip";
                        contentType = ContentMimeType.UaJson;
                        break;
                }
                return contentType;
            }
            /// <summary>
            /// Cancel task
            /// </summary>
            /// <returns></returns>
            public Task CancelAsync() {
                _cts.Cancel();
                return _job;
            }

            private readonly CancellationTokenSource _cts;
            private readonly Task _job;
            private readonly DataUploadServices _outer;
        }

        private readonly IEndpointServices _client;
        private readonly IBlobUpload _upload;
        private readonly ITaskScheduler _scheduler;
        private readonly ILogger _logger;
        private readonly ConcurrentDictionary<EndpointIdentifier, ModelUploadTask> _tasks;
    }
}
