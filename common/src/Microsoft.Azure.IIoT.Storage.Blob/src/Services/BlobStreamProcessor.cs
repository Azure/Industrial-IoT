// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IIoT.Storage.Blob.Services {
    using Microsoft.Azure.IIoT.Messaging;
    using Microsoft.Azure.IIoT.Hub;
    using Microsoft.WindowsAzure.Storage;
    using Microsoft.WindowsAzure.Storage.Blob;
    using Serilog;
    using System;
    using System.Collections.Generic;
    using System.Text;
    using System.Threading;
    using System.Threading.Tasks;

    /// <summary>
    /// Process blobs by reading the stream and passing it to a handler.
    /// The processor can be registered with an event hub processor or
    /// with the IoT Hub blob upload host.  The latter is a simple setup,
    /// the former allows fan out of processing through event hub or
    /// service bus.  No matter what the processor simply processes the
    /// stream it is handed.
    /// </summary>
    public class BlobStreamProcessor : IDeviceFileUploadHandler, IEventProcessingHandler {

        /// <summary>
        /// Create stream processor
        /// </summary>
        /// <param name="config"></param>
        /// <param name="processor"></param>
        /// <param name="logger"></param>
        public BlobStreamProcessor(IStorageConfig config, IBlobProcessor processor,
            ILogger logger) {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _processor = processor ?? throw new ArgumentNullException(nameof(processor));
            var storageAccount = CloudStorageAccount.Parse(config.BlobStorageConnString);
            _client = storageAccount.CreateCloudBlobClient();
            _options = new BlobRequestOptions();
        }

        /// <inheritdoc/>
        public async Task HandleAsync(string deviceId, string moduleId, string blobName,
            string contentType, string blobUri, DateTime enqueuedTimeUtc, CancellationToken ct) {
            // If registered with blob upload notification host directly - this gets called.
            var properties = new Dictionary<string, string> {
                { CommonProperties.DeviceId, deviceId },
                { CommonProperties.ModuleId, moduleId },
                { CommonProperties.EventSchemaType, contentType },
                { "BlobUri", blobUri },
                { "BlobName", blobName },
                { "EnqueuedTimeUtc", enqueuedTimeUtc.ToString() }
            };
            await ProcessBlobAsync(blobUri, properties, ct);
        }

        /// <inheritdoc/>
        public async Task HandleAsync(byte[] eventData, IDictionary<string, string> properties,
            Func<Task> checkpoint) {
            // Called as a result of an event - allows fan out of blob processing
            if (eventData == null) {
                return;
            }
            // Assume the event data is a string representing the uri to process
            var blobUri = Encoding.UTF8.GetString(eventData);
            if (!Uri.TryCreate(blobUri, UriKind.Absolute, out _)) {
                // We can always add more formats here later...
                return;
            }
            await ProcessBlobAsync(blobUri, properties, CancellationToken.None);
        }

        /// <inheritdoc/>
        public Task OnBatchCompleteAsync() {
            return Task.CompletedTask;
        }

        /// <summary>
        /// Process blob
        /// </summary>
        /// <param name="blobUri"></param>
        /// <param name="properties"></param>
        /// <param name="ct"></param>
        /// <returns></returns>
        private async Task ProcessBlobAsync(string blobUri,
            IDictionary<string, string> properties, CancellationToken ct) {
            try {
                var blob = new CloudBlockBlob(new Uri(blobUri), _client);
                while (true) {
                    var context = new OperationContext();
                    using (var stream = await blob.OpenReadAsync(
                        AccessCondition.GenerateIfExistsCondition(), _options, context, ct)) {

                        properties.AddOrUpdate("RequestId", context.ClientRequestID);
                        var disposition = await _processor.ProcessAsync(stream, properties, ct);
                        if (disposition == BlobDisposition.Retry) {
                            continue;
                        }
                        if (disposition != BlobDisposition.Delete) {
                            break;
                        }
                        await blob.DeleteAsync(DeleteSnapshotsOption.IncludeSnapshots,
                            AccessCondition.GenerateIfExistsCondition(), _options, context, ct);
                        break;
                    }
                }
            }
            catch (StorageException ex) {
                _logger.Error(ex, "Failed to process blob stream due to storage exception");
            }
        }

        private readonly CloudBlobClient _client;
        private readonly BlobRequestOptions _options;
        private readonly ILogger _logger;
        private readonly IBlobProcessor _processor;
    }
}
