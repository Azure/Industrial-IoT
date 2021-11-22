// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IIoT.OpcUa.Edge.Publisher.Services {
    using Microsoft.Azure.IIoT.Exceptions;
    using Microsoft.Azure.IIoT.OpcUa.Edge.Publisher;
    using Microsoft.Azure.IIoT.OpcUa.Publisher.Config.Models;
    using Serilog;
    using System;
    using System.Collections.Generic;
    using System.Net;
    using System.Threading;
    using System.Threading.Tasks;


    /// <summary>
    /// Publisher config services
    /// </summary>
    public class PublisherConfigServices : IPublisherConfigServices {

        /// <summary>
        /// 
        /// </summary>
        /// <param name="logger"></param>
        public PublisherConfigServices(ILogger logger) {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        /// <inheritdoc/>
        public async Task<List<string>> PublishNodesAsync(PublishedNodesEntryModel request) {
            await _lock.WaitAsync().ConfigureAwait(false);
            try {
                _logger.Information("PublishNodes method triggered");
                return new List<string> { "NotImplemented" };
            }
            finally {
                _lock.Release();
            }
        }

        /// <inheritdoc/>
        public async Task<List<string>> UnpublishNodesAsync(PublishedNodesEntryModel request) {
            await _lock.WaitAsync().ConfigureAwait(false);
            try {
                _logger.Information("UnpublishNodes method triggered");
                return new List<string> { "NotImplemented" };
            }
            finally {
                _lock.Release();
            }
        }

        /// <inheritdoc/>
        public async Task UnpublishAllNodesAsync() {
            await _lock.WaitAsync().ConfigureAwait(false);
            try {
                _logger.Information("UnpublishAllNodes method triggered");
                throw new MethodCallStatusException((int)HttpStatusCode.NotImplemented, "Not Implemented");
            }
            finally {
                _lock.Release();
            }
        }

        /// <inheritdoc/>
        public async Task GetConfiguredEndpointsAsync() {
            await _lock.WaitAsync().ConfigureAwait(false);
            try {
                _logger.Information("GetConfiguredEndpointsAsync method triggered");
                throw new MethodCallStatusException((int)HttpStatusCode.NotImplemented, "Not Implemented");
            }
            finally {
                _lock.Release();
            }
        }

        /// <inheritdoc/>
        public async Task GetConfiguredNodesOnEndpointAsync() {
            await _lock.WaitAsync().ConfigureAwait(false);
            try {
                _logger.Information("GetConfiguredNodesOnEndpointAsync method triggered");
                throw new MethodCallStatusException((int)HttpStatusCode.NotImplemented, "Not Implemented");
            }
            finally {
                _lock.Release();
            }
        }

        private readonly ILogger _logger;

        private readonly SemaphoreSlim _lock = new SemaphoreSlim(1, 1);
    }
}
