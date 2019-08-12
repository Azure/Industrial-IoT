// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IIoT.Hub.Client {
    using Microsoft.Azure.Devices;
    using Serilog;
    using Microsoft.Azure.IIoT.Hub;
    using Microsoft.Azure.IIoT.Utils;
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Threading;
    using System.Threading.Tasks;

    /// <summary>
    /// File notification processor
    /// </summary>
    public sealed class IoTHubFileNotificationHost : IHost,
        IDisposable {

        /// <summary>
        /// Create service client
        /// </summary>
        /// <param name="config"></param>
        /// <param name="handlers"></param>
        /// <param name="logger"></param>
        public IoTHubFileNotificationHost(IIoTHubConfig config,
            IEnumerable<IBlobUploadHandler> handlers, ILogger logger) {
            if (string.IsNullOrEmpty(config.IoTHubConnString)) {
                throw new ArgumentException(nameof(config));
            }

            _handlers = handlers ?? throw new ArgumentNullException(nameof(handlers));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _client = ServiceClient.CreateFromConnectionString(config.IoTHubConnString);
            _cts = new CancellationTokenSource();
        }

        /// <inheritdoc/>
        public async Task StartAsync() {
            try {
                await _client.OpenAsync();
                var receiver = _client.GetFileNotificationReceiver();
                _task = Task.Run(() => RunAsync(receiver));
                _logger.Debug("File notification host started.");
            }
            catch (Exception ex) {
                _logger.Error(ex, "Failed to start file notification host.");
            }
        }

        /// <inheritdoc/>
        public async Task StopAsync() {
            _cts.Cancel();
            try {
                await _client.CloseAsync();
                await _task;
                _logger.Debug("File notification host stopped.");
            }
            catch (Exception ex) {
                _logger.Error(ex, "Failed to stop file notification host.");
            }
        }

        /// <inheritdoc/>
        public void Dispose() {
            StopAsync().Wait();
            _client.Dispose();
            _cts.Dispose();
        }

        /// <summary>
        /// Handle file notifications
        /// </summary>
        /// <param name="receiver"></param>
        /// <returns></returns>
        private async Task RunAsync(FileNotificationReceiver<FileNotification> receiver) {
            while (!_cts.IsCancellationRequested) {
                var notification = await receiver.ReceiveAsync();
                if (notification == null) {
                    continue;
                }

                // Parse content type from blob name
                var blobName = notification.BlobName;
                string contentType = null;
                var delim = blobName.IndexOf('/');
                if (delim != -1) {
                    // the first segment is the url encoded content type
                    contentType = blobName.Substring(0, delim).UrlDecode();
                    blobName = blobName.Substring(delim + 1);
                }

                // Call handlers
                await Try.Async(() => Task.WhenAll(_handlers
                    .Select(h => h.HandleAsync(notification.DeviceId, null,
                        blobName, contentType, notification.BlobUri,
                        notification.EnqueuedTimeUtc, _cts.Token))));
                await receiver.CompleteAsync(notification);
            }
        }

        private readonly CancellationTokenSource _cts;
        private readonly IEnumerable<IBlobUploadHandler> _handlers;
        private readonly ILogger _logger;
        private readonly ServiceClient _client;
        private Task _task;
    }
}
